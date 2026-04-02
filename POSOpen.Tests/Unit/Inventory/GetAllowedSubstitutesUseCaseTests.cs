using FluentAssertions;
using POSOpen.Application.UseCases.Inventory;
using POSOpen.Domain.Enums;
using POSOpen.Infrastructure.Services;

namespace POSOpen.Tests.Unit.Inventory;

public sealed class GetAllowedSubstitutesUseCaseTests
{
	[Fact]
	public async Task ExecuteAsync_ReturnsRoleFilteredOrderedSubstitutes()
	{
		var sut = new GetAllowedSubstitutesUseCase(new SeededInventorySubstitutionPolicyProvider());

		var result = await sut.ExecuteAsync(new GetAllowedSubstitutesQuery(
			Guid.NewGuid(),
			StaffRole.Cashier,
			["banner-custom", "cake-custom", "table-themed"]));

		result.IsSuccess.Should().BeTrue();
		result.Payload.Should().NotBeNull();
		result.Payload!.Select(x => x.SourceOptionId).Should().Equal("banner-custom", "table-themed");
	}
}
