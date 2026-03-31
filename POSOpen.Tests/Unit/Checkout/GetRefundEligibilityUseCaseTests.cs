using FluentAssertions;
using Moq;
using POSOpen.Application.Abstractions.Repositories;
using POSOpen.Application.Abstractions.Security;
using POSOpen.Application.Security;
using POSOpen.Application.UseCases.Checkout;
using POSOpen.Domain.Entities;
using POSOpen.Domain.Enums;

namespace POSOpen.Tests.Unit.Checkout;

public sealed class GetRefundEligibilityUseCaseTests
{
	[Fact]
	public async Task ExecuteAsync_CashierWithInitiateOnly_ReturnsApprovalPathOnly()
	{
		var cartId = Guid.NewGuid();
		var staffId = Guid.NewGuid();
		var now = DateTime.UtcNow;

		var cartRepo = new Mock<ICartSessionRepository>();
		cartRepo.Setup(x => x.GetByIdAsync(cartId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(CartSession.Create(cartId, null, staffId, now));

		var paymentRepo = new Mock<ICheckoutPaymentAttemptRepository>();
		paymentRepo.Setup(x => x.ListByCartSessionAsync(cartId, It.IsAny<CancellationToken>()))
			.ReturnsAsync([
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

		var refundRepo = new Mock<IRefundRepository>();
		refundRepo.Setup(x => x.SumCompletedAmountByCartSessionAsync(cartId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(0);

		var currentSession = new Mock<ICurrentSessionService>();
		currentSession.Setup(x => x.GetCurrent())
			.Returns(new CurrentSession(staffId, StaffRole.Cashier, 1, 1));

		var auth = new Mock<IAuthorizationPolicyService>();
		auth.Setup(x => x.HasPermission(StaffRole.Cashier, RolePermissions.CheckoutRefundInitiate)).Returns(true);
		auth.Setup(x => x.HasPermission(StaffRole.Cashier, RolePermissions.CheckoutRefundApprove)).Returns(false);

		var sut = new GetRefundEligibilityUseCase(
			cartRepo.Object,
			paymentRepo.Object,
			refundRepo.Object,
			currentSession.Object,
			auth.Object);

		var result = await sut.ExecuteAsync(cartId);

		result.IsSuccess.Should().BeTrue();
		result.Payload.Should().NotBeNull();
		result.Payload!.IsEligible.Should().BeTrue();
		result.Payload.AllowedPaths.Should().ContainSingle().Which.Should().Be(RefundPath.ApprovalRequired);
	}

	[Fact]
	public async Task ExecuteAsync_WhenNoApprovedAttempt_ReturnsNotEligible()
	{
		var cartId = Guid.NewGuid();
		var cartRepo = new Mock<ICartSessionRepository>();
		cartRepo.Setup(x => x.GetByIdAsync(cartId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(CartSession.Create(cartId, null, Guid.NewGuid(), DateTime.UtcNow));

		var paymentRepo = new Mock<ICheckoutPaymentAttemptRepository>();
		paymentRepo.Setup(x => x.ListByCartSessionAsync(cartId, It.IsAny<CancellationToken>()))
			.ReturnsAsync([]);

		var refundRepo = new Mock<IRefundRepository>();
		var currentSession = new Mock<ICurrentSessionService>();
		currentSession.Setup(x => x.GetCurrent())
			.Returns(new CurrentSession(Guid.NewGuid(), StaffRole.Manager, 1, 1));

		var auth = new Mock<IAuthorizationPolicyService>();
		auth.Setup(x => x.HasPermission(StaffRole.Manager, RolePermissions.CheckoutRefundInitiate)).Returns(true);
		auth.Setup(x => x.HasPermission(StaffRole.Manager, RolePermissions.CheckoutRefundApprove)).Returns(true);

		var sut = new GetRefundEligibilityUseCase(
			cartRepo.Object,
			paymentRepo.Object,
			refundRepo.Object,
			currentSession.Object,
			auth.Object);

		var result = await sut.ExecuteAsync(cartId);

		result.IsSuccess.Should().BeTrue();
		result.Payload!.IsEligible.Should().BeFalse();
		result.Payload.IneligibilityReasonCode.Should().Be(RefundWorkflowConstants.ErrorNotEligible);
	}

	[Fact]
	public async Task ExecuteAsync_WhenApprovedAmountsSumToZero_ReturnsNotEligible()
	{
		var cartId = Guid.NewGuid();
		var cartRepo = new Mock<ICartSessionRepository>();
		cartRepo.Setup(x => x.GetByIdAsync(cartId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(CartSession.Create(cartId, null, Guid.NewGuid(), DateTime.UtcNow));

		var paymentRepo = new Mock<ICheckoutPaymentAttemptRepository>();
		paymentRepo.Setup(x => x.ListByCartSessionAsync(cartId, It.IsAny<CancellationToken>()))
			.ReturnsAsync([
				CheckoutPaymentAttempt.Create(
					Guid.NewGuid(),
					cartId,
					0,
					"USD",
					CheckoutPaymentAuthorizationStatus.Approved,
					null,
					null,
					DateTime.UtcNow)
			]);

		var refundRepo = new Mock<IRefundRepository>();
		var currentSession = new Mock<ICurrentSessionService>();
		currentSession.Setup(x => x.GetCurrent())
			.Returns(new CurrentSession(Guid.NewGuid(), StaffRole.Manager, 1, 1));

		var auth = new Mock<IAuthorizationPolicyService>();
		auth.Setup(x => x.HasPermission(StaffRole.Manager, RolePermissions.CheckoutRefundInitiate)).Returns(true);
		auth.Setup(x => x.HasPermission(StaffRole.Manager, RolePermissions.CheckoutRefundApprove)).Returns(true);

		var sut = new GetRefundEligibilityUseCase(
			cartRepo.Object,
			paymentRepo.Object,
			refundRepo.Object,
			currentSession.Object,
			auth.Object);

		var result = await sut.ExecuteAsync(cartId);

		result.IsSuccess.Should().BeTrue();
		result.Payload!.IsEligible.Should().BeFalse();
		result.Payload.IneligibilityReasonCode.Should().Be(RefundWorkflowConstants.ErrorNotEligible);
	}

	[Fact]
	public async Task ExecuteAsync_WhenRoleLacksInitiatePermission_ReturnsFailure()
	{
		var cartRepo = new Mock<ICartSessionRepository>();
		var paymentRepo = new Mock<ICheckoutPaymentAttemptRepository>();
		var refundRepo = new Mock<IRefundRepository>();
		var currentSession = new Mock<ICurrentSessionService>();
		currentSession.Setup(x => x.GetCurrent())
			.Returns(new CurrentSession(Guid.NewGuid(), StaffRole.Cashier, 1, 1));

		var auth = new Mock<IAuthorizationPolicyService>();
		auth.Setup(x => x.HasPermission(StaffRole.Cashier, RolePermissions.CheckoutRefundInitiate)).Returns(false);

		var sut = new GetRefundEligibilityUseCase(
			cartRepo.Object,
			paymentRepo.Object,
			refundRepo.Object,
			currentSession.Object,
			auth.Object);

		var result = await sut.ExecuteAsync(Guid.NewGuid());

		result.IsSuccess.Should().BeFalse();
		result.ErrorCode.Should().Be(RefundWorkflowConstants.ErrorAuthForbidden);
	}
}