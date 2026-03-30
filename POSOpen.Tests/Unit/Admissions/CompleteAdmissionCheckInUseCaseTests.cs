using FluentAssertions;
using Moq;
using POSOpen.Application.Abstractions.Repositories;
using POSOpen.Application.Abstractions.Security;
using POSOpen.Application.Abstractions.Services;
using POSOpen.Application.Security;
using POSOpen.Application.UseCases.Admissions;
using POSOpen.Domain.Entities;
using POSOpen.Domain.Enums;
using POSOpen.Shared.Operational;

namespace POSOpen.Tests.Unit.Admissions;

public sealed class CompleteAdmissionCheckInUseCaseTests
{
	[Fact]
	public async Task ExecuteAsync_authorized_completion_returns_success_and_does_not_enqueue_outbox()
	{
		var familyId = Guid.NewGuid();
		var familyRepository = CreateFamilyRepository(familyId, WaiverStatus.Valid);
		var admissionRepository = new Mock<IAdmissionCheckInRepository>();
		AdmissionCheckInPersistenceRequest? captured = null;
		admissionRepository
			.Setup(x => x.SaveCompletionAsync(It.IsAny<AdmissionCheckInPersistenceRequest>(), It.IsAny<CancellationToken>()))
			.Callback<AdmissionCheckInPersistenceRequest, CancellationToken>((request, _) => captured = request)
			.ReturnsAsync((AdmissionCheckInPersistenceRequest request, CancellationToken _) => request.Record);

		var useCase = CreateUseCase(
			familyRepository.Object,
			admissionRepository.Object,
			new AdmissionSettlementDecision(AdmissionSettlementDecisionType.Authorized, "auth-ref", null, null));

		var result = await useCase.ExecuteAsync(new CompleteAdmissionCheckInCommand(familyId, 2500, "USD"));

		result.IsSuccess.Should().BeTrue();
		result.Payload.Should().NotBeNull();
		result.Payload!.SettlementStatus.Should().Be(AdmissionSettlementStatus.Authorized);
		captured.Should().NotBeNull();
		captured!.OutboxEventType.Should().BeNull();
		captured.OperationLogEventType.Should().Be(CompleteAdmissionCheckInConstants.EventAdmissionCompleted);
	}

	[Fact]
	public async Task ExecuteAsync_deferred_completion_enqueues_outbox_and_returns_deferred_status()
	{
		var familyId = Guid.NewGuid();
		var familyRepository = CreateFamilyRepository(familyId, WaiverStatus.Valid);
		var admissionRepository = new Mock<IAdmissionCheckInRepository>();
		AdmissionCheckInPersistenceRequest? captured = null;
		admissionRepository
			.Setup(x => x.SaveCompletionAsync(It.IsAny<AdmissionCheckInPersistenceRequest>(), It.IsAny<CancellationToken>()))
			.Callback<AdmissionCheckInPersistenceRequest, CancellationToken>((request, _) => captured = request)
			.ReturnsAsync((AdmissionCheckInPersistenceRequest request, CancellationToken _) => request.Record);

		var useCase = CreateUseCase(
			familyRepository.Object,
			admissionRepository.Object,
			new AdmissionSettlementDecision(AdmissionSettlementDecisionType.DeferredEligible, null, "NETWORK_UNAVAILABLE", "queued"));

		var result = await useCase.ExecuteAsync(new CompleteAdmissionCheckInCommand(familyId, 2500, "USD"));

		result.IsSuccess.Should().BeTrue();
		result.Payload!.SettlementStatus.Should().Be(AdmissionSettlementStatus.DeferredQueued);
		captured.Should().NotBeNull();
		captured!.OutboxEventType.Should().Be(CompleteAdmissionCheckInConstants.EventAdmissionPaymentQueued);
		captured.OutboxPayload.Should().NotBeNull();
	}

	[Fact]
	public async Task ExecuteAsync_when_fast_path_is_invalid_returns_blocked_error()
	{
		var familyId = Guid.NewGuid();
		var familyRepository = CreateFamilyRepository(familyId, WaiverStatus.Expired);
		var useCase = CreateUseCase(
			familyRepository.Object,
			Mock.Of<IAdmissionCheckInRepository>(),
			new AdmissionSettlementDecision(AdmissionSettlementDecisionType.Authorized, "auth-ref", null, null));

		var result = await useCase.ExecuteAsync(new CompleteAdmissionCheckInCommand(familyId, 2500, "USD"));

		result.IsSuccess.Should().BeFalse();
		result.ErrorCode.Should().Be(CompleteAdmissionCheckInConstants.ErrorFastPathBlocked);
	}

	[Fact]
	public async Task ExecuteAsync_without_permission_returns_auth_forbidden()
	{
		var familyId = Guid.NewGuid();
		var familyRepository = CreateFamilyRepository(familyId, WaiverStatus.Valid);

		var useCase = CreateUseCase(
			familyRepository.Object,
			Mock.Of<IAdmissionCheckInRepository>(),
			new AdmissionSettlementDecision(AdmissionSettlementDecisionType.Authorized, "auth-ref", null, null),
			hasPermission: false);

		var result = await useCase.ExecuteAsync(new CompleteAdmissionCheckInCommand(familyId, 2500, "USD"));

		result.IsSuccess.Should().BeFalse();
		result.ErrorCode.Should().Be(CompleteAdmissionCheckInConstants.ErrorAuthForbidden);
	}

	[Fact]
	public async Task ExecuteAsync_when_settlement_is_non_eligible_failure_returns_completion_failed_and_does_not_persist()
	{
		var familyId = Guid.NewGuid();
		var familyRepository = CreateFamilyRepository(familyId, WaiverStatus.Valid);
		var admissionRepository = new Mock<IAdmissionCheckInRepository>();

		var useCase = CreateUseCase(
			familyRepository.Object,
			admissionRepository.Object,
			new AdmissionSettlementDecision(
				AdmissionSettlementDecisionType.NonEligibleFailure,
				null,
				"PROCESSOR_DECLINED",
				"Unable to authorize payment right now."));

		var result = await useCase.ExecuteAsync(new CompleteAdmissionCheckInCommand(familyId, 2500, "USD"));

		result.IsSuccess.Should().BeFalse();
		result.ErrorCode.Should().Be(CompleteAdmissionCheckInConstants.ErrorCompletionFailed);
		admissionRepository.Verify(
			x => x.SaveCompletionAsync(It.IsAny<AdmissionCheckInPersistenceRequest>(), It.IsAny<CancellationToken>()),
			Times.Never);
	}

	private static Mock<IFamilyProfileRepository> CreateFamilyRepository(Guid familyId, WaiverStatus waiverStatus)
	{
		var profile = FamilyProfile.Create(familyId, "Ava", "Stone", "5551000", null, null, DateTime.UtcNow);
		profile.WaiverStatus = waiverStatus;

		var repository = new Mock<IFamilyProfileRepository>();
		repository
			.Setup(x => x.GetByIdAsync(familyId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(profile);

		return repository;
	}

	private static CompleteAdmissionCheckInUseCase CreateUseCase(
		IFamilyProfileRepository familyRepository,
		IAdmissionCheckInRepository admissionCheckInRepository,
		AdmissionSettlementDecision settlementDecision,
		bool hasPermission = true)
	{
		var currentSessionService = new Mock<ICurrentSessionService>();
		currentSessionService
			.Setup(x => x.GetCurrent())
			.Returns(new CurrentSession(Guid.NewGuid(), StaffRole.Cashier, 1, 1));

		var authorizationPolicyService = new Mock<IAuthorizationPolicyService>();
		authorizationPolicyService
			.Setup(x => x.HasPermission(It.IsAny<StaffRole>(), RolePermissions.AdmissionsLookup))
			.Returns(hasPermission);

		var evaluateUseCase = new EvaluateFastPathCheckInUseCase(
			familyRepository,
			currentSessionService.Object,
			authorizationPolicyService.Object,
			Microsoft.Extensions.Logging.Abstractions.NullLogger<EvaluateFastPathCheckInUseCase>.Instance);

		var profileUseCase = new ProfileAdmissionUseCase(
			familyRepository,
			currentSessionService.Object,
			authorizationPolicyService.Object,
			Microsoft.Extensions.Logging.Abstractions.NullLogger<ProfileAdmissionUseCase>.Instance);

		var settlementService = new Mock<IAdmissionSettlementService>();
		settlementService
			.Setup(x => x.AttemptAuthorizationAsync(
				It.IsAny<Guid>(),
				It.IsAny<long>(),
				It.IsAny<string>(),
				It.IsAny<OperationContext>(),
				It.IsAny<CancellationToken>()))
			.ReturnsAsync(settlementDecision);

		var operationContextFactory = new Mock<IOperationContextFactory>();
		operationContextFactory
			.Setup(x => x.CreateRoot(It.IsAny<Guid?>()))
			.Returns(new OperationContext(Guid.NewGuid(), Guid.NewGuid(), null, DateTime.UtcNow));

		return new CompleteAdmissionCheckInUseCase(
			evaluateUseCase,
			profileUseCase,
			currentSessionService.Object,
			authorizationPolicyService.Object,
			settlementService.Object,
			admissionCheckInRepository,
			operationContextFactory.Object,
			Mock.Of<IAppStateService>(),
			Microsoft.Extensions.Logging.Abstractions.NullLogger<CompleteAdmissionCheckInUseCase>.Instance);
	}
}
