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

public sealed class SearchFamiliesUseCaseTests
{
	[Fact]
	public async Task ExecuteAsync_text_search_returns_matching_dtos()
	{
		var profile = CreateProfile("Ava", "Smith", "5551000", WaiverStatus.Valid);
		var repository = new Mock<IFamilyProfileRepository>();
		repository
			.Setup(x => x.SearchAsync("smith", It.IsAny<CancellationToken>()))
			.ReturnsAsync(new[] { profile });

		var useCase = CreateUseCase(repository.Object);
		var result = await useCase.ExecuteAsync(new SearchFamiliesQuery("smith", FamilyLookupMode.Text));

		result.IsSuccess.Should().BeTrue();
		result.Payload.Should().ContainSingle();
		result.Payload![0].DisplayName.Should().Be("Smith, Ava");
		result.Payload[0].WaiverStatusLabel.Should().Be("Waiver OK");
		result.Payload[0].HasPaymentOnFile.Should().BeFalse();
	}

	[Fact]
	public async Task ExecuteAsync_text_search_trims_query_before_repository_lookup()
	{
		var repository = new Mock<IFamilyProfileRepository>();
		repository
			.Setup(x => x.SearchAsync("smith", It.IsAny<CancellationToken>()))
			.ReturnsAsync(Array.Empty<FamilyProfile>());

		var useCase = CreateUseCase(repository.Object);
		var result = await useCase.ExecuteAsync(new SearchFamiliesQuery("  smith  ", FamilyLookupMode.Text));

		result.IsSuccess.Should().BeTrue();
		repository.Verify(x => x.SearchAsync("smith", It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task ExecuteAsync_text_search_short_query_returns_lookup_query_too_short()
	{
		var useCase = CreateUseCase(Mock.Of<IFamilyProfileRepository>());
		var result = await useCase.ExecuteAsync(new SearchFamiliesQuery("a", FamilyLookupMode.Text));

		result.IsSuccess.Should().BeFalse();
		result.ErrorCode.Should().Be("LOOKUP_QUERY_TOO_SHORT");
	}

	[Fact]
	public async Task ExecuteAsync_text_search_no_matches_returns_success_with_empty_list()
	{
		var repository = new Mock<IFamilyProfileRepository>();
		repository
			.Setup(x => x.SearchAsync("jones", It.IsAny<CancellationToken>()))
			.ReturnsAsync(Array.Empty<FamilyProfile>());

		var useCase = CreateUseCase(repository.Object);
		var result = await useCase.ExecuteAsync(new SearchFamiliesQuery("jones", FamilyLookupMode.Text));

		result.IsSuccess.Should().BeTrue();
		result.Payload.Should().NotBeNull();
		result.Payload.Should().BeEmpty();
	}

	[Fact]
	public async Task ExecuteAsync_scan_mode_match_returns_single_result()
	{
		var profile = CreateProfile("Liam", "Stone", "5552000", WaiverStatus.Pending);
		repositorySetupForScan(profile, out var repository);

		var useCase = CreateUseCase(repository.Object);
		var result = await useCase.ExecuteAsync(new SearchFamiliesQuery("token-123", FamilyLookupMode.Scan));

		result.IsSuccess.Should().BeTrue();
		result.Payload.Should().ContainSingle();
		result.Payload![0].WaiverStatusLabel.Should().Be("Waiver Pending");
	}

	[Fact]
	public async Task ExecuteAsync_scan_mode_trims_token_before_lookup()
	{
		var profile = CreateProfile("Liam", "Stone", "5552000", WaiverStatus.Pending);
		repositorySetupForScan(profile, out var repository);

		var useCase = CreateUseCase(repository.Object);
		var result = await useCase.ExecuteAsync(new SearchFamiliesQuery("  token-123  ", FamilyLookupMode.Scan));

		result.IsSuccess.Should().BeTrue();
		repository.Verify(x => x.GetByScanTokenAsync("token-123", It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task ExecuteAsync_scan_mode_miss_returns_empty_list_success()
	{
		var repository = new Mock<IFamilyProfileRepository>();
		repository
			.Setup(x => x.GetByScanTokenAsync("token-missing", It.IsAny<CancellationToken>()))
			.ReturnsAsync((FamilyProfile?)null);

		var useCase = CreateUseCase(repository.Object);
		var result = await useCase.ExecuteAsync(new SearchFamiliesQuery("token-missing", FamilyLookupMode.Scan));

		result.IsSuccess.Should().BeTrue();
		result.Payload.Should().NotBeNull();
		result.Payload.Should().BeEmpty();
	}

	[Fact]
	public async Task ExecuteAsync_without_session_returns_auth_forbidden()
	{
		var repository = Mock.Of<IFamilyProfileRepository>();
		var currentSession = new Mock<ICurrentSessionService>();
		currentSession.Setup(x => x.GetCurrent()).Returns((CurrentSession?)null);
		var authorization = new Mock<IAuthorizationPolicyService>();
		var useCase = new SearchFamiliesUseCase(
			repository,
			currentSession.Object,
			authorization.Object,
			NullLogger<SearchFamiliesUseCase>.Instance);

		var result = await useCase.ExecuteAsync(new SearchFamiliesQuery("smith", FamilyLookupMode.Text));

		result.IsSuccess.Should().BeFalse();
		result.ErrorCode.Should().Be("AUTH_FORBIDDEN");
	}

	[Fact]
	public async Task ExecuteAsync_without_permission_returns_auth_forbidden()
	{
		var repository = Mock.Of<IFamilyProfileRepository>();
		var currentSession = new Mock<ICurrentSessionService>();
		currentSession
			.Setup(x => x.GetCurrent())
			.Returns(new CurrentSession(Guid.NewGuid(), StaffRole.Cashier, 1, 1));

		var authorization = new Mock<IAuthorizationPolicyService>();
		authorization
			.Setup(x => x.HasPermission(StaffRole.Cashier, RolePermissions.AdmissionsLookup))
			.Returns(false);

		var useCase = new SearchFamiliesUseCase(
			repository,
			currentSession.Object,
			authorization.Object,
			NullLogger<SearchFamiliesUseCase>.Instance);

		var result = await useCase.ExecuteAsync(new SearchFamiliesQuery("smith", FamilyLookupMode.Text));

		result.IsSuccess.Should().BeFalse();
		result.ErrorCode.Should().Be("AUTH_FORBIDDEN");
	}

	[Fact]
	public async Task ExecuteAsync_when_repository_throws_returns_lookup_unavailable()
	{
		var repository = new Mock<IFamilyProfileRepository>();
		repository
			.Setup(x => x.SearchAsync("smith", It.IsAny<CancellationToken>()))
			.ThrowsAsync(new InvalidOperationException("db unavailable"));

		var useCase = CreateUseCase(repository.Object);
		var result = await useCase.ExecuteAsync(new SearchFamiliesQuery("smith", FamilyLookupMode.Text));

		result.IsSuccess.Should().BeFalse();
		result.ErrorCode.Should().Be("LOOKUP_UNAVAILABLE");
		result.UserMessage.Should().Be("Search is temporarily unavailable. Please try again.");
	}

	private static SearchFamiliesUseCase CreateUseCase(IFamilyProfileRepository repository)
	{
		var currentSession = new Mock<ICurrentSessionService>();
		currentSession
			.Setup(x => x.GetCurrent())
			.Returns(new CurrentSession(Guid.NewGuid(), StaffRole.Cashier, 1, 1));

		var authorization = new Mock<IAuthorizationPolicyService>();
		authorization
			.Setup(x => x.HasPermission(It.IsAny<StaffRole>(), RolePermissions.AdmissionsLookup))
			.Returns(true);

		return new SearchFamiliesUseCase(
			repository,
			currentSession.Object,
			authorization.Object,
			NullLogger<SearchFamiliesUseCase>.Instance);
	}

	private static FamilyProfile CreateProfile(string firstName, string lastName, string phone, WaiverStatus status)
	{
		var profile = FamilyProfile.Create(Guid.NewGuid(), firstName, lastName, phone, null, null, DateTime.UtcNow);
		profile.WaiverStatus = status;
		return profile;
	}

	private static void repositorySetupForScan(FamilyProfile profile, out Mock<IFamilyProfileRepository> repository)
	{
		repository = new Mock<IFamilyProfileRepository>();
		repository
			.Setup(x => x.GetByScanTokenAsync("token-123", It.IsAny<CancellationToken>()))
			.ReturnsAsync(profile);
	}
}
