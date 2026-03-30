using FluentAssertions;
using Moq;
using POSOpen.Application.Abstractions.Repositories;
using POSOpen.Application.Abstractions.Security;
using POSOpen.Application.Abstractions.Services;
using POSOpen.Application.Security;
using POSOpen.Application.UseCases.Admissions;
using POSOpen.Domain.Entities;
using POSOpen.Domain.Enums;
using POSOpen.Features.Admissions.ViewModels;
using POSOpen.Shared.Operational;

namespace POSOpen.Tests.Unit.Admissions;

public sealed class FastPathCheckInViewModelTests
{
	[Fact]
	public async Task CompleteCheckInCommand_rechecks_latest_state_and_blocks_when_waiver_becomes_invalid()
	{
		var familyId = Guid.NewGuid();
		var repository = new Mock<IFamilyProfileRepository>();
		repository
			.SetupSequence(x => x.GetByIdAsync(familyId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(CreateProfile(familyId, WaiverStatus.Valid))
			.ReturnsAsync(CreateProfile(familyId, WaiverStatus.Valid))
			.ReturnsAsync(CreateProfile(familyId, WaiverStatus.Expired));

		var useCase = CreateUseCase(repository.Object);
		var profileAdmissionUseCase = CreateProfileAdmissionUseCase(repository.Object);
		var completeUseCase = CreateCompleteUseCase(repository.Object, AdmissionSettlementDecisionType.Authorized);
		var viewModel = new FastPathCheckInViewModel(
			useCase,
			profileAdmissionUseCase,
			completeUseCase,
			new StubAdmissionPricingService(),
			Mock.Of<IFastPathCheckInUiService>(),
			new ControlledLatencyTimer(0, 1500),
			Mock.Of<ICheckInLatencyMonitor>());
		viewModel.Initialize(familyId);

		await viewModel.LoadAsync();
		viewModel.IsEligible.Should().BeTrue();

		await viewModel.CompleteCheckInCommand.ExecuteAsync(null);

		viewModel.IsEligible.Should().BeFalse();
		viewModel.ErrorMessage.Should().Be("Fast-path completion is blocked until waiver requirements are satisfied.");
		viewModel.ShowCompletionResult.Should().BeFalse();
	}

	[Fact]
	public async Task CompleteCheckInCommand_when_settlement_is_deferred_shows_queued_state_and_operation_id()
	{
		var familyId = Guid.NewGuid();
		var repository = new Mock<IFamilyProfileRepository>();
		repository
			.Setup(x => x.GetByIdAsync(familyId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(CreateProfile(familyId, WaiverStatus.Valid));

		var viewModel = new FastPathCheckInViewModel(
			CreateUseCase(repository.Object),
			CreateProfileAdmissionUseCase(repository.Object),
			CreateCompleteUseCase(repository.Object, AdmissionSettlementDecisionType.DeferredEligible),
			new StubAdmissionPricingService(),
			Mock.Of<IFastPathCheckInUiService>(),
			new ControlledLatencyTimer(0, 1500),
			Mock.Of<ICheckInLatencyMonitor>());
		viewModel.Initialize(familyId);

		await viewModel.LoadAsync();
		await viewModel.CompleteCheckInCommand.ExecuteAsync(null);

		viewModel.ErrorMessage.Should().BeNull();
		viewModel.ShowCompletionResult.Should().BeTrue();
		viewModel.IsDeferredQueued.Should().BeTrue();
		viewModel.CompletionStatusLabel.Should().Be(CompleteAdmissionCheckInConstants.SettlementLabelDeferred);
		viewModel.OperationIdText.Should().NotBeNullOrWhiteSpace();
	}

	[Fact]
	public async Task RefreshWaiverStatusCommand_when_evaluation_throws_sets_safe_error_and_resets_loading()
	{
		var familyId = Guid.NewGuid();
		var repository = new Mock<IFamilyProfileRepository>();
		repository
			.Setup(x => x.GetByIdAsync(familyId, It.IsAny<CancellationToken>()))
			.ThrowsAsync(new InvalidOperationException("simulated failure"));

		var useCase = CreateUseCase(repository.Object);
		var profileAdmissionUseCase = CreateProfileAdmissionUseCase(repository.Object);
		var viewModel = new FastPathCheckInViewModel(
			useCase,
			profileAdmissionUseCase,
			CreateCompleteUseCase(repository.Object, AdmissionSettlementDecisionType.Authorized),
			new StubAdmissionPricingService(),
			Mock.Of<IFastPathCheckInUiService>(),
			new ControlledLatencyTimer(0, 1500),
			Mock.Of<ICheckInLatencyMonitor>());
		viewModel.Initialize(familyId);

		await viewModel.RefreshWaiverStatusCommand.ExecuteAsync(null);

		viewModel.IsLoading.Should().BeFalse();
		viewModel.ErrorMessage.Should().Be(EvaluateFastPathCheckInConstants.SafeFastPathUnavailableMessage);
	}

	[Fact]
	public async Task StartWaiverRecoveryCommand_when_navigation_fails_sets_user_safe_message()
	{
		var familyId = Guid.NewGuid();
		var uiService = new Mock<IFastPathCheckInUiService>();
		uiService
			.Setup(x => x.NavigateToWaiverRecoveryAsync(familyId))
			.ThrowsAsync(new InvalidOperationException("navigation unavailable"));

		var repository = new Mock<IFamilyProfileRepository>();
		repository
			.Setup(x => x.GetByIdAsync(familyId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(CreateProfile(familyId, WaiverStatus.Pending));
		var viewModel = new FastPathCheckInViewModel(
			CreateUseCase(repository.Object),
			CreateProfileAdmissionUseCase(repository.Object),
			CreateCompleteUseCase(repository.Object, AdmissionSettlementDecisionType.Authorized),
			new StubAdmissionPricingService(),
			uiService.Object,
			new ControlledLatencyTimer(0, 1500),
			Mock.Of<ICheckInLatencyMonitor>());
		viewModel.Initialize(familyId);

		await viewModel.StartWaiverRecoveryCommand.ExecuteAsync(null);

		viewModel.ErrorMessage.Should().Be("Waiver recovery is not available on this terminal yet.");
	}

	[Fact]
	public async Task StartProfileCompletionCommand_when_navigation_fails_sets_route_unavailable_message()
	{
		var familyId = Guid.NewGuid();
		var repository = new Mock<IFamilyProfileRepository>();
		repository
			.Setup(x => x.GetByIdAsync(familyId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(CreateProfile(familyId, WaiverStatus.Valid));

		var uiService = new Mock<IFastPathCheckInUiService>();
		uiService
			.Setup(x => x.NavigateToProfileCompletionAsync(familyId))
			.ThrowsAsync(new InvalidOperationException("route unavailable"));

		var viewModel = new FastPathCheckInViewModel(
			CreateUseCase(repository.Object),
			CreateProfileAdmissionUseCase(repository.Object),
			CreateCompleteUseCase(repository.Object, AdmissionSettlementDecisionType.Authorized),
			new StubAdmissionPricingService(),
			uiService.Object,
			new ControlledLatencyTimer(0, 1500),
			Mock.Of<ICheckInLatencyMonitor>());
		viewModel.Initialize(familyId);

		await viewModel.LoadAsync();
		await viewModel.StartProfileCompletionCommand.ExecuteAsync(null);

		viewModel.ErrorMessage.Should().Be(ProfileAdmissionConstants.SafeAdmissionRouteUnavailableMessage);
	}

	[Fact]
	public async Task LoadAsync_when_profile_completeness_check_fails_blocks_eligibility_with_safe_error()
	{
		var familyId = Guid.NewGuid();
		var repository = new Mock<IFamilyProfileRepository>();
		repository
			.SetupSequence(x => x.GetByIdAsync(familyId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(CreateProfile(familyId, WaiverStatus.Valid))
			.ThrowsAsync(new InvalidOperationException("persistence error"));

		var viewModel = new FastPathCheckInViewModel(
			CreateUseCase(repository.Object),
			CreateProfileAdmissionUseCase(repository.Object),
			CreateCompleteUseCase(repository.Object, AdmissionSettlementDecisionType.Authorized),
			new StubAdmissionPricingService(),
			Mock.Of<IFastPathCheckInUiService>(),
			new ControlledLatencyTimer(0, 1500),
			Mock.Of<ICheckInLatencyMonitor>());
		viewModel.Initialize(familyId);

		await viewModel.LoadAsync();

		viewModel.IsEligible.Should().BeFalse();
		viewModel.ErrorMessage.Should().Be(ProfileAdmissionConstants.SafeProfileSaveFailedMessage);
	}

	[Fact]
	public async Task LoadAsync_when_feedback_is_within_two_seconds_records_non_breach_latency()
	{
		var familyId = Guid.NewGuid();
		var repository = new Mock<IFamilyProfileRepository>();
		repository
			.Setup(x => x.GetByIdAsync(familyId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(CreateProfile(familyId, WaiverStatus.Valid));

		var latencyMonitor = new Mock<ICheckInLatencyMonitor>();
		var viewModel = new FastPathCheckInViewModel(
			CreateUseCase(repository.Object),
			CreateProfileAdmissionUseCase(repository.Object),
			CreateCompleteUseCase(repository.Object, AdmissionSettlementDecisionType.Authorized),
			new StubAdmissionPricingService(),
			Mock.Of<IFastPathCheckInUiService>(),
			new ControlledLatencyTimer(1000, 2500),
			latencyMonitor.Object);
		viewModel.Initialize(familyId);

		await viewModel.LoadAsync();

		latencyMonitor.Verify(
			x => x.Record(
				"EvaluateFastPathCheckIn",
				familyId,
				1500,
				false),
			Times.Once);
	}

	[Fact]
	public async Task LoadAsync_when_feedback_exceeds_two_seconds_records_threshold_breach()
	{
		var familyId = Guid.NewGuid();
		var repository = new Mock<IFamilyProfileRepository>();
		repository
			.Setup(x => x.GetByIdAsync(familyId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(CreateProfile(familyId, WaiverStatus.Valid));

		var latencyMonitor = new Mock<ICheckInLatencyMonitor>();
		var viewModel = new FastPathCheckInViewModel(
			CreateUseCase(repository.Object),
			CreateProfileAdmissionUseCase(repository.Object),
			CreateCompleteUseCase(repository.Object, AdmissionSettlementDecisionType.Authorized),
			new StubAdmissionPricingService(),
			Mock.Of<IFastPathCheckInUiService>(),
			new ControlledLatencyTimer(1000, 3501),
			latencyMonitor.Object);
		viewModel.Initialize(familyId);

		await viewModel.LoadAsync();

		latencyMonitor.Verify(
			x => x.Record(
				"EvaluateFastPathCheckIn",
				familyId,
				2501,
				true),
			Times.Once);
	}

	[Fact]
	public async Task CompleteCheckInCommand_reuses_evaluated_total_and_avoids_extra_pricing_fetch()
	{
		var familyId = Guid.NewGuid();
		var repository = new Mock<IFamilyProfileRepository>();
		repository
			.Setup(x => x.GetByIdAsync(familyId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(CreateProfile(familyId, WaiverStatus.Valid));

		var pricingService = new Mock<IAdmissionPricingService>();
		pricingService
			.Setup(x => x.GetAdmissionTotalAsync(familyId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(new AdmissionTotal(2500, "USD"));

		var viewModel = new FastPathCheckInViewModel(
			CreateUseCase(repository.Object),
			CreateProfileAdmissionUseCase(repository.Object),
			CreateCompleteUseCase(repository.Object, AdmissionSettlementDecisionType.Authorized),
			pricingService.Object,
			Mock.Of<IFastPathCheckInUiService>(),
			new ControlledLatencyTimer(1000, 1100, 2000, 2100),
			Mock.Of<ICheckInLatencyMonitor>());
		viewModel.Initialize(familyId);

		await viewModel.LoadAsync();
		await viewModel.CompleteCheckInCommand.ExecuteAsync(null);

		pricingService.Verify(
			x => x.GetAdmissionTotalAsync(familyId, It.IsAny<CancellationToken>()),
			Times.Exactly(2));
	}

	private static EvaluateFastPathCheckInUseCase CreateUseCase(IFamilyProfileRepository repository)
	{
		var currentSession = new Mock<ICurrentSessionService>();
		currentSession
			.Setup(x => x.GetCurrent())
			.Returns(new CurrentSession(Guid.NewGuid(), StaffRole.Cashier, 1, 1));

		var authorization = new Mock<IAuthorizationPolicyService>();
		authorization
			.Setup(x => x.HasPermission(It.IsAny<StaffRole>(), RolePermissions.AdmissionsLookup))
			.Returns(true);

		return new EvaluateFastPathCheckInUseCase(
			repository,
			currentSession.Object,
			authorization.Object,
			Microsoft.Extensions.Logging.Abstractions.NullLogger<EvaluateFastPathCheckInUseCase>.Instance);
	}

	private static FamilyProfile CreateProfile(Guid id, WaiverStatus waiverStatus)
	{
		var profile = FamilyProfile.Create(id, "Ava", "Stone", "5551000", null, null, DateTime.UtcNow);
		profile.WaiverStatus = waiverStatus;
		return profile;
	}

	private static ProfileAdmissionUseCase CreateProfileAdmissionUseCase(IFamilyProfileRepository repository)
	{
		var currentSession = new Mock<ICurrentSessionService>();
		currentSession
			.Setup(x => x.GetCurrent())
			.Returns(new CurrentSession(Guid.NewGuid(), StaffRole.Cashier, 1, 1));

		var authorization = new Mock<IAuthorizationPolicyService>();
		authorization
			.Setup(x => x.HasPermission(It.IsAny<StaffRole>(), RolePermissions.AdmissionsLookup))
			.Returns(true);

		return new ProfileAdmissionUseCase(
			repository,
			currentSession.Object,
			authorization.Object,
			Microsoft.Extensions.Logging.Abstractions.NullLogger<ProfileAdmissionUseCase>.Instance);
	}

	private static CompleteAdmissionCheckInUseCase CreateCompleteUseCase(
		IFamilyProfileRepository repository,
		AdmissionSettlementDecisionType settlementDecisionType)
	{
		var evaluateUseCase = CreateUseCase(repository);
		var profileUseCase = CreateProfileAdmissionUseCase(repository);

		var currentSession = new Mock<ICurrentSessionService>();
		currentSession
			.Setup(x => x.GetCurrent())
			.Returns(new CurrentSession(Guid.NewGuid(), StaffRole.Cashier, 1, 1));

		var authorization = new Mock<IAuthorizationPolicyService>();
		authorization
			.Setup(x => x.HasPermission(It.IsAny<StaffRole>(), RolePermissions.AdmissionsLookup))
			.Returns(true);

		var settlementService = new Mock<IAdmissionSettlementService>();
		settlementService
			.Setup(x => x.AttemptAuthorizationAsync(
				It.IsAny<Guid>(),
				It.IsAny<long>(),
				It.IsAny<string>(),
				It.IsAny<OperationContext>(),
				It.IsAny<CancellationToken>()))
			.ReturnsAsync(new AdmissionSettlementDecision(settlementDecisionType, "processor-ref", null, null));

		var repositoryMock = new Mock<IAdmissionCheckInRepository>();
		repositoryMock
			.Setup(x => x.SaveCompletionAsync(It.IsAny<AdmissionCheckInPersistenceRequest>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync((AdmissionCheckInPersistenceRequest request, CancellationToken _) => request.Record);

		var operationContextFactory = new Mock<IOperationContextFactory>();
		operationContextFactory
			.Setup(x => x.CreateRoot(It.IsAny<Guid?>()))
			.Returns(new OperationContext(Guid.NewGuid(), Guid.NewGuid(), null, DateTime.UtcNow));

		return new CompleteAdmissionCheckInUseCase(
			evaluateUseCase,
			profileUseCase,
			currentSession.Object,
			authorization.Object,
			settlementService.Object,
			repositoryMock.Object,
			operationContextFactory.Object,
			Mock.Of<IAppStateService>(),
			Microsoft.Extensions.Logging.Abstractions.NullLogger<CompleteAdmissionCheckInUseCase>.Instance);
	}

	private sealed class StubAdmissionPricingService : IAdmissionPricingService
	{
		public Task<AdmissionTotal> GetAdmissionTotalAsync(Guid familyId, CancellationToken cancellationToken = default)
		{
			return Task.FromResult(new AdmissionTotal(2500, "USD"));
		}
	}

	private sealed class ControlledLatencyTimer : ICheckInLatencyTimer
	{
		private readonly Queue<long> _timestamps;
		private long _lastTimestamp;

		public ControlledLatencyTimer(params long[] timestamps)
		{
			if (timestamps.Length == 0)
			{
				throw new ArgumentException("At least one timestamp value is required.", nameof(timestamps));
			}

			_timestamps = new Queue<long>(timestamps);
			_lastTimestamp = timestamps[^1];
		}

		public long GetTimestamp()
		{
			if (_timestamps.Count == 0)
			{
				return _lastTimestamp;
			}

			_lastTimestamp = _timestamps.Dequeue();
			return _lastTimestamp;
		}

		public double GetElapsedMilliseconds(long startTimestamp, long endTimestamp)
		{
			return endTimestamp - startTimestamp;
		}
	}
}
