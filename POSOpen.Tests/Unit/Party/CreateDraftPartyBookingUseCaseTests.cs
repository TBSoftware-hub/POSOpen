using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using POSOpen.Application.Abstractions.Repositories;
using POSOpen.Application.Abstractions.Services;
using POSOpen.Application.UseCases.Party;
using POSOpen.Domain.Entities;
using POSOpen.Domain.Enums;
using POSOpen.Shared.Operational;

namespace POSOpen.Tests.Unit.Party;

public sealed class CreateDraftPartyBookingUseCaseTests
{
	[Fact]
	public async Task ExecuteAsync_WhenPackageMissing_ReturnsValidationFailure()
	{
		var sut = BuildSut();
		var command = new CreateDraftPartyBookingCommand(
			null,
			DateTime.UtcNow.Date.AddDays(1),
			"10:00",
			" ",
			new OperationContext(Guid.NewGuid(), Guid.NewGuid(), null, DateTime.UtcNow));

		var result = await sut.ExecuteAsync(command);

		result.IsSuccess.Should().BeFalse();
		result.ErrorCode.Should().Be(PartyBookingConstants.ErrorPackageRequired);
	}

	[Fact]
	public async Task ExecuteAsync_WhenSlotUnavailable_ReturnsSlotUnavailableFailure()
	{
		var repository = new Mock<IPartyBookingRepository>();
		repository.Setup(x => x.GetByOperationIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync((PartyBooking?)null);
		repository.Setup(x => x.IsSlotUnavailableAsync(It.IsAny<DateTime>(), "10:00", null, It.IsAny<CancellationToken>()))
			.ReturnsAsync(true);

		var sut = BuildSut(repository);
		var command = new CreateDraftPartyBookingCommand(
			null,
			DateTime.UtcNow.Date.AddDays(1),
			"10:00",
			"basic-party",
			new OperationContext(Guid.NewGuid(), Guid.NewGuid(), null, DateTime.UtcNow));

		var result = await sut.ExecuteAsync(command);

		result.IsSuccess.Should().BeFalse();
		result.ErrorCode.Should().Be(PartyBookingConstants.ErrorSlotUnavailable);
	}

	[Fact]
	public async Task ExecuteAsync_WhenValid_PersistsDraftWithOperationAndCorrelationIds()
	{
		var repository = new Mock<IPartyBookingRepository>();
		repository.Setup(x => x.GetByOperationIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync((PartyBooking?)null);
		repository.Setup(x => x.IsSlotUnavailableAsync(It.IsAny<DateTime>(), "13:00", null, It.IsAny<CancellationToken>()))
			.ReturnsAsync(false);
		repository.Setup(x => x.UpsertDraftAsync(It.IsAny<PartyBooking>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync((PartyBooking booking, CancellationToken _) => booking);

		var sut = BuildSut(repository);
		var context = new OperationContext(Guid.NewGuid(), Guid.NewGuid(), null, DateTime.UtcNow);
		var command = new CreateDraftPartyBookingCommand(
			null,
			DateTime.UtcNow.Date.AddDays(1),
			"13:00",
			"deluxe-party",
			context);

		var result = await sut.ExecuteAsync(command);

		result.IsSuccess.Should().BeTrue();
		result.Payload.Should().NotBeNull();
		result.Payload!.Status.Should().Be(PartyBookingStatus.Draft);
		result.Payload.OperationId.Should().Be(context.OperationId);
		result.Payload.CorrelationId.Should().Be(context.CorrelationId);
		repository.Verify(x => x.UpsertDraftAsync(It.IsAny<PartyBooking>(), It.IsAny<CancellationToken>()), Times.Once);
	}

	private static CreateDraftPartyBookingUseCase BuildSut(Mock<IPartyBookingRepository>? repositoryMock = null)
	{
		var repository = repositoryMock ?? new Mock<IPartyBookingRepository>();
		if (repositoryMock is null)
		{
			repository.Setup(x => x.GetByOperationIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync((PartyBooking?)null);
			repository.Setup(x => x.IsSlotUnavailableAsync(It.IsAny<DateTime>(), It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync(false);
			repository.Setup(x => x.UpsertDraftAsync(It.IsAny<PartyBooking>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync((PartyBooking booking, CancellationToken _) => booking);
		}

		var clock = new Mock<IUtcClock>();
		clock.Setup(x => x.UtcNow).Returns(DateTime.UtcNow);

		return new CreateDraftPartyBookingUseCase(
			repository.Object,
			clock.Object,
			new Mock<ILogger<CreateDraftPartyBookingUseCase>>().Object);
	}
}
