using FluentAssertions;
using Moq;
using POSOpen.Application.Abstractions.Repositories;
using POSOpen.Application.Security;
using POSOpen.Application.UseCases.Party;
using POSOpen.Domain.Enums;
using POSOpen.Infrastructure.Persistence;
using POSOpen.Infrastructure.Persistence.Repositories;
using POSOpen.Shared.Operational;

namespace POSOpen.Tests.Integration.Party;

public sealed class PartyBookingRepositoryTests
{
[Fact]
public async Task UpsertDraftAndConfirm_PersistsLifecycleAndTraceabilityFields()
{
await using var fixture = await CreateFixtureAsync();
var partyDate = new DateTime(2026, 4, 8, 10, 0, 0, DateTimeKind.Utc);
var createUseCase = new CreateDraftPartyBookingUseCase(fixture.Repository, fixture.Clock, NullLoggerFactory.CreateLogger<CreateDraftPartyBookingUseCase>());
var confirmUseCase = new ConfirmPartyBookingUseCase(fixture.Repository, new Mock<IOperationLogRepository>().Object, fixture.Clock, NullLoggerFactory.CreateLogger<ConfirmPartyBookingUseCase>());

var draftContext = new OperationContext(Guid.NewGuid(), Guid.NewGuid(), null, fixture.Clock.UtcNow);
var draft = await createUseCase.ExecuteAsync(new CreateDraftPartyBookingCommand(null, partyDate, "10:00", "basic-party", draftContext));
draft.IsSuccess.Should().BeTrue();

var confirmContext = new OperationContext(Guid.NewGuid(), Guid.NewGuid(), null, fixture.Clock.UtcNow.AddMinutes(1));
var confirmed = await confirmUseCase.ExecuteAsync(new ConfirmPartyBookingCommand(draft.Payload!.BookingId, confirmContext));
confirmed.IsSuccess.Should().BeTrue();

var persisted = await fixture.Repository.GetByIdAsync(draft.Payload.BookingId);
persisted.Should().NotBeNull();
persisted!.Status.Should().Be(PartyBookingStatus.Booked);
persisted.OperationId.Should().Be(confirmContext.OperationId);
persisted.CorrelationId.Should().Be(confirmContext.CorrelationId);
persisted.BookedAtUtc.Should().NotBeNull();
}

[Fact]
public async Task CreateDraft_WithConcurrentSameSlotRequests_OnlyOneSucceeds()
{
await using var fixture = await CreateFixtureAsync();
var partyDate = new DateTime(2026, 4, 10, 13, 0, 0, DateTimeKind.Utc);

var firstUseCase = new CreateDraftPartyBookingUseCase(fixture.Repository, fixture.Clock, NullLoggerFactory.CreateLogger<CreateDraftPartyBookingUseCase>());
var secondUseCase = new CreateDraftPartyBookingUseCase(fixture.Repository, fixture.Clock, NullLoggerFactory.CreateLogger<CreateDraftPartyBookingUseCase>());

var firstTask = firstUseCase.ExecuteAsync(new CreateDraftPartyBookingCommand(
null,
partyDate,
"13:00",
"deluxe-party",
new OperationContext(Guid.NewGuid(), Guid.NewGuid(), null, fixture.Clock.UtcNow)));

var secondTask = secondUseCase.ExecuteAsync(new CreateDraftPartyBookingCommand(
null,
partyDate,
"13:00",
"vip-party",
new OperationContext(Guid.NewGuid(), Guid.NewGuid(), null, fixture.Clock.UtcNow.AddSeconds(1))));

var results = await Task.WhenAll(firstTask, secondTask);
results.Count(x => x.IsSuccess).Should().Be(1);
results.Count(x => !x.IsSuccess).Should().Be(1);
results.Single(x => !x.IsSuccess).ErrorCode.Should().Be(PartyBookingConstants.ErrorSlotUnavailable);
}

[Fact]
public async Task ConfirmPartyBookingUseCase_WhenSlotConflictAtConfirmTime_PersistsConfirmationDeniedAuditEvent()
{
await using var fixture = await CreateFixtureAsync();
var bookingId = Guid.NewGuid();
var partyDate = new DateTime(2026, 4, 15, 10, 0, 0, DateTimeKind.Utc);

var stubRepo = new Mock<IPartyBookingRepository>();
stubRepo.Setup(x => x.GetByIdAsync(bookingId, It.IsAny<CancellationToken>()))
.ReturnsAsync(POSOpen.Domain.Entities.PartyBooking.CreateDraft(
bookingId, partyDate, "10:00", "basic-party",
Guid.NewGuid(), Guid.NewGuid(), fixture.Clock.UtcNow));
stubRepo.Setup(x => x.IsSlotUnavailableAsync(
It.IsAny<DateTime>(), It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
.ReturnsAsync(true);

var useCase = new ConfirmPartyBookingUseCase(
stubRepo.Object,
fixture.OperationLogRepository,
fixture.Clock,
NullLoggerFactory.CreateLogger<ConfirmPartyBookingUseCase>());

var context = new OperationContext(Guid.NewGuid(), Guid.NewGuid(), null, fixture.Clock.UtcNow);
var result = await useCase.ExecuteAsync(new ConfirmPartyBookingCommand(bookingId, context));

result.IsSuccess.Should().BeFalse();
result.ErrorCode.Should().Be(PartyBookingConstants.ErrorSlotUnavailable);

var logs = await fixture.OperationLogRepository.ListAsync();
var deniedEvent = logs.FirstOrDefault(x =>
x.EventType == SecurityAuditEventTypes.PartyBookingConfirmationDenied);
deniedEvent.Should().NotBeNull();
deniedEvent!.AggregateId.Should().Be(bookingId.ToString());
}

private static async Task<TestFixture> CreateFixtureAsync()
{
var databasePath = TestDatabasePaths.Create();
var dbContextFactory = new TestDbContextFactory(databasePath);
var initializer = new AppDbContextInitializer(dbContextFactory, NullLoggerFactory.CreateLogger<AppDbContextInitializer>());
await initializer.InitializeAsync();

var clock = new TestUtcClock(new DateTime(2026, 4, 8, 9, 0, 0, DateTimeKind.Utc));
var operationLogRepository = new OperationLogRepository(dbContextFactory, clock);
return new TestFixture(dbContextFactory, new PartyBookingRepository(dbContextFactory), clock, operationLogRepository);
}

private sealed class TestFixture : IAsyncDisposable
{
public TestFixture(TestDbContextFactory dbContextFactory, PartyBookingRepository repository, TestUtcClock clock, OperationLogRepository operationLogRepository)
{
DbContextFactory = dbContextFactory;
Repository = repository;
Clock = clock;
OperationLogRepository = operationLogRepository;
}

public TestDbContextFactory DbContextFactory { get; }
public PartyBookingRepository Repository { get; }
public TestUtcClock Clock { get; }
public OperationLogRepository OperationLogRepository { get; }

public async ValueTask DisposeAsync()
{
await DbContextFactory.DisposeAsync();
}
}

private static class NullLoggerFactory
{
public static Microsoft.Extensions.Logging.ILogger<T> CreateLogger<T>() where T : class
{
return Microsoft.Extensions.Logging.Abstractions.NullLogger<T>.Instance;
}
}
}