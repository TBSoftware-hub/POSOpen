using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using POSOpen.Application.Abstractions.Repositories;
using POSOpen.Application.Abstractions.Security;
using POSOpen.Application.Security;
using POSOpen.Application.UseCases.Security;
using POSOpen.Domain.Enums;
using POSOpen.Shared.Operational;

namespace POSOpen.Tests.Integration.Security;

public sealed class SubmitOverrideIntegrationTests
{
	[Fact]
	public async Task ExecuteAsync_WithValidOverride_AppendsImmutableEventWithRequiredPayloadFields()
	{
		// Arrange
		var staffId = Guid.NewGuid();
		var operationId = Guid.NewGuid();
		var correlationId = Guid.NewGuid();
		var occurredUtc = DateTime.UtcNow;

		var session = new CurrentSession(staffId, StaffRole.Manager, 1, 1);

		var mockCurrentSessionService = new Mock<ICurrentSessionService>();
		mockCurrentSessionService.Setup(s => s.GetCurrent()).Returns(session);

		var mockAuthorizationPolicyService = new Mock<IAuthorizationPolicyService>();
		mockAuthorizationPolicyService
			.Setup(a => a.HasPermission(StaffRole.Manager, RolePermissions.SecurityOverrideExecute))
			.Returns(true);

		var appendedPayload = (object?)null;
		var appendedEventType = "";
		var appendedAggregateId = "";
		var appendedContext = (OperationContext?)null;

		var mockOperationLogRepository = new Mock<IOperationLogRepository>();
		mockOperationLogRepository
			.Setup(o => o.AppendAsync(
				It.IsAny<string>(),
				It.IsAny<string>(),
				It.IsAny<object>(),
				It.IsAny<OperationContext>(),
				It.IsAny<int>(),
				It.IsAny<CancellationToken>()))
			.Callback<string, string, object, OperationContext, int, CancellationToken>(
				(eventType, aggregateId, payload, context, version, ct) =>
				{
					appendedEventType = eventType;
					appendedAggregateId = aggregateId;
					appendedPayload = payload;
					appendedContext = context;
				})
			.ReturnsAsync(new Domain.Entities.OperationLogEntry());

		var mockLogger = new Mock<ILogger<SubmitOverrideUseCase>>();

		var useCase = new SubmitOverrideUseCase(
			mockCurrentSessionService.Object,
			mockAuthorizationPolicyService.Object,
			mockOperationLogRepository.Object,
			mockLogger.Object);

		var context = new OperationContext(operationId, correlationId, null, occurredUtc);
		var command = new SubmitOverrideCommand(
			"user.unlock",
			"staff-789",
			"Employee locked due to excessive failed sign-in attempts",
			context);

		// Act
		var result = await useCase.ExecuteAsync(command);

		// Assert
		result.IsSuccess.Should().BeTrue();

		// Verify event type
		appendedEventType.Should().Be("OverrideActionCommitted");

		// Verify aggregate ID (staff ID)
		appendedAggregateId.Should().Be(staffId.ToString());

		// Verify context passed through
		appendedContext.Should().NotBeNull();
		appendedContext!.OperationId.Should().Be(operationId);
		appendedContext.CorrelationId.Should().Be(correlationId);
		appendedContext.OccurredUtc.Should().Be(occurredUtc);

		// Verify payload contains all required immutable fields
		appendedPayload.Should().NotBeNull();
		var payloadType = appendedPayload!.GetType();

		// Check required payload properties
		var staffIdProperty = payloadType.GetProperty("StaffId");
		staffIdProperty.Should().NotBeNull();
		staffIdProperty!.GetValue(appendedPayload).Should().Be(staffId);

		var staffRoleProperty = payloadType.GetProperty("StaffRole");
		staffRoleProperty.Should().NotBeNull();
		staffRoleProperty!.GetValue(appendedPayload).Should().Be(StaffRole.Manager.ToString());

		var actionKeyProperty = payloadType.GetProperty("ActionKey");
		actionKeyProperty.Should().NotBeNull();
		actionKeyProperty!.GetValue(appendedPayload).Should().Be("user.unlock");

		var targetReferenceProperty = payloadType.GetProperty("TargetReference");
		targetReferenceProperty.Should().NotBeNull();
		targetReferenceProperty!.GetValue(appendedPayload).Should().Be("staff-789");

		var reasonProperty = payloadType.GetProperty("Reason");
		reasonProperty.Should().NotBeNull();
		reasonProperty!.GetValue(appendedPayload).Should().Be("Employee locked due to excessive failed sign-in attempts");

		var operationIdProperty = payloadType.GetProperty("OperationId");
		operationIdProperty.Should().NotBeNull();
		operationIdProperty!.GetValue(appendedPayload).Should().Be(operationId);

		var occurredUtcProperty = payloadType.GetProperty("OccurredUtc");
		occurredUtcProperty.Should().NotBeNull();
		occurredUtcProperty!.GetValue(appendedPayload).Should().Be(occurredUtc);
	}

	[Fact]
	public async Task ExecuteAsync_WithValidationFailure_DoesNotAppendEvent()
	{
		// Arrange
		var staffId = Guid.NewGuid();
		var session = new CurrentSession(staffId, StaffRole.Manager, 1, 1);

		var mockCurrentSessionService = new Mock<ICurrentSessionService>();
		mockCurrentSessionService.Setup(s => s.GetCurrent()).Returns(session);

		var mockAuthorizationPolicyService = new Mock<IAuthorizationPolicyService>();
		var mockOperationLogRepository = new Mock<IOperationLogRepository>();
		var mockLogger = new Mock<ILogger<SubmitOverrideUseCase>>();

		var useCase = new SubmitOverrideUseCase(
			mockCurrentSessionService.Object,
			mockAuthorizationPolicyService.Object,
			mockOperationLogRepository.Object,
			mockLogger.Object);

		var context = new OperationContext(Guid.NewGuid(), Guid.NewGuid(), null, DateTime.UtcNow);
		var command = new SubmitOverrideCommand("user.unlock", "staff-789", string.Empty, context);

		// Act
		var result = await useCase.ExecuteAsync(command);

		// Assert
		result.IsSuccess.Should().BeFalse();
		result.ErrorCode.Should().Be(SubmitOverrideConstants.ErrorReasonRequired);

		// Verify no event was appended
		mockOperationLogRepository.Verify(
			o => o.AppendAsync(
				It.IsAny<string>(),
				It.IsAny<string>(),
				It.IsAny<object>(),
				It.IsAny<OperationContext>(),
				It.IsAny<int>(),
				It.IsAny<CancellationToken>()),
			Times.Never);
	}

	[Fact]
	public async Task ExecuteAsync_WithAuthorizationFailure_DoesNotAppendEvent()
	{
		// Arrange
		var staffId = Guid.NewGuid();
		var session = new CurrentSession(staffId, StaffRole.Cashier, 1, 1);

		var mockCurrentSessionService = new Mock<ICurrentSessionService>();
		mockCurrentSessionService.Setup(s => s.GetCurrent()).Returns(session);

		var mockAuthorizationPolicyService = new Mock<IAuthorizationPolicyService>();
		mockAuthorizationPolicyService
			.Setup(a => a.HasPermission(StaffRole.Cashier, RolePermissions.SecurityOverrideExecute))
			.Returns(false);

		var mockOperationLogRepository = new Mock<IOperationLogRepository>();
		var mockLogger = new Mock<ILogger<SubmitOverrideUseCase>>();

		var useCase = new SubmitOverrideUseCase(
			mockCurrentSessionService.Object,
			mockAuthorizationPolicyService.Object,
			mockOperationLogRepository.Object,
			mockLogger.Object);

		var context = new OperationContext(Guid.NewGuid(), Guid.NewGuid(), null, DateTime.UtcNow);
		var command = new SubmitOverrideCommand("user.unlock", "staff-789", "Valid reason", context);

		// Act
		var result = await useCase.ExecuteAsync(command);

		// Assert
		result.IsSuccess.Should().BeFalse();
		result.ErrorCode.Should().Be(SubmitOverrideConstants.ErrorAuthForbidden);

		// Verify no event was appended
		mockOperationLogRepository.Verify(
			o => o.AppendAsync(
				It.IsAny<string>(),
				It.IsAny<string>(),
				It.IsAny<object>(),
				It.IsAny<OperationContext>(),
				It.IsAny<int>(),
				It.IsAny<CancellationToken>()),
			Times.Never);
	}

	[Fact]
	public async Task ExecuteAsync_WithWhitespaceReason_TrimsBeforeAppending()
	{
		// Arrange
		var staffId = Guid.NewGuid();
		var operationId = Guid.NewGuid();
		var session = new CurrentSession(staffId, StaffRole.Manager, 1, 1);

		var mockCurrentSessionService = new Mock<ICurrentSessionService>();
		mockCurrentSessionService.Setup(s => s.GetCurrent()).Returns(session);

		var mockAuthorizationPolicyService = new Mock<IAuthorizationPolicyService>();
		mockAuthorizationPolicyService
			.Setup(a => a.HasPermission(StaffRole.Manager, RolePermissions.SecurityOverrideExecute))
			.Returns(true);

		var appendedPayload = (object?)null;
		var mockOperationLogRepository = new Mock<IOperationLogRepository>();
		mockOperationLogRepository
			.Setup(o => o.AppendAsync(
				It.IsAny<string>(),
				It.IsAny<string>(),
				It.IsAny<object>(),
				It.IsAny<OperationContext>(),
				It.IsAny<int>(),
				It.IsAny<CancellationToken>()))
			.Callback<string, string, object, OperationContext, int, CancellationToken>(
				(eventType, aggregateId, payload, context, version, ct) =>
				{
					appendedPayload = payload;
				})
			.ReturnsAsync(new Domain.Entities.OperationLogEntry());

		var mockLogger = new Mock<ILogger<SubmitOverrideUseCase>>();

		var useCase = new SubmitOverrideUseCase(
			mockCurrentSessionService.Object,
			mockAuthorizationPolicyService.Object,
			mockOperationLogRepository.Object,
			mockLogger.Object);

		var context = new OperationContext(operationId, Guid.NewGuid(), null, DateTime.UtcNow);
		var command = new SubmitOverrideCommand("user.unlock", "staff-789", "  \n  Reason with whitespace  \t  ", context);

		// Act
		var result = await useCase.ExecuteAsync(command);

		// Assert
		result.IsSuccess.Should().BeTrue();

		// Verify reason was trimmed
		appendedPayload.Should().NotBeNull();
		var reasonProperty = appendedPayload!.GetType().GetProperty("Reason");
		var trimmedReason = reasonProperty!.GetValue(appendedPayload) as string;
		trimmedReason.Should().Be("Reason with whitespace");
		trimmedReason!.Should().NotStartWith(" ");
		trimmedReason.Should().NotEndWith(" ");
	}
}
