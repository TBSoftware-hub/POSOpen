using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using POSOpen.Application.Abstractions.Repositories;
using POSOpen.Application.Abstractions.Security;
using POSOpen.Application.Security;
using POSOpen.Application.UseCases.Security;
using POSOpen.Domain.Enums;
using POSOpen.Shared.Operational;

namespace POSOpen.Tests.Unit.Security;

public sealed class SubmitOverrideUseCaseTests
{
	private readonly Mock<ICurrentSessionService> _mockCurrentSessionService = new();
	private readonly Mock<IAuthorizationPolicyService> _mockAuthorizationPolicyService = new();
	private readonly Mock<IOperationLogRepository> _mockOperationLogRepository = new();
	private readonly Mock<ILogger<SubmitOverrideUseCase>> _mockLogger = new();

	private SubmitOverrideUseCase CreateUseCase()
	{
		return new SubmitOverrideUseCase(
			_mockCurrentSessionService.Object,
			_mockAuthorizationPolicyService.Object,
			_mockOperationLogRepository.Object,
			_mockLogger.Object);
	}

	[Fact]
	public async Task ExecuteAsync_WithValidReasonAndAuthorizedManager_SucceedsAndCommitsEvent()
	{
		// Arrange
		var staffId = Guid.NewGuid();
		var session = new CurrentSession(staffId, StaffRole.Manager, 1, 1);
		_mockCurrentSessionService.Setup(s => s.GetCurrent()).Returns(session);
		_mockAuthorizationPolicyService
			.Setup(a => a.HasPermission(StaffRole.Manager, RolePermissions.SecurityOverrideExecute))
			.Returns(true);

		var operationId = Guid.NewGuid();
		var context = new OperationContext(operationId, Guid.NewGuid(), null, DateTime.UtcNow);
		var command = new SubmitOverrideCommand("user.unlock", "staff-123", "Employee locked due to excessive failed attempts", context);

		_mockOperationLogRepository
			.Setup(o => o.AppendAsync(
				It.IsAny<string>(),
				It.IsAny<string>(),
				It.IsAny<object>(),
				It.IsAny<OperationContext>(),
				It.IsAny<int>(),
				It.IsAny<CancellationToken>()))
			.ReturnsAsync(new Domain.Entities.OperationLogEntry());

		var useCase = CreateUseCase();

		// Act
		var result = await useCase.ExecuteAsync(command);

		// Assert
		result.IsSuccess.Should().BeTrue();
		result.Payload.Should().NotBeNull();
		result.Payload.OperationId.Should().Be(operationId);
		result.Payload.ApprovedByStaffId.Should().Be(staffId);
		result.Payload.ApprovedByRole.Should().Be(StaffRole.Manager);

		_mockOperationLogRepository.Verify(
			o => o.AppendAsync(
				"OverrideActionCommitted",
				staffId.ToString(),
				It.IsAny<object>(),
				context,
				1,
				It.IsAny<CancellationToken>()),
			Times.Once);
	}

	[Fact]
	public async Task ExecuteAsync_WithValidReasonAndAuthorizedOwner_SucceedsAndCommitsEvent()
	{
		// Arrange
		var staffId = Guid.NewGuid();
		var session = new CurrentSession(staffId, StaffRole.Owner, 1, 1);
		_mockCurrentSessionService.Setup(s => s.GetCurrent()).Returns(session);
		_mockAuthorizationPolicyService
			.Setup(a => a.HasPermission(StaffRole.Owner, RolePermissions.SecurityOverrideExecute))
			.Returns(true);

		var operationId = Guid.NewGuid();
		var context = new OperationContext(operationId, Guid.NewGuid(), null, DateTime.UtcNow);
		var command = new SubmitOverrideCommand("manager.termination", "staff-456", "Immediate termination due to policy violation", context);

		_mockOperationLogRepository
			.Setup(o => o.AppendAsync(
				It.IsAny<string>(),
				It.IsAny<string>(),
				It.IsAny<object>(),
				It.IsAny<OperationContext>(),
				It.IsAny<int>(),
				It.IsAny<CancellationToken>()))
			.ReturnsAsync(new Domain.Entities.OperationLogEntry());

		var useCase = CreateUseCase();

		// Act
		var result = await useCase.ExecuteAsync(command);

		// Assert
		result.IsSuccess.Should().BeTrue();
		result.Payload.ApprovedByRole.Should().Be(StaffRole.Owner);
	}

	[Fact]
	public async Task ExecuteAsync_WithValidReasonAndAuthorizedAdmin_SucceedsAndCommitsEvent()
	{
		// Arrange
		var staffId = Guid.NewGuid();
		var session = new CurrentSession(staffId, StaffRole.Admin, 1, 1);
		_mockCurrentSessionService.Setup(s => s.GetCurrent()).Returns(session);
		_mockAuthorizationPolicyService
			.Setup(a => a.HasPermission(StaffRole.Admin, RolePermissions.SecurityOverrideExecute))
			.Returns(true);

		var operationId = Guid.NewGuid();
		var context = new OperationContext(operationId, Guid.NewGuid(), null, DateTime.UtcNow);
		var command = new SubmitOverrideCommand("system.reset", "location-789", "System reset for maintenance window", context);

		_mockOperationLogRepository
			.Setup(o => o.AppendAsync(
				It.IsAny<string>(),
				It.IsAny<string>(),
				It.IsAny<object>(),
				It.IsAny<OperationContext>(),
				It.IsAny<int>(),
				It.IsAny<CancellationToken>()))
			.ReturnsAsync(new Domain.Entities.OperationLogEntry());

		var useCase = CreateUseCase();

		// Act
		var result = await useCase.ExecuteAsync(command);

		// Assert
		result.IsSuccess.Should().BeTrue();
		result.Payload.ApprovedByRole.Should().Be(StaffRole.Admin);
	}

	[Fact]
	public async Task ExecuteAsync_WithMissingReason_FailsWithReasonRequiredError()
	{
		// Arrange
		var staffId = Guid.NewGuid();
		var session = new CurrentSession(staffId, StaffRole.Manager, 1, 1);
		_mockCurrentSessionService.Setup(s => s.GetCurrent()).Returns(session);
		_mockAuthorizationPolicyService
			.Setup(a => a.HasPermission(StaffRole.Manager, RolePermissions.SecurityOverrideExecute))
			.Returns(true);

		var context = new OperationContext(Guid.NewGuid(), Guid.NewGuid(), null, DateTime.UtcNow);
		var command = new SubmitOverrideCommand("user.unlock", "staff-123", null!, context);

		var useCase = CreateUseCase();

		// Act
		var result = await useCase.ExecuteAsync(command);

		// Assert
		result.IsSuccess.Should().BeFalse();
		result.ErrorCode.Should().Be(SubmitOverrideConstants.ErrorReasonRequired);
		result.UserMessage.Should().Be(SubmitOverrideConstants.SafeReasonRequiredMessage);
		
		_mockOperationLogRepository.Verify(
			o => o.AppendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>(), It.IsAny<OperationContext>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
			Times.Never);
	}

	[Fact]
	public async Task ExecuteAsync_WithEmptyReason_FailsWithReasonRequiredError()
	{
		// Arrange
		var staffId = Guid.NewGuid();
		var session = new CurrentSession(staffId, StaffRole.Manager, 1, 1);
		_mockCurrentSessionService.Setup(s => s.GetCurrent()).Returns(session);

		var context = new OperationContext(Guid.NewGuid(), Guid.NewGuid(), null, DateTime.UtcNow);
		var command = new SubmitOverrideCommand("user.unlock", "staff-123", "", context);

		var useCase = CreateUseCase();

		// Act
		var result = await useCase.ExecuteAsync(command);

		// Assert
		result.IsSuccess.Should().BeFalse();
		result.ErrorCode.Should().Be(SubmitOverrideConstants.ErrorReasonRequired);
	}

	[Fact]
	public async Task ExecuteAsync_WithWhitespaceOnlyReason_FailsWithReasonRequiredError()
	{
		// Arrange
		var staffId = Guid.NewGuid();
		var session = new CurrentSession(staffId, StaffRole.Manager, 1, 1);
		_mockCurrentSessionService.Setup(s => s.GetCurrent()).Returns(session);

		var context = new OperationContext(Guid.NewGuid(), Guid.NewGuid(), null, DateTime.UtcNow);
		var command = new SubmitOverrideCommand("user.unlock", "staff-123", "   \t\n  ", context);

		var useCase = CreateUseCase();

		// Act
		var result = await useCase.ExecuteAsync(command);

		// Assert
		result.IsSuccess.Should().BeFalse();
		result.ErrorCode.Should().Be(SubmitOverrideConstants.ErrorReasonRequired);
	}

	[Fact]
	public async Task ExecuteAsync_WithUnauthorizedCashierRole_FailsWithForbiddenError()
	{
		// Arrange
		var staffId = Guid.NewGuid();
		var session = new CurrentSession(staffId, StaffRole.Cashier, 1, 1);
		_mockCurrentSessionService.Setup(s => s.GetCurrent()).Returns(session);
		_mockAuthorizationPolicyService
			.Setup(a => a.HasPermission(StaffRole.Cashier, RolePermissions.SecurityOverrideExecute))
			.Returns(false);

		var context = new OperationContext(Guid.NewGuid(), Guid.NewGuid(), null, DateTime.UtcNow);
		var command = new SubmitOverrideCommand("user.unlock", "staff-123", "Valid reason", context);

		var useCase = CreateUseCase();

		// Act
		var result = await useCase.ExecuteAsync(command);

		// Assert
		result.IsSuccess.Should().BeFalse();
		result.ErrorCode.Should().Be(SubmitOverrideConstants.ErrorAuthForbidden);
		result.UserMessage.Should().Be(SubmitOverrideConstants.SafeAuthForbiddenMessage);
		
		_mockOperationLogRepository.Verify(
			o => o.AppendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>(), It.IsAny<OperationContext>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
			Times.Never);
	}

	[Fact]
	public async Task ExecuteAsync_WithNoActiveSession_FailsWithForbiddenError()
	{
		// Arrange
		_mockCurrentSessionService.Setup(s => s.GetCurrent()).Returns((CurrentSession?)null);

		var context = new OperationContext(Guid.NewGuid(), Guid.NewGuid(), null, DateTime.UtcNow);
		var command = new SubmitOverrideCommand("user.unlock", "staff-123", "Valid reason", context);

		var useCase = CreateUseCase();

		// Act
		var result = await useCase.ExecuteAsync(command);

		// Assert
		result.IsSuccess.Should().BeFalse();
		result.ErrorCode.Should().Be(SubmitOverrideConstants.ErrorAuthForbidden);
		
		_mockOperationLogRepository.Verify(
			o => o.AppendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>(), It.IsAny<OperationContext>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
			Times.Never);
	}

	[Fact]
	public async Task ExecuteAsync_WithMissingActionKey_FailsWithContextInvalidError()
	{
		// Arrange
		var staffId = Guid.NewGuid();
		var session = new CurrentSession(staffId, StaffRole.Manager, 1, 1);
		_mockCurrentSessionService.Setup(s => s.GetCurrent()).Returns(session);

		var context = new OperationContext(Guid.NewGuid(), Guid.NewGuid(), null, DateTime.UtcNow);
		var command = new SubmitOverrideCommand(string.Empty, "staff-123", "Valid reason", context);

		var useCase = CreateUseCase();

		// Act
		var result = await useCase.ExecuteAsync(command);

		// Assert
		result.IsSuccess.Should().BeFalse();
		result.ErrorCode.Should().Be(SubmitOverrideConstants.ErrorContextInvalid);
		result.UserMessage.Should().Be(SubmitOverrideConstants.SafeContextInvalidMessage);
	}

	[Fact]
	public async Task ExecuteAsync_WithMissingTargetReference_FailsWithContextInvalidError()
	{
		// Arrange
		var staffId = Guid.NewGuid();
		var session = new CurrentSession(staffId, StaffRole.Manager, 1, 1);
		_mockCurrentSessionService.Setup(s => s.GetCurrent()).Returns(session);

		var context = new OperationContext(Guid.NewGuid(), Guid.NewGuid(), null, DateTime.UtcNow);
		var command = new SubmitOverrideCommand("user.unlock", null!, "Valid reason", context);

		var useCase = CreateUseCase();

		// Act
		var result = await useCase.ExecuteAsync(command);

		// Assert
		result.IsSuccess.Should().BeFalse();
		result.ErrorCode.Should().Be(SubmitOverrideConstants.ErrorContextInvalid);
	}

	[Fact]
	public async Task ExecuteAsync_WhenRepositoryThrowsException_FailsWithCommitFailedError()
	{
		// Arrange
		var staffId = Guid.NewGuid();
		var session = new CurrentSession(staffId, StaffRole.Manager, 1, 1);
		_mockCurrentSessionService.Setup(s => s.GetCurrent()).Returns(session);
		_mockAuthorizationPolicyService
			.Setup(a => a.HasPermission(StaffRole.Manager, RolePermissions.SecurityOverrideExecute))
			.Returns(true);

		_mockOperationLogRepository
			.Setup(o => o.AppendAsync(
				It.IsAny<string>(),
				It.IsAny<string>(),
				It.IsAny<object>(),
				It.IsAny<OperationContext>(),
				It.IsAny<int>(),
				It.IsAny<CancellationToken>()))
			.ThrowsAsync(new InvalidOperationException("Database connection failed"));

		var context = new OperationContext(Guid.NewGuid(), Guid.NewGuid(), null, DateTime.UtcNow);
		var command = new SubmitOverrideCommand("user.unlock", "staff-123", "Valid reason", context);

		var useCase = CreateUseCase();

		// Act
		var result = await useCase.ExecuteAsync(command);

		// Assert
		result.IsSuccess.Should().BeFalse();
		result.ErrorCode.Should().Be(SubmitOverrideConstants.ErrorCommitFailed);
		result.UserMessage.Should().Be(SubmitOverrideConstants.SafeCommitFailedMessage);
	}
}
