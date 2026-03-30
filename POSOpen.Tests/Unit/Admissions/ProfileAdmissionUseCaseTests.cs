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

public sealed class ProfileAdmissionUseCaseTests
{
	[Fact]
	public async Task InitializeAsync_without_family_returns_new_draft_with_missing_required_fields()
	{
		var useCase = CreateUseCase(Mock.Of<IFamilyProfileRepository>());

		var result = await useCase.InitializeAsync(new InitializeProfileAdmissionDraftQuery(null, "smith"));

		result.IsSuccess.Should().BeTrue();
		result.Payload.Should().NotBeNull();
		result.Payload!.IsExistingProfile.Should().BeFalse();
		result.Payload.MissingRequiredFields.Should().BeEquivalentTo(["firstName", "lastName", "phone"]);
	}

	[Fact]
	public async Task InitializeAsync_existing_incomplete_profile_returns_missing_fields_only()
	{
		var familyId = Guid.NewGuid();
		var profile = FamilyProfile.Create(familyId, "Ava", "", "", null, null, DateTime.UtcNow);
		var repository = new Mock<IFamilyProfileRepository>();
		repository.Setup(x => x.GetByIdAsync(familyId, It.IsAny<CancellationToken>())).ReturnsAsync(profile);

		var useCase = CreateUseCase(repository.Object);
		var result = await useCase.InitializeAsync(new InitializeProfileAdmissionDraftQuery(familyId, null));

		result.IsSuccess.Should().BeTrue();
		result.Payload!.IsExistingProfile.Should().BeTrue();
		result.Payload.MissingRequiredFields.Should().BeEquivalentTo(["lastName", "phone"]);
	}

	[Fact]
	public async Task SubmitAsync_missing_required_fields_returns_validation_error()
	{
		var useCase = CreateUseCase(Mock.Of<IFamilyProfileRepository>());

		var result = await useCase.SubmitAsync(new SubmitProfileAdmissionCommand(null, "Ava", "", "", null));

		result.IsSuccess.Should().BeFalse();
		result.ErrorCode.Should().Be(ProfileAdmissionConstants.ErrorProfileRequiredFieldsMissing);
	}

	[Fact]
	public async Task SubmitAsync_new_profile_valid_input_calls_add_and_returns_family_id()
	{
		var repository = new Mock<IFamilyProfileRepository>();
		var useCase = CreateUseCase(repository.Object);

		var result = await useCase.SubmitAsync(new SubmitProfileAdmissionCommand(null, "Ava", "Stone", "5551000", "a@b.com"));

		result.IsSuccess.Should().BeTrue();
		result.Payload.Should().NotBeNull();
		repository.Verify(x => x.AddAsync(It.IsAny<FamilyProfile>(), It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task SubmitAsync_existing_profile_not_found_returns_profile_not_found()
	{
		var repository = new Mock<IFamilyProfileRepository>();
		repository.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((FamilyProfile?)null);
		var useCase = CreateUseCase(repository.Object);

		var result = await useCase.SubmitAsync(new SubmitProfileAdmissionCommand(Guid.NewGuid(), "Ava", "Stone", "5551000", null));

		result.IsSuccess.Should().BeFalse();
		result.ErrorCode.Should().Be(ProfileAdmissionConstants.ErrorProfileNotFound);
	}

	[Fact]
	public async Task SubmitAsync_existing_profile_valid_input_updates_profile()
	{
		var familyId = Guid.NewGuid();
		var existing = FamilyProfile.Create(familyId, "Ava", "", "", null, null, DateTime.UtcNow);
		var repository = new Mock<IFamilyProfileRepository>();
		repository.Setup(x => x.GetByIdAsync(familyId, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

		var useCase = CreateUseCase(repository.Object);
		var result = await useCase.SubmitAsync(new SubmitProfileAdmissionCommand(familyId, "Ava", "Stone", "5551000", null));

		result.IsSuccess.Should().BeTrue();
		repository.Verify(x => x.UpdateAsync(It.IsAny<FamilyProfile>(), It.IsAny<CancellationToken>()), Times.Once);
		existing.PrimaryContactLastName.Should().Be("Stone");
		existing.Phone.Should().Be("5551000");
	}

	[Fact]
	public async Task SubmitAsync_normalizes_phone_before_persisting()
	{
		FamilyProfile? captured = null;
		var repository = new Mock<IFamilyProfileRepository>();
		repository
			.Setup(x => x.AddAsync(It.IsAny<FamilyProfile>(), It.IsAny<CancellationToken>()))
			.Callback<FamilyProfile, CancellationToken>((profile, _) => captured = profile)
			.Returns(Task.CompletedTask);

		var useCase = CreateUseCase(repository.Object);
		var result = await useCase.SubmitAsync(new SubmitProfileAdmissionCommand(null, "Ava", "Stone", "(555) 100-2000", null));

		result.IsSuccess.Should().BeTrue();
		captured.Should().NotBeNull();
		captured!.Phone.Should().Be("5551002000");
	}

	[Fact]
	public async Task SubmitAsync_without_session_returns_auth_forbidden()
	{
		var repository = new Mock<IFamilyProfileRepository>();
		var session = new Mock<ICurrentSessionService>();
		session.Setup(x => x.GetCurrent()).Returns((CurrentSession?)null);
		var auth = new Mock<IAuthorizationPolicyService>();

		var useCase = new ProfileAdmissionUseCase(
			repository.Object,
			session.Object,
			auth.Object,
			NullLogger<ProfileAdmissionUseCase>.Instance);

		var result = await useCase.SubmitAsync(new SubmitProfileAdmissionCommand(null, "Ava", "Stone", "5551000", null));

		result.IsSuccess.Should().BeFalse();
		result.ErrorCode.Should().Be(ProfileAdmissionConstants.ErrorAuthForbidden);
	}

	private static ProfileAdmissionUseCase CreateUseCase(IFamilyProfileRepository repository)
	{
		var session = new Mock<ICurrentSessionService>();
		session.Setup(x => x.GetCurrent()).Returns(new CurrentSession(Guid.NewGuid(), StaffRole.Cashier, 1, 1));

		var auth = new Mock<IAuthorizationPolicyService>();
		auth.Setup(x => x.HasPermission(It.IsAny<StaffRole>(), RolePermissions.AdmissionsLookup)).Returns(true);

		return new ProfileAdmissionUseCase(repository, session.Object, auth.Object, NullLogger<ProfileAdmissionUseCase>.Instance);
	}
}
