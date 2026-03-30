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

public sealed class NewProfileAdmissionViewModelTests
{
	[Fact]
	public async Task LoadAsync_existing_incomplete_profile_shows_only_missing_required_fields()
	{
		var familyId = Guid.NewGuid();
		var profile = FamilyProfile.Create(familyId, "Ava", string.Empty, "5551000", null, null, DateTime.UtcNow);
		var repository = new Mock<IFamilyProfileRepository>();
		repository
			.Setup(x => x.GetByIdAsync(familyId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(profile);

		var viewModel = CreateViewModel(repository.Object, Mock.Of<IProfileAdmissionUiService>());
		viewModel.SetRouteInput(familyId, null);

		await viewModel.LoadAsync();

		viewModel.ShowFirstNameField.Should().BeFalse();
		viewModel.ShowLastNameField.Should().BeTrue();
		viewModel.ShowPhoneField.Should().BeFalse();
	}

	[Fact]
	public async Task SubmitCommand_with_invalid_input_preserves_entered_values_and_sets_field_errors()
	{
		var viewModel = CreateViewModel(Mock.Of<IFamilyProfileRepository>(), Mock.Of<IProfileAdmissionUiService>());
		viewModel.FirstName = "Ava";
		viewModel.LastName = string.Empty;
		viewModel.Phone = string.Empty;
		viewModel.Email = "invalid-email";

		await viewModel.SubmitCommand.ExecuteAsync(null);

		viewModel.FirstName.Should().Be("Ava");
		viewModel.LastName.Should().BeEmpty();
		viewModel.Phone.Should().BeEmpty();
		viewModel.Email.Should().Be("invalid-email");
		viewModel.LastNameError.Should().NotBeEmpty();
		viewModel.PhoneError.Should().NotBeEmpty();
		viewModel.EmailError.Should().NotBeEmpty();
	}

	[Fact]
	public async Task SubmitCommand_when_navigation_fails_sets_route_unavailable_error()
	{
		var uiService = new Mock<IProfileAdmissionUiService>();
		uiService
			.Setup(x => x.NavigateToFastPathCheckInAsync(It.IsAny<Guid>()))
			.ThrowsAsync(new InvalidOperationException("route unavailable"));

		var viewModel = CreateViewModel(Mock.Of<IFamilyProfileRepository>(), uiService.Object);
		viewModel.FirstName = "Ava";
		viewModel.LastName = "Stone";
		viewModel.Phone = "5551000";

		await viewModel.SubmitCommand.ExecuteAsync(null);

		viewModel.SummaryError.Should().Be(ProfileAdmissionConstants.SafeAdmissionRouteUnavailableMessage);
	}

	private static NewProfileAdmissionViewModel CreateViewModel(IFamilyProfileRepository repository, IProfileAdmissionUiService uiService)
	{
		var session = new Mock<ICurrentSessionService>();
		session.Setup(x => x.GetCurrent()).Returns(new CurrentSession(Guid.NewGuid(), StaffRole.Cashier, 1, 1));

		var auth = new Mock<IAuthorizationPolicyService>();
		auth.Setup(x => x.HasPermission(It.IsAny<StaffRole>(), RolePermissions.AdmissionsLookup)).Returns(true);

		var useCase = new ProfileAdmissionUseCase(
			repository,
			session.Object,
			auth.Object,
			Microsoft.Extensions.Logging.Abstractions.NullLogger<ProfileAdmissionUseCase>.Instance);

		return new NewProfileAdmissionViewModel(useCase, uiService);
	}
}
