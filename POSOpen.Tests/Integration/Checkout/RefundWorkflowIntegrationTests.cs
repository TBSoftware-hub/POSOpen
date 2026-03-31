using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using POSOpen.Application.Abstractions.Security;
using POSOpen.Application.Security;
using POSOpen.Application.UseCases.Checkout;
using POSOpen.Domain.Entities;
using POSOpen.Domain.Enums;
using POSOpen.Infrastructure.Persistence;
using POSOpen.Infrastructure.Persistence.Repositories;
using POSOpen.Shared.Operational;

namespace POSOpen.Tests.Integration.Checkout;

public sealed class RefundWorkflowIntegrationTests
{
	[Fact]
	public async Task SubmitRefundUseCase_WithConcurrentSubmissions_OnlyOneSucceeds_AndTotalDoesNotExceedApprovedAmount()
	{
		await using var fixture = await CreateFixtureAsync();
		var cartId = Guid.NewGuid();
		var staffId = Guid.NewGuid();
		var now = new DateTime(2026, 4, 2, 9, 0, 0, DateTimeKind.Utc);

		await fixture.CartRepository.CreateAsync(CartSession.Create(cartId, null, staffId, now));
		await fixture.PaymentAttemptRepository.AddAsync(
			CheckoutPaymentAttempt.Create(
				Guid.NewGuid(),
				cartId,
				1000,
				"USD",
				CheckoutPaymentAuthorizationStatus.Approved,
				null,
				null,
				now));

		var auth = new Mock<IAuthorizationPolicyService>();
		auth.Setup(x => x.HasPermission(StaffRole.Manager, RolePermissions.CheckoutRefundInitiate)).Returns(true);
		auth.Setup(x => x.HasPermission(StaffRole.Manager, RolePermissions.CheckoutRefundApprove)).Returns(true);

		var session = new Mock<ICurrentSessionService>();
		session.Setup(x => x.GetCurrent()).Returns(new CurrentSession(staffId, StaffRole.Manager, 1, 1));

		var firstUseCase = new SubmitRefundUseCase(
			fixture.CartRepository,
			fixture.PaymentAttemptRepository,
			fixture.RefundRepository,
			session.Object,
			auth.Object,
			fixture.OperationLogRepository,
			fixture.Clock,
			new Mock<ILogger<SubmitRefundUseCase>>().Object);

		var secondUseCase = new SubmitRefundUseCase(
			fixture.CartRepository,
			fixture.PaymentAttemptRepository,
			fixture.RefundRepository,
			session.Object,
			auth.Object,
			fixture.OperationLogRepository,
			fixture.Clock,
			new Mock<ILogger<SubmitRefundUseCase>>().Object);

		var firstCommand = new SubmitRefundCommand(cartId, 750, "Customer request", RefundPath.Direct, new OperationContext(Guid.NewGuid(), Guid.NewGuid(), null, now));
		var secondCommand = new SubmitRefundCommand(cartId, 750, "Customer request", RefundPath.Direct, new OperationContext(Guid.NewGuid(), Guid.NewGuid(), null, now));

		var results = await Task.WhenAll(
			firstUseCase.ExecuteAsync(firstCommand),
			secondUseCase.ExecuteAsync(secondCommand));

		results.Count(x => x.IsSuccess).Should().Be(1);
		results.Count(x => !x.IsSuccess).Should().Be(1);
		results.Single(x => !x.IsSuccess).ErrorCode.Should().Be(RefundWorkflowConstants.ErrorAmountInvalid);

		var refundRecords = await fixture.RefundRepository.ListByCartSessionAsync(cartId);
		refundRecords.Should().ContainSingle();
		refundRecords.Sum(x => x.AmountCents).Should().BeLessThanOrEqualTo(1000);

		var logs = await fixture.OperationLogRepository.ListAsync();
		logs.Count(x => x.EventType == SecurityAuditEventTypes.RefundCompleted).Should().Be(1);
		logs.Count(x => x.EventType == SecurityAuditEventTypes.RefundDenied).Should().Be(1);
	}

	[Fact]
	public async Task SubmitRefundUseCase_PersistsRefund_AndAppendsImmutableCompletedAudit_WithoutDuplicateOnRetry()
	{
		await using var fixture = await CreateFixtureAsync();
		var cartId = Guid.NewGuid();
		var staffId = Guid.NewGuid();
		var now = new DateTime(2026, 4, 2, 9, 0, 0, DateTimeKind.Utc);

		await fixture.CartRepository.CreateAsync(CartSession.Create(cartId, null, staffId, now));
		await fixture.PaymentAttemptRepository.AddAsync(
			CheckoutPaymentAttempt.Create(
				Guid.NewGuid(),
				cartId,
				5000,
				"USD",
				CheckoutPaymentAuthorizationStatus.Approved,
				null,
				null,
				now));

		var auth = new Mock<IAuthorizationPolicyService>();
		auth.Setup(x => x.HasPermission(StaffRole.Manager, RolePermissions.CheckoutRefundInitiate)).Returns(true);
		auth.Setup(x => x.HasPermission(StaffRole.Manager, RolePermissions.CheckoutRefundApprove)).Returns(true);

		var session = new Mock<ICurrentSessionService>();
		session.Setup(x => x.GetCurrent()).Returns(new CurrentSession(staffId, StaffRole.Manager, 1, 1));

		var sut = new SubmitRefundUseCase(
			fixture.CartRepository,
			fixture.PaymentAttemptRepository,
			fixture.RefundRepository,
			session.Object,
			auth.Object,
			fixture.OperationLogRepository,
			fixture.Clock,
			new Mock<ILogger<SubmitRefundUseCase>>().Object);

		var context = new OperationContext(Guid.NewGuid(), Guid.NewGuid(), null, now);
		var command = new SubmitRefundCommand(cartId, 1500, "Customer corrected purchase", RefundPath.Direct, context);

		var first = await sut.ExecuteAsync(command);
		var second = await sut.ExecuteAsync(command);

		first.IsSuccess.Should().BeTrue();
		first.Payload!.Status.Should().Be(RefundStatus.Completed);
		second.IsSuccess.Should().BeTrue();
		second.Payload!.Status.Should().Be(RefundStatus.Completed);

		var refundRecords = await fixture.RefundRepository.ListByCartSessionAsync(cartId);
		refundRecords.Should().ContainSingle();
		refundRecords[0].OperationId.Should().Be(context.OperationId);

		var logs = await fixture.OperationLogRepository.ListAsync();
		logs.Count(x => x.EventType == SecurityAuditEventTypes.RefundCompleted && x.OperationId == context.OperationId)
			.Should().Be(1);
		logs.Count(x => x.EventType == SecurityAuditEventTypes.RefundInitiated && x.OperationId == context.OperationId)
			.Should().Be(1);
	}

	[Fact]
	public async Task SubmitRefundUseCase_WhenCashierDenied_PersistsImmutableDeniedAuditEvent()
	{
		await using var fixture = await CreateFixtureAsync();
		var cartId = Guid.NewGuid();
		var staffId = Guid.NewGuid();
		var now = new DateTime(2026, 4, 2, 9, 0, 0, DateTimeKind.Utc);

		await fixture.CartRepository.CreateAsync(CartSession.Create(cartId, null, staffId, now));
		await fixture.PaymentAttemptRepository.AddAsync(
			CheckoutPaymentAttempt.Create(
				Guid.NewGuid(),
				cartId,
				5000,
				"USD",
				CheckoutPaymentAuthorizationStatus.Approved,
				null,
				null,
				now));

		var auth = new Mock<IAuthorizationPolicyService>();
		auth.Setup(x => x.HasPermission(StaffRole.Cashier, RolePermissions.CheckoutRefundInitiate)).Returns(true);
		auth.Setup(x => x.HasPermission(StaffRole.Cashier, RolePermissions.CheckoutRefundApprove)).Returns(false);

		var session = new Mock<ICurrentSessionService>();
		session.Setup(x => x.GetCurrent()).Returns(new CurrentSession(staffId, StaffRole.Cashier, 1, 1));

		var sut = new SubmitRefundUseCase(
			fixture.CartRepository,
			fixture.PaymentAttemptRepository,
			fixture.RefundRepository,
			session.Object,
			auth.Object,
			fixture.OperationLogRepository,
			fixture.Clock,
			new Mock<ILogger<SubmitRefundUseCase>>().Object);

		var context = new OperationContext(Guid.NewGuid(), Guid.NewGuid(), null, now);
		var command = new SubmitRefundCommand(cartId, 1500, "policy mismatch", RefundPath.Direct, context);

		// Act
		var result = await sut.ExecuteAsync(command);

		// Assert
		result.IsSuccess.Should().BeFalse();
		result.ErrorCode.Should().Be(RefundWorkflowConstants.ErrorPathForbidden);

		// Verify refund record was NOT persisted
		var refunds = await fixture.RefundRepository.ListByCartSessionAsync(cartId);
		refunds.Should().BeEmpty();

		// Verify denial audit WAS persisted (end-to-end!)
		var logs = await fixture.OperationLogRepository.ListAsync();
		var deniedEvent = logs.FirstOrDefault(x =>
			x.EventType == SecurityAuditEventTypes.RefundDenied &&
			x.OperationId == context.OperationId);
		deniedEvent.Should().NotBeNull();
		deniedEvent!.AggregateId.Should().Be(cartId.ToString());
	}

	[Fact]
	public async Task DenyRefundApprovalUseCase_WhenManagerDeniesPendingRefund_PersistsApprovalDeniedStateAndAuditEvent()
	{
		await using var fixture = await CreateFixtureAsync();
		var cartId = Guid.NewGuid();
		var cashierId = Guid.NewGuid();
		var managerId = Guid.NewGuid();
		var now = new DateTime(2026, 4, 2, 10, 0, 0, DateTimeKind.Utc);

		await fixture.CartRepository.CreateAsync(CartSession.Create(cartId, null, cashierId, now));

		var pendingRefund = RefundRecord.Create(
			Guid.NewGuid(),
			cartId,
			Guid.NewGuid(),
			RefundStatus.PendingApproval,
			RefundPath.ApprovalRequired,
			1200,
			"USD",
			"cashier request",
			cashierId,
			StaffRole.Cashier.ToString(),
			now);

		await fixture.RefundRepository.AddAsync(pendingRefund);

		var auth = new Mock<IAuthorizationPolicyService>();
		auth.Setup(x => x.HasPermission(StaffRole.Manager, RolePermissions.CheckoutRefundApprove)).Returns(true);

		var session = new Mock<ICurrentSessionService>();
		session.Setup(x => x.GetCurrent()).Returns(new CurrentSession(managerId, StaffRole.Manager, 1, 1));

		var sut = new DenyRefundApprovalUseCase(
			fixture.RefundRepository,
			session.Object,
			auth.Object,
			fixture.OperationLogRepository,
			fixture.Clock,
			new Mock<ILogger<DenyRefundApprovalUseCase>>().Object);

		var context = new OperationContext(Guid.NewGuid(), Guid.NewGuid(), null, now);
		var command = new DenyRefundApprovalCommand(pendingRefund.Id, "manager denied", context);

		var result = await sut.ExecuteAsync(command);

		result.IsSuccess.Should().BeTrue();
		result.Payload.Should().NotBeNull();
		result.Payload!.Status.Should().Be(RefundStatus.ApprovalDenied);

		var updated = await fixture.RefundRepository.GetByIdAsync(pendingRefund.Id);
		updated.Should().NotBeNull();
		updated!.Status.Should().Be(RefundStatus.ApprovalDenied);
		updated.ActorStaffId.Should().Be(managerId);
		updated.Reason.Should().Be("manager denied");

		var logs = await fixture.OperationLogRepository.ListAsync();
		var deniedEvent = logs.FirstOrDefault(x =>
			x.EventType == SecurityAuditEventTypes.RefundApprovalDenied &&
			x.OperationId == context.OperationId);

		deniedEvent.Should().NotBeNull();
		deniedEvent!.AggregateId.Should().Be(cartId.ToString());
		deniedEvent.PayloadJson.Should().Contain("manager denied");
	}

	private static async Task<TestFixture> CreateFixtureAsync()
	{
		var databasePath = TestDatabasePaths.Create();
		var dbContextFactory = new TestDbContextFactory(databasePath);
		var initializer = new AppDbContextInitializer(dbContextFactory, new Mock<ILogger<AppDbContextInitializer>>().Object);
		await initializer.InitializeAsync();

		var clock = new TestUtcClock(new DateTime(2026, 4, 2, 9, 0, 0, DateTimeKind.Utc));
		return new TestFixture(
			dbContextFactory,
			new CartSessionRepository(dbContextFactory),
			new CheckoutPaymentAttemptRepository(dbContextFactory),
			new RefundRepository(dbContextFactory),
			new OperationLogRepository(dbContextFactory, clock),
			clock);
	}

	private sealed class TestFixture : IAsyncDisposable
	{
		public TestFixture(
			TestDbContextFactory dbContextFactory,
			CartSessionRepository cartRepository,
			CheckoutPaymentAttemptRepository paymentAttemptRepository,
			RefundRepository refundRepository,
			OperationLogRepository operationLogRepository,
			TestUtcClock clock)
		{
			DbContextFactory = dbContextFactory;
			CartRepository = cartRepository;
			PaymentAttemptRepository = paymentAttemptRepository;
			RefundRepository = refundRepository;
			OperationLogRepository = operationLogRepository;
			Clock = clock;
		}

		public TestDbContextFactory DbContextFactory { get; }
		public CartSessionRepository CartRepository { get; }
		public CheckoutPaymentAttemptRepository PaymentAttemptRepository { get; }
		public RefundRepository RefundRepository { get; }
		public OperationLogRepository OperationLogRepository { get; }
		public TestUtcClock Clock { get; }

		public async ValueTask DisposeAsync()
		{
			await DbContextFactory.DisposeAsync();
		}
	}
}