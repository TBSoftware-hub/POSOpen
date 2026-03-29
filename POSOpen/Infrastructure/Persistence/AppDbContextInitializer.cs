using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using POSOpen.Application.Abstractions.Persistence;

namespace POSOpen.Infrastructure.Persistence;

public sealed class AppDbContextInitializer : IAppDbContextInitializer
{
	private readonly IDbContextFactory<PosOpenDbContext> _dbContextFactory;
	private readonly ILogger<AppDbContextInitializer> _logger;

	public AppDbContextInitializer(
		IDbContextFactory<PosOpenDbContext> dbContextFactory,
		ILogger<AppDbContextInitializer> logger)
	{
		_dbContextFactory = dbContextFactory;
		_logger = logger;
	}

	public async Task InitializeAsync(CancellationToken cancellationToken = default)
	{
		await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
		var migrations = dbContext.Database.GetMigrations().ToArray();

		if (migrations.Any())
		{
			await dbContext.Database.MigrateAsync(cancellationToken);
			_logger.LogInformation("Applied {MigrationCount} EF Core migrations.", migrations.Count());
			return;
		}

		await dbContext.Database.EnsureCreatedAsync(cancellationToken);
		_logger.LogInformation("Created encrypted SQLite baseline without migrations.");
	}
}