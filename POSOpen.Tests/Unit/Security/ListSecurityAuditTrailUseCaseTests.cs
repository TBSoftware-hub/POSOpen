using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using POSOpen.Application.Abstractions.Repositories;
using POSOpen.Application.Abstractions.Security;
using POSOpen.Application.Security;
using POSOpen.Application.UseCases.Security;
using POSOpen.Domain.Entities;
using POSOpen.Domain.Enums;
using POSOpen.Shared.Operational;

namespace POSOpen.Tests.Unit.Security;

public sealed class ListSecurityAuditTrailUseCaseTests
{
	private readonly Mock<IOperationLogRepository> _mockRepository = new();
	private readonly Mock<ICurrentSessionService> _mockCurrentSessionService = new();
	private readonly Mock<IAuthorizationPolicyService> _mockAuthorizationPolicyService = new();
	private readonly Mock<ILogger<ListSecurityAuditTrailUseCase>> _mockLogger = new();

	private ListSecurityAuditTrailUseCase CreateUseCase() =>
		new(_mockRepository.Object, _mockCurrentSessionService.Object,
			_mockAuthorizationPolicyService.Object, _mockLogger.Object);

	private OperationContext MakeContext() =>
		new(Guid.NewGuid(), Guid.NewGuid(), null, DateTime.UtcNow);

	private void SetupRepoAppend() =>
		_mockRepository
			.Setup(r => r.AppendAsync(
				It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>(),
				It.IsAny<OperationContext>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(new OperationLogEntry());

	[Theory]
	[InlineData(StaffRole.Owner)]
	[InlineData(StaffRole.Admin)]
	public async Task ExecuteAsync_WithAuthorizedRole_ReturnsSuccessWithRecords(StaffRole role)
	{
		// Arrange
		var staffId = Guid.NewGuid();
		var session = new CurrentSession(staffId, role, 1, 1);
		_mockCurrentSessionService.Setup(s => s.GetCurrent()).Returns(session);
		_mockAuthorizationPolicyService
			.Setup(a => a.HasPermission(role, RolePermissions.SecurityAuditRead))
			.Returns(true);

		var now = DateTime.UtcNow;
		var entries = new List<OperationLogEntry>
		{
			new()
			{
				Id = Guid.NewGuid(),
				EventType = SecurityAuditEventTypes.StaffAccountCreated,
				AggregateId = Guid.NewGuid().ToString(),
				OperationId = Guid.NewGuid(),
				CorrelationId = Guid.NewGuid(),
				OccurredUtc = now.AddMinutes(-10),
				RecordedUtc = now.AddMinutes(-10),
				PayloadJson = "{}"
			},
			new()
			{
				Id = Guid.NewGuid(),
				EventType = SecurityAuditEventTypes.StaffRoleAssigned,
				AggregateId = Guid.NewGuid().ToString(),
				OperationId = Guid.NewGuid(),
				CorrelationId = Guid.NewGuid(),
				OccurredUtc = now.AddMinutes(-5),
				RecordedUtc = now.AddMinutes(-5),
				PayloadJson = "{}"
			}
		};

		_mockRepository
			.Setup(r => r.ListByEventTypesAsync(
				SecurityAuditEventTypes.SecurityCriticalScope,
				It.IsAny<CancellationToken>()))
			.ReturnsAsync(entries);

		var useCase = CreateUseCase();

		// Act
		var result = await useCase.ExecuteAsync(MakeContext());

		// Assert
		result.IsSuccess.Should().BeTrue();
		result.Payload.Should().NotBeNull();
		result.Payload!.Should().HaveCount(2);
		result.Payload[0].EventType.Should().Be(SecurityAuditEventTypes.StaffAccountCreated);
		result.Payload[1].EventType.Should().Be(SecurityAuditEventTypes.StaffRoleAssigned);
	}

	[Theory]
	[InlineData(StaffRole.Manager)]
	[InlineData(StaffRole.Cashier)]
	public async Task ExecuteAsync_WithUnauthorizedRole_ReturnsAuthForbiddenAndLogsAccessDenied(StaffRole role)
	{
		// Arrange
		var staffId = Guid.NewGuid();
		var session = new CurrentSession(staffId, role, 1, 1);
		_mockCurrentSessionService.Setup(s => s.GetCurrent()).Returns(session);
		_mockAuthorizationPolicyService
			.Setup(a => a.HasPermission(role, RolePermissions.SecurityAuditRead))
			.Returns(false);
		SetupRepoAppend();

		var useCase = CreateUseCase();

		// Act
		var result = await useCase.ExecuteAsync(MakeContext());

		// Assert
		result.IsSuccess.Should().BeFalse();
		result.ErrorCode.Should().Be(ListSecurityAuditTrailConstants.ErrorAuthForbidden);
		result.UserMessage.Should().Be(ListSecurityAuditTrailConstants.SafeAuthForbiddenMessage);

		_mockRepository.Verify(
			r => r.AppendAsync(
				SecurityAuditEventTypes.SecurityAuditAccessDenied,
				staffId.ToString(),
				It.IsAny<object>(),
				It.IsAny<OperationContext>(),
				1,
				It.IsAny<CancellationToken>()),
			Times.Once);
	}

	[Fact]
	public async Task ExecuteAsync_WithNoActiveSession_ReturnsAuthForbidden()
	{
		// Arrange
		_mockCurrentSessionService.Setup(s => s.GetCurrent()).Returns((CurrentSession?)null);

		var useCase = CreateUseCase();

		// Act
		var result = await useCase.ExecuteAsync(MakeContext());

		// Assert
		result.IsSuccess.Should().BeFalse();
		result.ErrorCode.Should().Be(ListSecurityAuditTrailConstants.ErrorAuthForbidden);
		result.UserMessage.Should().Be(ListSecurityAuditTrailConstants.SafeAuthForbiddenMessage);

		_mockRepository.Verify(
			r => r.AppendAsync(
				It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>(),
				It.IsAny<OperationContext>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
			Times.Never);
	}

	[Fact]
	public async Task ExecuteAsync_WhenRepositoryThrows_ReturnsAuditTrailUnavailable()
	{
		// Arrange
		var staffId = Guid.NewGuid();
		var session = new CurrentSession(staffId, StaffRole.Owner, 1, 1);
		_mockCurrentSessionService.Setup(s => s.GetCurrent()).Returns(session);
		_mockAuthorizationPolicyService
			.Setup(a => a.HasPermission(StaffRole.Owner, RolePermissions.SecurityAuditRead))
			.Returns(true);

		_mockRepository
			.Setup(r => r.ListByEventTypesAsync(
				It.IsAny<IReadOnlyList<string>>(),
				It.IsAny<CancellationToken>()))
			.ThrowsAsync(new InvalidOperationException("DB failure"));

		var useCase = CreateUseCase();

		// Act
		var result = await useCase.ExecuteAsync(MakeContext());

		// Assert
		result.IsSuccess.Should().BeFalse();
		result.ErrorCode.Should().Be(ListSecurityAuditTrailConstants.ErrorAuditTrailUnavailable);
		result.UserMessage.Should().Be(ListSecurityAuditTrailConstants.SafeAuditTrailUnavailableMessage);
	}

	[Fact]
	public async Task ExecuteAsync_WithAuthorizedOwner_DoesNotAppendDenialEvent()
	{
		// Arrange
		var staffId = Guid.NewGuid();
		var session = new CurrentSession(staffId, StaffRole.Owner, 1, 1);
		_mockCurrentSessionService.Setup(s => s.GetCurrent()).Returns(session);
		_mockAuthorizationPolicyService
			.Setup(a => a.HasPermission(StaffRole.Owner, RolePermissions.SecurityAuditRead))
			.Returns(true);

		_mockRepository
			.Setup(r => r.ListByEventTypesAsync(
				It.IsAny<IReadOnlyList<string>>(),
				It.IsAny<CancellationToken>()))
			.ReturnsAsync(new List<OperationLogEntry>());

		var useCase = CreateUseCase();

		// Act
		await useCase.ExecuteAsync(MakeContext());

		// Assert — no AppendAsync call for denial when access is authorized
		_mockRepository.Verify(
			r => r.AppendAsync(
				SecurityAuditEventTypes.SecurityAuditAccessDenied,
				It.IsAny<string>(),
				It.IsAny<object>(),
				It.IsAny<OperationContext>(),
				It.IsAny<int>(),
				It.IsAny<CancellationToken>()),
			Times.Never);
	}

	[Fact]
	public async Task ExecuteAsync_QueriesOnlySecurityCriticalScopeEventTypes()
	{
		// Arrange
		var staffId = Guid.NewGuid();
		var session = new CurrentSession(staffId, StaffRole.Admin, 1, 1);
		_mockCurrentSessionService.Setup(s => s.GetCurrent()).Returns(session);
		_mockAuthorizationPolicyService
			.Setup(a => a.HasPermission(StaffRole.Admin, RolePermissions.SecurityAuditRead))
			.Returns(true);

		IReadOnlyList<string>? capturedEventTypes = null;
		_mockRepository
			.Setup(r => r.ListByEventTypesAsync(
				It.IsAny<IReadOnlyList<string>>(),
				It.IsAny<CancellationToken>()))
			.Callback<IReadOnlyList<string>, CancellationToken>((types, _) => capturedEventTypes = types)
			.ReturnsAsync(new List<OperationLogEntry>());

		var useCase = CreateUseCase();

		// Act
		await useCase.ExecuteAsync(MakeContext());

		// Assert
		capturedEventTypes.Should().NotBeNull();
		capturedEventTypes.Should().BeEquivalentTo(SecurityAuditEventTypes.SecurityCriticalScope);
	}
}
