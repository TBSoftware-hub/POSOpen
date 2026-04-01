using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using POSOpen.Application.Abstractions.Repositories;
using POSOpen.Application.UseCases.Party;
using POSOpen.Domain.Entities;
using POSOpen.Domain.Enums;
using POSOpen.Shared.Operational;

namespace POSOpen.Tests.Unit.Party;

public sealed class RecordPartyDepositCommitmentUseCaseTests
{
	[Fact]
	public async Task ExecuteAsync_WhenAmountInvalid_ReturnsValidationFailure()
	{
		var sut = BuildSut();
		var command = new RecordPartyDepositCommitmentCommand(
			Guid.NewGuid(),
			0,
			"USD",
			new OperationContext(Guid.NewGuid(), Guid.NewGuid(), null, DateTime.UtcNow));

		var result = await sut.ExecuteAsync(command);

		result.IsSuccess.Should().BeFalse();
		result.ErrorCode.Should().Be(PartyBookingConstants.ErrorDepositAmountInvalid);
	}

	[Fact]
	public async Task ExecuteAsync_WhenAlreadyCommittedWithSameOperation_ReturnsIdempotentSuccess()
	{
		var operationId = Guid.NewGuid();
		var booking = PartyBooking.CreateDraft(
			Guid.NewGuid(),
			DateTime.UtcNow.Date.AddDays(1),
			"10:00",
			"basic-party",
			Guid.NewGuid(),
			Guid.NewGuid(),
			DateTime.UtcNow);
		booking.Status = PartyBookingStatus.Booked;
		booking.DepositCommitmentStatus = PartyDepositCommitmentStatus.Committed;
		booking.DepositAmountCents = 15000;
		booking.DepositCurrency = "USD";
		booking.DepositCommittedAtUtc = DateTime.UtcNow;
		booking.DepositCommitmentOperationId = operationId;

		var repository = new Mock<IPartyBookingRepository>();
		repository.Setup(x => x.GetByIdAsync(booking.Id, It.IsAny<CancellationToken>()))
			.ReturnsAsync(booking);

		var sut = BuildSut(repository);
		var command = new RecordPartyDepositCommitmentCommand(
			booking.Id,
			15000,
			"USD",
			new OperationContext(operationId, Guid.NewGuid(), null, DateTime.UtcNow));

		var result = await sut.ExecuteAsync(command);

		result.IsSuccess.Should().BeTrue();
		result.UserMessage.Should().Be(PartyBookingConstants.DepositAlreadyCommittedMessage);
	}

	[Fact]
	public async Task ExecuteAsync_WhenValid_PersistsCommitmentAndTraceability()
	{
		var booking = PartyBooking.CreateDraft(
			Guid.NewGuid(),
			DateTime.UtcNow.Date.AddDays(1),
			"13:00",
			"deluxe-party",
			Guid.NewGuid(),
			Guid.NewGuid(),
			DateTime.UtcNow);
		booking.Status = PartyBookingStatus.Booked;

		var repository = new Mock<IPartyBookingRepository>();
		repository.Setup(x => x.GetByIdAsync(booking.Id, It.IsAny<CancellationToken>()))
			.ReturnsAsync(booking);
		repository.Setup(x => x.RecordDepositCommitmentAsync(
				booking,
				20000,
				"USD",
				It.IsAny<Guid>(),
				It.IsAny<Guid>(),
				It.IsAny<DateTime>(),
				It.IsAny<CancellationToken>()))
			.ReturnsAsync((PartyBooking b, long amount, string currency, Guid operationId, Guid correlationId, DateTime committedAtUtc, CancellationToken _) =>
			{
				b.RecordDepositCommitment(amount, currency, operationId, correlationId, committedAtUtc);
				return b;
			});

		var sut = BuildSut(repository);
		var context = new OperationContext(Guid.NewGuid(), Guid.NewGuid(), null, DateTime.UtcNow);
		var command = new RecordPartyDepositCommitmentCommand(booking.Id, 20000, "usd", context);

		var result = await sut.ExecuteAsync(command);

		result.IsSuccess.Should().BeTrue();
		result.Payload.Should().NotBeNull();
		result.Payload!.DepositAmountCents.Should().Be(20000);
		result.Payload.DepositCurrency.Should().Be("USD");
		result.Payload.OperationId.Should().Be(context.OperationId);
		result.Payload.CorrelationId.Should().Be(context.CorrelationId);
	}

	private static RecordPartyDepositCommitmentUseCase BuildSut(Mock<IPartyBookingRepository>? repositoryMock = null)
	{
		var repository = repositoryMock ?? new Mock<IPartyBookingRepository>();
		return new RecordPartyDepositCommitmentUseCase(
			repository.Object,
			new Mock<ILogger<RecordPartyDepositCommitmentUseCase>>().Object);
	}
}
