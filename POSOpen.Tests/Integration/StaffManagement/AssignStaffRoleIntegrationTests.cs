using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using POSOpen.Application.Security;
using POSOpen.Application.UseCases.Shell;
using POSOpen.Application.UseCases.StaffManagement;
using POSOpen.Domain.Entities;
using POSOpen.Domain.Enums;
using POSOpen.Infrastructure.Persistence;
using POSOpen.Infrastructure.Persistence.Repositories;
using POSOpen.Infrastructure.Security;
using POSOpen.Infrastructure.Services;
using POSOpen.Shared.Operational;

namespace POSOpen.Tests.Integration.StaffManagement;

public sealed class AssignStaffRoleIntegrationTests
{
	[Fact]
	public async Task Assign_role_persists_and_requires_session_refresh_for_fresh_permissions()
	{
		var databasePath = TestDatabasePaths.Create();
		await using var dbContextFactory = new TestDbContextFactory(databasePath);
		var initializer = new AppDbContextInitializer(dbContextFactory, NullLogger<AppDbContextInitializer>.Instance);
		await initializer.InitializeAsync();

		var repository = new StaffAccountRepository(dbContextFactory);
		var operationLogs = new OperationLogRepository(dbContextFactory, new TestUtcClock(DateTime.UtcNow));
		var appState = new AppStateService();
		appState.SetCurrentSession(Guid.NewGuid(), StaffRole.Owner, 1);
		var sessionService = new AppStateCurrentSessionService(appState);
		var policyService = new AuthorizationPolicyService();
		var assignUseCase = new AssignStaffRoleUseCase(
			repository,
			operationLogs,
			policyService,
			sessionService,
			NullLogger<AssignStaffRoleUseCase>.Instance);

		var now = new DateTime(2026, 3, 29, 18, 0, 0, DateTimeKind.Utc);
		var account = StaffAccount.Create(Guid.NewGuid(), "Jordan", "River", "jordan@example.com", "hash", "salt", StaffRole.Cashier, StaffAccountStatus.Active, now, now, null, null);
		await repository.AddAsync(account);

		var assignResult = await assignUseCase.ExecuteAsync(new AssignStaffRoleCommand(
			account.Id,
			StaffRole.Manager,
			new OperationContext(Guid.NewGuid(), Guid.NewGuid(), null, now.AddMinutes(1))));
		assignResult.IsSuccess.Should().BeTrue();

		var updated = await repository.GetByIdAsync(account.Id);
		updated.Should().NotBeNull();
		updated!.Role.Should().Be(StaffRole.Manager);

		var staleSessionManagerUseCase = new ExecuteManagerOperationUseCase(policyService, sessionService);
		var staleResult = staleSessionManagerUseCase.Execute();
		staleResult.IsSuccess.Should().BeFalse();
		staleResult.ErrorCode.Should().Be("AUTH_FORBIDDEN");

		sessionService.RefreshPermissionSnapshot();
		appState.SetCurrentSession(updated.Id, updated.Role, appState.SessionVersion);
		sessionService.RefreshPermissionSnapshot();

		var freshSessionManagerUseCase = new ExecuteManagerOperationUseCase(policyService, sessionService);
		var freshResult = freshSessionManagerUseCase.Execute();
		freshResult.IsSuccess.Should().BeTrue();
	}
}
