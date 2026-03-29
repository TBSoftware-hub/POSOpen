using FluentAssertions;
using POSOpen.Application.Abstractions.Security;
using POSOpen.Application.Security;
using POSOpen.Application.UseCases.Shell;
using POSOpen.Domain.Enums;
using POSOpen.Infrastructure.Security;

namespace POSOpen.Tests.Unit.Security;

public sealed class ExecuteManagerOperationUseCaseTests
{
	[Fact]
	public void Execute_denies_cashier_with_user_safe_message()
	{
		var useCase = new ExecuteManagerOperationUseCase(
			new AuthorizationPolicyService(),
			new TestCurrentSessionService(new CurrentSession(Guid.NewGuid(), StaffRole.Cashier, 1, 1)));

		var result = useCase.Execute();

		result.IsSuccess.Should().BeFalse();
		result.ErrorCode.Should().Be("AUTH_FORBIDDEN");
		result.UserMessage.Should().Be("You do not have access to this action.");
	}

	[Fact]
	public void Execute_denies_when_session_permissions_are_stale()
	{
		var useCase = new ExecuteManagerOperationUseCase(
			new AuthorizationPolicyService(),
			new TestCurrentSessionService(new CurrentSession(Guid.NewGuid(), StaffRole.Manager, 2, 1)));

		var result = useCase.Execute();

		result.IsSuccess.Should().BeFalse();
		result.ErrorCode.Should().Be("AUTH_FORBIDDEN");
	}

	[Fact]
	public void Execute_allows_manager_with_fresh_snapshot()
	{
		var useCase = new ExecuteManagerOperationUseCase(
			new AuthorizationPolicyService(),
			new TestCurrentSessionService(new CurrentSession(Guid.NewGuid(), StaffRole.Manager, 3, 3)));

		var result = useCase.Execute();

		result.IsSuccess.Should().BeTrue();
	}

	private sealed class TestCurrentSessionService : ICurrentSessionService
	{
		private readonly CurrentSession _session;

		public TestCurrentSessionService(CurrentSession session)
		{
			_session = session;
		}

		public CurrentSession? GetCurrent() => _session;

		public void RefreshPermissionSnapshot()
		{
		}

		public long IncrementSessionVersion() => _session.SessionVersion + 1;
	}
}
