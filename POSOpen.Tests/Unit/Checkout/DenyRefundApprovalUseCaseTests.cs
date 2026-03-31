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

public sealed class DenyRefundApprovalUseCaseTests
{
	[Fact]
	public async Task ExecuteAsync_WhenPendingApprovalAndManager_DeniesAndAppendsAudit()
	{
		var refund = RefundRecord.Create(
			Guid.NewGuid(),
			Guid.NewGuid(),
			Guid.NewGuid(),
			RefundStatus.PendingApproval,
			RefundPath.ApprovalRequired,
			1200,
			"USD",
			"Original request",
			Guid.NewGuid(),
			StaffRole.Cashier.ToString(),
			DateTime.UtcNow);
		var managerId = Guid.NewGuid();
		var now = DateTime.UtcNow;

		var refundRepository = new Mock<IRefundRepository>();
		refundRepository.Setup(x => x.GetByIdAsync(refund.Id, It.IsAny<CancellationToken>())).ReturnsAsync(refund);
		refundRepository.Setup(x => x.UpdateAsync(It.IsAny<RefundRecord>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync((RefundRecord updated, CancellationToken _) => updated);

		var currentSession = new Mock<ICurrentSessionService>();
		currentSession.Setup(x => x.GetCurrent()).Returns(new CurrentSession(managerId, StaffRole.Manager, 1, 1));

		var authorization = new Mock<IAuthorizationPolicyService>();
		authorization.Setup(x => x.HasPermission(StaffRole.Manager, RolePermissions.CheckoutRefundApprove)).Returns(true);

		var operationLogRepository = new Mock<IOperationLogRepository>();
		operationLogRepository.Setup(x => x.AppendAsync(
			It.IsAny<string>(),
			It.IsAny<string>(),
			It.IsAny<object>(),
			It.IsAny<OperationContext>(),
			It.IsAny<int>(),
			It.IsAny<CancellationToken>())).ReturnsAsync(new OperationLogEntry());

		var clock = new Mock<IUtcClock>();
		clock.Setup(x => x.UtcNow).Returns(now);

		var sut = new DenyRefundApprovalUseCase(
			refundRepository.Object,
			currentSession.Object,
			authorization.Object,
			operationLogRepository.Object,
			clock.Object,
			new Mock<ILogger<DenyRefundApprovalUseCase>>().Object);

		var command = new DenyRefundApprovalCommand(
			refund.Id,
			"Policy mismatch",
			new OperationContext(Guid.NewGuid(), Guid.NewGuid(), null, now));

		var result = await sut.ExecuteAsync(command);

		result.IsSuccess.Should().BeTrue();
		result.Payload!.Status.Should().Be(RefundStatus.ApprovalDenied);
		refundRepository.Verify(x => x.UpdateAsync(It.Is<RefundRecord>(r =>
			r.Id == refund.Id &&
			r.Status == RefundStatus.ApprovalDenied &&
			r.ActorStaffId == managerId &&
			r.Reason == "Policy mismatch"), It.IsAny<CancellationToken>()), Times.Once);
		operationLogRepository.Verify(x => x.AppendAsync(
			SecurityAuditEventTypes.RefundApprovalDenied,
			refund.CartSessionId.ToString(),
			It.IsAny<object>(),
			command.Context,
			It.IsAny<int>(),
			It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task ExecuteAsync_WhenRefundNotPendingApproval_ReturnsFailure()
	{
		var refund = RefundRecord.Create(
			Guid.NewGuid(),
			Guid.NewGuid(),
			Guid.NewGuid(),
			RefundStatus.Completed,
			RefundPath.Direct,
			1200,
			"USD",
			"Original request",
			Guid.NewGuid(),
			StaffRole.Manager.ToString(),
			DateTime.UtcNow,
			DateTime.UtcNow);

		var refundRepository = new Mock<IRefundRepository>();
		refundRepository.Setup(x => x.GetByIdAsync(refund.Id, It.IsAny<CancellationToken>())).ReturnsAsync(refund);

		var currentSession = new Mock<ICurrentSessionService>();
		currentSession.Setup(x => x.GetCurrent()).Returns(new CurrentSession(Guid.NewGuid(), StaffRole.Manager, 1, 1));

		var authorization = new Mock<IAuthorizationPolicyService>();
		authorization.Setup(x => x.HasPermission(StaffRole.Manager, RolePermissions.CheckoutRefundApprove)).Returns(true);

		var sut = new DenyRefundApprovalUseCase(
			refundRepository.Object,
			currentSession.Object,
			authorization.Object,
			new Mock<IOperationLogRepository>().Object,
			new Mock<IUtcClock>().Object,
			new Mock<ILogger<DenyRefundApprovalUseCase>>().Object);

		var result = await sut.ExecuteAsync(new DenyRefundApprovalCommand(
			refund.Id,
			"Policy mismatch",
			new OperationContext(Guid.NewGuid(), Guid.NewGuid(), null, DateTime.UtcNow)));

		result.IsSuccess.Should().BeFalse();
		result.ErrorCode.Should().Be(RefundWorkflowConstants.ErrorApprovalStateInvalid);
	}
}
