using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using POSOpen.Application.Abstractions.Repositories;
using POSOpen.Application.UseCases.Party;
using POSOpen.Domain.Entities;
using POSOpen.Domain.Enums;

namespace POSOpen.Tests.Unit.Party;

public sealed class GetBookingAddOnOptionsUseCaseTests
{
	private static readonly Guid BookingId = Guid.NewGuid();
	private static readonly DateTime TestDateUtc = new(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc);

	private static GetBookingAddOnOptionsUseCase CreateSut(IPartyBookingRepository repository) =>
		new(repository, NullLogger<GetBookingAddOnOptionsUseCase>.Instance);

	private static PartyBooking CreateBookingWithSelections(params PartyBookingAddOnSelection[] selections) =>
		new()
		{
			Id = BookingId,
			PartyDateUtc = TestDateUtc,
			SlotId = "10:00",
			PackageId = "basic",
			Status = PartyBookingStatus.Booked,
			OperationId = Guid.NewGuid(),
			CorrelationId = Guid.NewGuid(),
			CreatedAtUtc = TestDateUtc,
			UpdatedAtUtc = TestDateUtc,
			BookedAtUtc = TestDateUtc,
			DepositCommitmentStatus = PartyDepositCommitmentStatus.None,
			AddOnSelections = selections,
		};

	[Fact]
	public async Task ExecuteAsync_NoSelections_ReturnsUnselectedOptionsAndZeroTotal()
	{
		var repo = new Mock<IPartyBookingRepository>();
		repo.Setup(x => x.GetByIdWithSelectionsAsync(BookingId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(CreateBookingWithSelections());
		var sut = CreateSut(repo.Object);

		var result = await sut.ExecuteAsync(new GetBookingAddOnOptionsQuery(BookingId));

		result.IsSuccess.Should().BeTrue();
		result.Payload.Should().NotBeNull();
		result.Payload!.AddOnTotalAmountCents.Should().Be(0);
		result.Payload.CateringOptions.Should().OnlyContain(option => !option.IsSelected && option.Quantity == 0);
		result.Payload.DecorOptions.Should().OnlyContain(option => !option.IsSelected && option.Quantity == 0);
	}

	[Fact]
	public async Task ExecuteAsync_WithSelections_ComputesTotalsAndRiskMetadata()
	{
		var selections = new[]
		{
			new PartyBookingAddOnSelection
			{
				Id = Guid.NewGuid(),
				BookingId = BookingId,
				AddOnType = PartyAddOnType.Catering,
				OptionId = "cake-custom",
				Quantity = 1,
				SelectedAtUtc = TestDateUtc,
				SelectionOperationId = Guid.NewGuid(),
			},
			new PartyBookingAddOnSelection
			{
				Id = Guid.NewGuid(),
				BookingId = BookingId,
				AddOnType = PartyAddOnType.Decor,
				OptionId = "banner-standard",
				Quantity = 1,
				SelectedAtUtc = TestDateUtc,
				SelectionOperationId = Guid.NewGuid(),
			},
		};

		var repo = new Mock<IPartyBookingRepository>();
		repo.Setup(x => x.GetByIdWithSelectionsAsync(BookingId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(CreateBookingWithSelections(selections));
		var sut = CreateSut(repo.Object);

		var result = await sut.ExecuteAsync(new GetBookingAddOnOptionsQuery(BookingId));

		result.IsSuccess.Should().BeTrue();
		result.Payload.Should().NotBeNull();
		result.Payload!.AddOnTotalAmountCents.Should().Be(
			PartyBookingConstants.AddOnOptionPriceCents["cake-custom"] +
			PartyBookingConstants.AddOnOptionPriceCents["banner-standard"]);

		var risky = result.Payload.CateringOptions.Single(option => option.OptionId == "cake-custom");
		risky.IsSelected.Should().BeTrue();
		risky.IsAtRisk.Should().BeTrue();
		risky.RiskSeverity.Should().Be(PartyBookingConstants.RiskSeverityHigh);
		risky.RiskReason.Should().Be(PartyBookingConstants.RiskReasonInventoryShortfall);
	}
}
