using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using POSOpen.Application.Abstractions.Repositories;
using POSOpen.Application.Abstractions.Security;
using POSOpen.Application.Security;
using POSOpen.Application.UseCases.Admissions;
using POSOpen.Domain.Entities;
using POSOpen.Domain.Enums;

namespace POSOpen.Tests.Unit.Admissions;

public sealed class EvaluateFastPathCheckInUseCaseTests
{
	[Fact]
	public async Task ExecuteAsync_valid_waiver_returns_allowed_eligible_result()
	{
		var familyId = Guid.NewGuid();
		var repository = BuildRepository(CreateProfile(familyId, WaiverStatus.Valid));
		var useCase = CreateUseCase(repository.Object);

		var result = await useCase.ExecuteAsync(new EvaluateFastPathCheckInQuery(familyId));

		result.IsSuccess.Should().BeTrue();
		result.Payload.Should().NotBeNull();
		result.Payload!.State.Should().Be(FastPathEligibilityState.Allowed);
		result.Payload.IsEligible.Should().BeTrue();
		result.Payload.WaiverStatusLabel.Should().Be("Waiver OK");
	}

	[Fact]
	public async Task ExecuteAsync_pending_waiver_without_refresh_returns_refresh_required()
	{
		var familyId = Guid.NewGuid();
		var repository = BuildRepository(CreateProfile(familyId, WaiverStatus.Pending));
		var useCase = CreateUseCase(repository.Object);

		var result = await useCase.ExecuteAsync(new EvaluateFastPathCheckInQuery(familyId, IsRefreshRequested: false));

		result.IsSuccess.Should().BeTrue();
		result.Payload!.State.Should().Be(FastPathEligibilityState.RefreshRequired);
		result.Payload.IsEligible.Should().BeFalse();
		result.Payload.ShowRefreshAction.Should().BeTrue();
	}

	[Fact]
	public async Task ExecuteAsync_pending_waiver_with_refresh_returns_blocked_until_completed()
	{
		var familyId = Guid.NewGuid();
		var repository = BuildRepository(CreateProfile(familyId, WaiverStatus.Pending));
		var useCase = CreateUseCase(repository.Object);

		var result = await useCase.ExecuteAsync(new EvaluateFastPathCheckInQuery(familyId, IsRefreshRequested: true));

		result.IsSuccess.Should().BeTrue();
		result.Payload!.State.Should().Be(FastPathEligibilityState.Blocked);
		result.Payload.IsEligible.Should().BeFalse();
		result.Payload.ShowRecoveryAction.Should().BeTrue();
	}

	[Theory]
	[InlineData(WaiverStatus.None)]
	[InlineData(WaiverStatus.Expired)]
	public async Task ExecuteAsync_non_valid_waiver_returns_blocked(WaiverStatus status)
	{
		var familyId = Guid.NewGuid();
		var repository = BuildRepository(CreateProfile(familyId, status));
		var useCase = CreateUseCase(repository.Object);

		var result = await useCase.ExecuteAsync(new EvaluateFastPathCheckInQuery(familyId));

		result.IsSuccess.Should().BeTrue();
		result.Payload!.State.Should().Be(FastPathEligibilityState.Blocked);
		result.Payload.IsEligible.Should().BeFalse();
	}

	[Fact]
	public async Task ExecuteAsync_without_session_returns_auth_forbidden()
	{
		var repository = new Mock<IFamilyProfileRepository>();
		var currentSession = new Mock<ICurrentSessionService>();
		currentSession.Setup(x => x.GetCurrent()).Returns((CurrentSession?)null);
		var authorization = new Mock<IAuthorizationPolicyService>();
		var useCase = new EvaluateFastPathCheckInUseCase(
			repository.Object,
			currentSession.Object,
			authorization.Object,
			NullLogger<EvaluateFastPathCheckInUseCase>.Instance);

		var result = await useCase.ExecuteAsync(new EvaluateFastPathCheckInQuery(Guid.NewGuid()));

		result.IsSuccess.Should().BeFalse();
		result.ErrorCode.Should().Be("AUTH_FORBIDDEN");
	}

	[Fact]
	public async Task ExecuteAsync_without_permission_returns_auth_forbidden()
	{
		var repository = new Mock<IFamilyProfileRepository>();
		var currentSession = new Mock<ICurrentSessionService>();
		currentSession
			.Setup(x => x.GetCurrent())
			.Returns(new CurrentSession(Guid.NewGuid(), StaffRole.Cashier, 1, 1));

		var authorization = new Mock<IAuthorizationPolicyService>();
		authorization
			.Setup(x => x.HasPermission(StaffRole.Cashier, RolePermissions.AdmissionsLookup))
			.Returns(false);

		var useCase = new EvaluateFastPathCheckInUseCase(
			repository.Object,
			currentSession.Object,
			authorization.Object,
			NullLogger<EvaluateFastPathCheckInUseCase>.Instance);

		var result = await useCase.ExecuteAsync(new EvaluateFastPathCheckInQuery(Guid.NewGuid()));

		result.IsSuccess.Should().BeFalse();
		result.ErrorCode.Should().Be("AUTH_FORBIDDEN");
	}

	[Fact]
	public async Task ExecuteAsync_when_family_missing_returns_family_not_found()
	{
		var repository = new Mock<IFamilyProfileRepository>();
		repository
			.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync((FamilyProfile?)null);
		var useCase = CreateUseCase(repository.Object);

		var result = await useCase.ExecuteAsync(new EvaluateFastPathCheckInQuery(Guid.NewGuid()));

		result.IsSuccess.Should().BeFalse();
		result.ErrorCode.Should().Be("FAMILY_NOT_FOUND");
	}

	[Fact]
	public async Task ExecuteAsync_when_repository_throws_returns_fast_path_unavailable()
	{
		var repository = new Mock<IFamilyProfileRepository>();
		repository
			.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
			.ThrowsAsync(new InvalidOperationException("db unavailable"));
		var useCase = CreateUseCase(repository.Object);

		var result = await useCase.ExecuteAsync(new EvaluateFastPathCheckInQuery(Guid.NewGuid()));

		result.IsSuccess.Should().BeFalse();
		result.ErrorCode.Should().Be("FAST_PATH_UNAVAILABLE");
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
			NullLogger<EvaluateFastPathCheckInUseCase>.Instance);
	}

	private static Mock<IFamilyProfileRepository> BuildRepository(FamilyProfile profile)
	{
		var repository = new Mock<IFamilyProfileRepository>();
		repository
			.Setup(x => x.GetByIdAsync(profile.Id, It.IsAny<CancellationToken>()))
			.ReturnsAsync(profile);
		return repository;
	}

	private static FamilyProfile CreateProfile(Guid id, WaiverStatus status)
	{
		var profile = FamilyProfile.Create(id, "Ava", "Stone", "5551000", null, null, DateTime.UtcNow);
		profile.WaiverStatus = status;
		return profile;
	}
}
