using FluentAssertions;
using Moq;
using POSOpen.Application.Abstractions.Repositories;
using POSOpen.Application.Abstractions.Security;
using POSOpen.Application.Abstractions.Services;
using POSOpen.Application.Results;
using POSOpen.Application.Security;
using POSOpen.Application.UseCases.Checkout;
using POSOpen.Domain.Entities;
using POSOpen.Domain.Enums;
using POSOpen.Features.Checkout.ViewModels;
using POSOpen.Shared.Operational;

namespace POSOpen.Tests.Unit.Checkout;

public sealed class RefundWorkflowViewModelTests
{
	[Fact]
	public async Task InitializeCommand_WhenCashierOnlyInitiate_ShowsApprovalOnlyMode()
	{
		var cartId = Guid.NewGuid();
		var vm = BuildViewModel(cartId, StaffRole.Cashier, allowApprove: false);

		vm.CartSessionIdParam = cartId.ToString();
		await vm.InitializeCommand.ExecuteAsync(null);

		vm.IsEligible.Should().BeTrue();
		vm.CanUseApprovalPath.Should().BeTrue();
		vm.CanUseDirectPath.Should().BeFalse();
		vm.IsApprovalOnlyMode.Should().BeTrue();
	}

	[Fact]
	public async Task SubmitCommand_WhenAmountNotNumeric_ShowsValidationError()
	{
		var cartId = Guid.NewGuid();
		var vm = BuildViewModel(cartId, StaffRole.Manager, allowApprove: true);
		vm.CartSessionIdParam = cartId.ToString();

		await vm.InitializeCommand.ExecuteAsync(null);
		vm.AmountCentsInput = "not-a-number";

		await vm.SubmitCommand.ExecuteAsync(null);

		vm.HasError.Should().BeTrue();
		vm.ErrorMessage.Should().Contain("cents");
	}

	[Fact]
	public async Task SubmitCommand_WhenManagerDirectRefund_Completes()
	{
		var cartId = Guid.NewGuid();
		var vm = BuildViewModel(cartId, StaffRole.Manager, allowApprove: true);
		vm.CartSessionIdParam = cartId.ToString();

		await vm.InitializeCommand.ExecuteAsync(null);
		vm.AmountCentsInput = "800";
		vm.SelectedPath = RefundPath.Direct;
		vm.Reason = "Customer return";

		await vm.SubmitCommand.ExecuteAsync(null);

		vm.IsCompleted.Should().BeTrue();
		vm.IsPendingApproval.Should().BeFalse();
	}

	private static RefundWorkflowViewModel BuildViewModel(Guid cartId, StaffRole role, bool allowApprove)
	{
		var cart = CartSession.Create(cartId, null, Guid.NewGuid(), DateTime.UtcNow);

		var cartRepo = new Mock<ICartSessionRepository>();
		cartRepo.Setup(x => x.GetByIdAsync(cartId, It.IsAny<CancellationToken>())).ReturnsAsync(cart);

		var paymentRepo = new Mock<ICheckoutPaymentAttemptRepository>();
		paymentRepo.Setup(x => x.ListByCartSessionAsync(cartId, It.IsAny<CancellationToken>()))
			.ReturnsAsync([
				CheckoutPaymentAttempt.Create(
					Guid.NewGuid(),
					cartId,
					2000,
					"USD",
					CheckoutPaymentAuthorizationStatus.Approved,
					null,
					null,
					DateTime.UtcNow)
			]);

		var refundRepo = new Mock<IRefundRepository>();
		refundRepo.Setup(x => x.SumCompletedAmountByCartSessionAsync(cartId, It.IsAny<CancellationToken>())).ReturnsAsync(0);
		refundRepo.Setup(x => x.GetByOperationIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((RefundRecord?)null);
		refundRepo.Setup(x => x.AddAsync(It.IsAny<RefundRecord>(), It.IsAny<CancellationToken>())).ReturnsAsync((RefundRecord r, CancellationToken _) => r);

		var session = new Mock<ICurrentSessionService>();
		session.Setup(x => x.GetCurrent()).Returns(new CurrentSession(Guid.NewGuid(), role, 1, 1));

		var auth = new Mock<IAuthorizationPolicyService>();
		auth.Setup(x => x.HasPermission(role, RolePermissions.CheckoutRefundInitiate)).Returns(true);
		auth.Setup(x => x.HasPermission(role, RolePermissions.CheckoutRefundApprove)).Returns(allowApprove);

		var operationLogRepo = new Mock<IOperationLogRepository>();
		operationLogRepo.Setup(x => x.AppendAsync(
			It.IsAny<string>(),
			It.IsAny<string>(),
			It.IsAny<object>(),
			It.IsAny<OperationContext>(),
			It.IsAny<int>(),
			It.IsAny<CancellationToken>())).ReturnsAsync(new OperationLogEntry());

		var clock = new Mock<IUtcClock>();
		clock.Setup(x => x.UtcNow).Returns(DateTime.UtcNow);

		var getEligibility = new GetRefundEligibilityUseCase(
			cartRepo.Object,
			paymentRepo.Object,
			refundRepo.Object,
			session.Object,
			auth.Object);

		var submitRefund = new SubmitRefundUseCase(
			cartRepo.Object,
			paymentRepo.Object,
			refundRepo.Object,
			session.Object,
			auth.Object,
			operationLogRepo.Object,
			clock.Object,
			new Mock<Microsoft.Extensions.Logging.ILogger<SubmitRefundUseCase>>().Object);

		var operationContextFactory = new Mock<IOperationContextFactory>();
		operationContextFactory.Setup(x => x.CreateRoot(It.IsAny<Guid?>()))
			.Returns(new OperationContext(Guid.NewGuid(), Guid.NewGuid(), null, DateTime.UtcNow));

		var checkoutUi = new Mock<ICheckoutUiService>();
		checkoutUi.Setup(x => x.NavigateToCheckoutCompletionAsync(It.IsAny<Guid>())).Returns(Task.CompletedTask);

		return new RefundWorkflowViewModel(
			getEligibility,
			submitRefund,
			operationContextFactory.Object,
			checkoutUi.Object);
	}
}