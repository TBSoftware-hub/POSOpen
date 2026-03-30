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
		var uiService = new Mock<IFastPathCheckInUiService>();
		var viewModel = new FastPathCheckInViewModel(useCase, profileAdmissionUseCase, uiService.Object);
		viewModel.Initialize(familyId);

		await viewModel.LoadAsync();
		viewModel.IsEligible.Should().BeTrue();

		await viewModel.CompleteCheckInCommand.ExecuteAsync(null);

		viewModel.IsEligible.Should().BeFalse();
		viewModel.ErrorMessage.Should().Be("Fast-path completion is blocked until waiver requirements are satisfied.");
		uiService.Verify(x => x.ShowFastPathReadyAsync(), Times.Never);
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
		var viewModel = new FastPathCheckInViewModel(useCase, profileAdmissionUseCase, Mock.Of<IFastPathCheckInUiService>());
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
			uiService.Object);
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
			uiService.Object);
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
			Mock.Of<IFastPathCheckInUiService>());
		viewModel.Initialize(familyId);

		await viewModel.LoadAsync();

		viewModel.IsEligible.Should().BeFalse();
		viewModel.ErrorMessage.Should().Be(ProfileAdmissionConstants.SafeProfileSaveFailedMessage);
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
}
