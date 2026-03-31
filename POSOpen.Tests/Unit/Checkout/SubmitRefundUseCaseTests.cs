using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using POSOpen.Application.Abstractions.Repositories;
using POSOpen.Application.Abstractions.Security;
using POSOpen.Application.Abstractions.Services;
using POSOpen.Application.Security;
using POSOpen.Application.UseCases.Checkout;
using POSOpen.Domain.Entities;
using POSOpen.Domain.Enums;
using POSOpen.Shared.Operational;

namespace POSOpen.Tests.Unit.Checkout;

public sealed class SubmitRefundUseCaseTests
{
	[Fact]
	public async Task ExecuteAsync_WhenApprovalPathMissingReason_ReturnsReasonRequired()
	{
		var sut = BuildSut();
		var command = new SubmitRefundCommand(
			Guid.NewGuid(),
			500,
			" ",
			RefundPath.ApprovalRequired,
			new OperationContext(Guid.NewGuid(), Guid.NewGuid(), null, DateTime.UtcNow));

		var result = await sut.UseCase.ExecuteAsync(command);

		result.IsSuccess.Should().BeFalse();
		result.ErrorCode.Should().Be(RefundWorkflowConstants.ErrorReasonRequired);
	}

	[Fact]
	public async Task ExecuteAsync_WhenCashierRequestsDirectPath_AppendsDeniedAuditAndReturnsForbidden()
	{
		var staffId = Guid.NewGuid();
		var cartId = Guid.NewGuid();
		var now = DateTime.UtcNow;

		var sut = BuildSut(
			session: new CurrentSession(staffId, StaffRole.Cashier, 1, 1),
			allowInitiate: true,
			allowApprove: false,
			cart: CartSession.Create(cartId, null, staffId, now),
			attempts:
			[
				CheckoutPaymentAttempt.Create(
					Guid.NewGuid(),
					cartId,
					2500,
					"USD",
					CheckoutPaymentAuthorizationStatus.Approved,
					null,
					null,
					now)
			]);

		var command = new SubmitRefundCommand(
			cartId,
			500,
			"Policy mismatch",
			RefundPath.Direct,
			new OperationContext(Guid.NewGuid(), Guid.NewGuid(), null, now));

		var result = await sut.UseCase.ExecuteAsync(command);

		result.IsSuccess.Should().BeFalse();
		result.ErrorCode.Should().Be(RefundWorkflowConstants.ErrorPathForbidden);
		sut.OperationLogRepository.Verify(x => x.AppendAsync(
			SecurityAuditEventTypes.RefundDenied,
			cartId.ToString(),
			It.IsAny<object>(),
			command.Context,
			It.IsAny<int>(),
			It.IsAny<CancellationToken>()), Times.Once);
		sut.RefundRepository.Verify(x => x.AddAsync(It.IsAny<RefundRecord>(), It.IsAny<CancellationToken>()), Times.Never);
	}

	[Fact]
	public async Task ExecuteAsync_WhenManagerDirectPath_PersistsCompletedRefundAndAppendsAudit()
	{
		var staffId = Guid.NewGuid();
		var cartId = Guid.NewGuid();
		var now = DateTime.UtcNow;

		var sut = BuildSut(
			session: new CurrentSession(staffId, StaffRole.Manager, 1, 1),
			allowInitiate: true,
			allowApprove: true,
			cart: CartSession.Create(cartId, null, staffId, now),
			attempts:
			[
				CheckoutPaymentAttempt.Create(
					Guid.NewGuid(),
					cartId,
					3000,
					"USD",
					CheckoutPaymentAuthorizationStatus.Approved,
					null,
					null,
					now)
			]);

		var operation = new OperationContext(Guid.NewGuid(), Guid.NewGuid(), null, now);
		var command = new SubmitRefundCommand(cartId, 1000, "Customer return", RefundPath.Direct, operation);

		var result = await sut.UseCase.ExecuteAsync(command);

		result.IsSuccess.Should().BeTrue();
		result.Payload!.Status.Should().Be(RefundStatus.Completed);
		sut.RefundRepository.Verify(x => x.AddAsyncWithBalanceCheckAsync(It.Is<RefundRecord>(r =>
                        r.Status == RefundStatus.Completed &&
                        r.OperationId == operation.OperationId &&
                        r.AmountCents == 1000), It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Once);
		sut.OperationLogRepository.Verify(x => x.AppendAsync(
			SecurityAuditEventTypes.RefundCompleted,
			cartId.ToString(),
			It.IsAny<object>(),
			operation,
			It.IsAny<int>(),
			It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task ExecuteAsync_WhenOperationAlreadyProcessed_ReturnsExistingWithoutSideEffects()
	{
		var existing = RefundRecord.Create(
			Guid.NewGuid(),
			Guid.NewGuid(),
			Guid.NewGuid(),
			RefundStatus.Completed,
			RefundPath.Direct,
			1200,
			"USD",
			"Prior run",
			Guid.NewGuid(),
			StaffRole.Manager.ToString(),
			DateTime.UtcNow,
			DateTime.UtcNow);

		var sut = BuildSut(existingByOperation: existing);
		var command = new SubmitRefundCommand(
			existing.CartSessionId,
			existing.AmountCents,
			"ignored",
			RefundPath.Direct,
			new OperationContext(existing.OperationId, Guid.NewGuid(), null, DateTime.UtcNow));

		var result = await sut.UseCase.ExecuteAsync(command);

		result.IsSuccess.Should().BeTrue();
		result.Payload!.OperationId.Should().Be(existing.OperationId);
		sut.RefundRepository.Verify(x => x.AddAsync(It.IsAny<RefundRecord>(), It.IsAny<CancellationToken>()), Times.Never);
		sut.OperationLogRepository.Verify(x => x.AppendAsync(
			It.IsAny<string>(),
			It.IsAny<string>(),
			It.IsAny<object>(),
			It.IsAny<OperationContext>(),
			It.IsAny<int>(),
			It.IsAny<CancellationToken>()), Times.Never);
	}

	[Fact]
	public async Task ExecuteAsync_WhenAmountInvalid_DoesNotCheckIdempotencyAndReturnsAmountFailure()
	{
		var sut = BuildSut();
		var command = new SubmitRefundCommand(
			Guid.NewGuid(),
			0,
			"ignored",
			RefundPath.Direct,
			new OperationContext(Guid.NewGuid(), Guid.NewGuid(), null, DateTime.UtcNow));

		var result = await sut.UseCase.ExecuteAsync(command);

		result.IsSuccess.Should().BeFalse();
		result.ErrorCode.Should().Be(RefundWorkflowConstants.ErrorAmountInvalid);
		sut.RefundRepository.Verify(
			x => x.GetByOperationIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
			Times.Never);
	}

	[Fact]
	public async Task ExecuteAsync_WhenDirectPathWithNoReason_SucceedsWithDefaultReason()
	{
		var staffId = Guid.NewGuid();
		var cartId = Guid.NewGuid();
		var now = DateTime.UtcNow;
		RefundRecord? persistedRecord = null;

		var sut = BuildSut(
			session: new CurrentSession(staffId, StaffRole.Manager, 1, 1),
			allowInitiate: true,
			allowApprove: true,
			cart: CartSession.Create(cartId, null, staffId, now),
			attempts:
			[
				CheckoutPaymentAttempt.Create(
					Guid.NewGuid(),
					cartId,
					3000,
					"USD",
					CheckoutPaymentAuthorizationStatus.Approved,
					null,
					null,
					now)
			]);

		var command = new SubmitRefundCommand(
			cartId,
			1000,
			"   ",
			RefundPath.Direct,
			new OperationContext(Guid.NewGuid(), Guid.NewGuid(), null, now));

		sut.RefundRepository
			.Setup(x => x.AddAsyncWithBalanceCheckAsync(It.IsAny<RefundRecord>(), It.IsAny<long>(), It.IsAny<CancellationToken>()))
			.Callback<RefundRecord, long, CancellationToken>((record, _, _) => persistedRecord = record)
			.ReturnsAsync((RefundRecord record, long _, CancellationToken _) => record);

		var result = await sut.UseCase.ExecuteAsync(command);

		result.IsSuccess.Should().BeTrue();
		result.Payload!.Status.Should().Be(RefundStatus.Completed);
		persistedRecord.Should().NotBeNull();
		persistedRecord!.Reason.Should().Be(RefundWorkflowConstants.DefaultReasonPlaceholder);
	}

	[Fact]
	public async Task ExecuteAsync_WhenPersistenceThrows_ReturnsSafeCommitFailureWithoutDiagnosticDetails()
	{
		var staffId = Guid.NewGuid();
		var cartId = Guid.NewGuid();
		var now = DateTime.UtcNow;

		var sut = BuildSut(
			session: new CurrentSession(staffId, StaffRole.Manager, 1, 1),
			allowInitiate: true,
			allowApprove: true,
			cart: CartSession.Create(cartId, null, staffId, now),
			attempts:
			[
				CheckoutPaymentAttempt.Create(
					Guid.NewGuid(),
					cartId,
					3000,
					"USD",
					CheckoutPaymentAuthorizationStatus.Approved,
					null,
					null,
					now)
			]);

		sut.RefundRepository
			.Setup(x => x.AddAsyncWithBalanceCheckAsync(It.IsAny<RefundRecord>(), It.IsAny<long>(), It.IsAny<CancellationToken>()))
			.ThrowsAsync(new Exception("SQL deadlock details"));

		var command = new SubmitRefundCommand(
			cartId,
			1000,
			"Customer return",
			RefundPath.Direct,
			new OperationContext(Guid.NewGuid(), Guid.NewGuid(), null, now));

		var result = await sut.UseCase.ExecuteAsync(command);

		result.IsSuccess.Should().BeFalse();
		result.ErrorCode.Should().Be(RefundWorkflowConstants.ErrorCommitFailed);
		result.UserMessage.Should().Be(RefundWorkflowConstants.SafeCommitFailedMessage);
		result.DiagnosticMessage.Should().BeNull();
	}

	[Fact]
	public async Task ExecuteAsync_WhenApprovalPathWithProvidedReason_SucceedsWithPendingApproval()
	{
		var staffId = Guid.NewGuid();
		var cartId = Guid.NewGuid();
		var now = DateTime.UtcNow;

		var sut = BuildSut(
			session: new CurrentSession(staffId, StaffRole.Cashier, 1, 1),
			allowInitiate: true,
			allowApprove: false,
			cart: CartSession.Create(cartId, null, staffId, now),
			attempts:
			[
				CheckoutPaymentAttempt.Create(
					Guid.NewGuid(),
					cartId,
					3000,
					"USD",
					CheckoutPaymentAuthorizationStatus.Approved,
					null,
					null,
					now)
			]);

		var command = new SubmitRefundCommand(
			cartId,
			1000,
			"Customer requested refund",
			RefundPath.ApprovalRequired,
			new OperationContext(Guid.NewGuid(), Guid.NewGuid(), null, now));

		var result = await sut.UseCase.ExecuteAsync(command);

		result.IsSuccess.Should().BeTrue();
		result.Payload!.Status.Should().Be(RefundStatus.PendingApproval);
	}

	private static SutFixture BuildSut(
		CurrentSession? session = null,
		bool allowInitiate = true,
		bool allowApprove = true,
		CartSession? cart = null,
		IReadOnlyList<CheckoutPaymentAttempt>? attempts = null,
		RefundRecord? existingByOperation = null)
	{
		var cartRepository = new Mock<ICartSessionRepository>();
		cartRepository.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(cart);

		var attemptRepository = new Mock<ICheckoutPaymentAttemptRepository>();
		attemptRepository.Setup(x => x.ListByCartSessionAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(attempts ?? []);

		var refundRepository = new Mock<IRefundRepository>();
		refundRepository.Setup(x => x.GetByOperationIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync((Guid operationId, CancellationToken _) =>
				existingByOperation is not null && existingByOperation.OperationId == operationId ? existingByOperation : null);
		refundRepository.Setup(x => x.SumCompletedAmountByCartSessionAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(0);
		refundRepository.Setup(x => x.AddAsync(It.IsAny<RefundRecord>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync((RefundRecord record, CancellationToken _) => record);
                refundRepository.Setup(x => x.AddAsyncWithBalanceCheckAsync(It.IsAny<RefundRecord>(), It.IsAny<long>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync((RefundRecord record, long _, CancellationToken _) => record);

		var currentSessionService = new Mock<ICurrentSessionService>();
		currentSessionService.Setup(x => x.GetCurrent()).Returns(session ?? new CurrentSession(Guid.NewGuid(), StaffRole.Manager, 1, 1));

		var authorization = new Mock<IAuthorizationPolicyService>();
		authorization.Setup(x => x.HasPermission(It.IsAny<StaffRole>(), RolePermissions.CheckoutRefundInitiate)).Returns(allowInitiate);
		authorization.Setup(x => x.HasPermission(It.IsAny<StaffRole>(), RolePermissions.CheckoutRefundApprove)).Returns(allowApprove);

		var operationLogRepository = new Mock<IOperationLogRepository>();
		operationLogRepository.Setup(x => x.AppendAsync(
			It.IsAny<string>(),
			It.IsAny<string>(),
			It.IsAny<object>(),
			It.IsAny<OperationContext>(),
			It.IsAny<int>(),
			It.IsAny<CancellationToken>()))
			.ReturnsAsync(new OperationLogEntry());

		var clock = new Mock<IUtcClock>();
		clock.Setup(x => x.UtcNow).Returns(DateTime.UtcNow);

		var useCase = new SubmitRefundUseCase(
			cartRepository.Object,
			attemptRepository.Object,
			refundRepository.Object,
			currentSessionService.Object,
			authorization.Object,
			operationLogRepository.Object,
			clock.Object,
			new Mock<ILogger<SubmitRefundUseCase>>().Object);

		return new SutFixture(useCase, refundRepository, operationLogRepository);
	}

	private sealed record SutFixture(
		SubmitRefundUseCase UseCase,
		Mock<IRefundRepository> RefundRepository,
		Mock<IOperationLogRepository> OperationLogRepository);
}



