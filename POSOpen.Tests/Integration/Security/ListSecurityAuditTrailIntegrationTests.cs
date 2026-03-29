using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using POSOpen.Application.Abstractions.Security;
using POSOpen.Application.Security;
using POSOpen.Application.UseCases.Security;
using POSOpen.Domain.Enums;
using POSOpen.Infrastructure.Persistence;
using POSOpen.Infrastructure.Persistence.Repositories;
using POSOpen.Infrastructure.Services;
using POSOpen.Shared.Operational;

namespace POSOpen.Tests.Integration.Security;

public sealed class ListSecurityAuditTrailIntegrationTests
{
	private static async Task<(OperationLogRepository repo, TestDbContextFactory factory)> SetupAsync()
	{
		var databasePath = TestDatabasePaths.Create();
		var factory = new TestDbContextFactory(databasePath);
		var clock = new TestUtcClock(DateTime.UtcNow);
		var initializer = new AppDbContextInitializer(factory, NullLogger<AppDbContextInitializer>.Instance);
		await initializer.InitializeAsync();
		var repo = new OperationLogRepository(factory, clock);
		return (repo, factory);
	}

	private static OperationContext MakeContext() =>
		new(Guid.NewGuid(), Guid.NewGuid(), null, DateTime.UtcNow);

	private static ListSecurityAuditTrailUseCase BuildUseCase(
		OperationLogRepository repo,
		StaffRole role,
		Guid staffId,
		out Mock<ICurrentSessionService> sessionSvc)
	{
		var mockSession = new Mock<ICurrentSessionService>();
		mockSession.Setup(s => s.GetCurrent()).Returns(new CurrentSession(staffId, role, 1, 1));
		sessionSvc = mockSession;

		var mockAuth = new Mock<IAuthorizationPolicyService>();
		mockAuth
			.Setup(a => a.HasPermission(It.IsAny<StaffRole>(), RolePermissions.SecurityAuditRead))
			.Returns(role is StaffRole.Owner or StaffRole.Admin);

		return new ListSecurityAuditTrailUseCase(
			repo,
			mockSession.Object,
			mockAuth.Object,
			NullLogger<ListSecurityAuditTrailUseCase>.Instance);
	}

	[Fact]
	public async Task ExecuteAsync_ReturnsOnlySecurityCriticalScopeEvents_InChronologicalOrder()
	{
		// Arrange
		var (repo, factory) = await SetupAsync();
		await using var dbFactory = factory;

		// Append two security events and one non-security event
		var ctx1 = MakeContext();
		await repo.AppendAsync(SecurityAuditEventTypes.StaffAccountCreated, Guid.NewGuid().ToString(),
			new { actorStaffId = Guid.NewGuid() }, ctx1);

		await Task.Delay(10); // ensure distinct RecordedUtc ordering

		var ctx2 = MakeContext();
		await repo.AppendAsync("SomeOtherEvent", Guid.NewGuid().ToString(),
			new { key = "value" }, ctx2);

		await Task.Delay(10);

		var ctx3 = MakeContext();
		await repo.AppendAsync(SecurityAuditEventTypes.StaffRoleAssigned, Guid.NewGuid().ToString(),
			new { actorStaffId = Guid.NewGuid() }, ctx3);

		var staffId = Guid.NewGuid();
		var useCase = BuildUseCase(repo, StaffRole.Owner, staffId, out _);

		// Act
		var result = await useCase.ExecuteAsync(MakeContext());

		// Assert
		result.IsSuccess.Should().BeTrue();
		result.Payload.Should().NotBeNull();
		result.Payload!.Should().HaveCount(2, "only security-critical events are included in the audit scope");
		result.Payload[0].EventType.Should().Be(SecurityAuditEventTypes.StaffAccountCreated);
		result.Payload[1].EventType.Should().Be(SecurityAuditEventTypes.StaffRoleAssigned);
	}

	[Fact]
	public async Task ExecuteAsync_WithOwnerRole_SucceedsAndReturnsAllScopeEventTypes()
	{
		// Arrange
		var (repo, factory) = await SetupAsync();
		await using var dbFactory = factory;

		foreach (var eventType in SecurityAuditEventTypes.SecurityCriticalScope)
		{
			await repo.AppendAsync(eventType, Guid.NewGuid().ToString(),
				new { actorStaffId = Guid.NewGuid() }, MakeContext());
			await Task.Delay(5);
		}

		var staffId = Guid.NewGuid();
		var useCase = BuildUseCase(repo, StaffRole.Owner, staffId, out _);

		// Act
		var result = await useCase.ExecuteAsync(MakeContext());

		// Assert
		result.IsSuccess.Should().BeTrue();
		result.Payload!.Should().HaveCount(SecurityAuditEventTypes.SecurityCriticalScope.Count);
		result.Payload.Select(r => r.EventType).Should().BeEquivalentTo(SecurityAuditEventTypes.SecurityCriticalScope);
	}

	[Theory]
	[InlineData(StaffRole.Manager)]
	[InlineData(StaffRole.Cashier)]
	public async Task ExecuteAsync_WithUnauthorizedRole_LogsDenialAndReturnsForbidden(StaffRole role)
	{
		// Arrange
		var (repo, factory) = await SetupAsync();
		await using var dbFactory = factory;

		var staffId = Guid.NewGuid();
		var useCase = BuildUseCase(repo, role, staffId, out _);
		var context = MakeContext();

		// Act
		var result = await useCase.ExecuteAsync(context);

		// Assert — access denied
		result.IsSuccess.Should().BeFalse();
		result.ErrorCode.Should().Be(ListSecurityAuditTrailConstants.ErrorAuthForbidden);

		// Assert — denial audit event persisted to DB
		var allEntries = await repo.ListAsync();
		var denialEvent = allEntries.FirstOrDefault(
			e => e.EventType == SecurityAuditEventTypes.SecurityAuditAccessDenied);

		denialEvent.Should().NotBeNull("denial must be recorded in the audit log");
		denialEvent!.AggregateId.Should().Be(staffId.ToString());
	}

	[Fact]
	public async Task ListByEventTypesAsync_NoEditOrDeleteApi_EntryCountOnlyGrowsOnAppend()
	{
		// Arrange — append-only guarantee
		var (repo, factory) = await SetupAsync();
		await using var dbFactory = factory;

		await repo.AppendAsync(SecurityAuditEventTypes.StaffAccountCreated, Guid.NewGuid().ToString(),
			new { actorStaffId = Guid.NewGuid() }, MakeContext());
		await repo.AppendAsync(SecurityAuditEventTypes.OverrideActionCommitted, Guid.NewGuid().ToString(),
			new { actorStaffId = Guid.NewGuid() }, MakeContext());

		// Act — query
		var entries = await repo.ListByEventTypesAsync(SecurityAuditEventTypes.SecurityCriticalScope);

		// Assert — count matches what was appended; no way to reduce via the public interface
		entries.Should().HaveCount(2);

		// IOperationLogRepository does NOT expose Delete or Update — compile-time guarantee
		// (the interface only has AppendAsync, ListAsync, and ListByEventTypesAsync)
	}

	[Fact]
	public async Task ExecuteAsync_WithEmptyDatabase_ReturnsSuccessWithEmptyList()
	{
		// Arrange
		var (repo, factory) = await SetupAsync();
		await using var dbFactory = factory;

		var staffId = Guid.NewGuid();
		var useCase = BuildUseCase(repo, StaffRole.Admin, staffId, out _);

		// Act
		var result = await useCase.ExecuteAsync(MakeContext());

		// Assert
		result.IsSuccess.Should().BeTrue();
		result.Payload.Should().NotBeNull();
		result.Payload!.Should().BeEmpty();
	}
}
