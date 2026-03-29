namespace POSOpen.Application.Abstractions.Persistence;

public interface IAppDbContextInitializer
{
	Task InitializeAsync(CancellationToken cancellationToken = default);
}