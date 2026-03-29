using Microsoft.EntityFrameworkCore;
using POSOpen.Infrastructure.Persistence;

namespace POSOpen.Tests;

public sealed class TestDbContextFactory : IDbContextFactory<PosOpenDbContext>, IAsyncDisposable
{
	private readonly string _databasePath;

	public TestDbContextFactory(string databasePath)
	{
		_databasePath = databasePath;
	}

	public PosOpenDbContext CreateDbContext()
	{
		var connectionString = SqliteConnectionStringFactory.Create(_databasePath, "test-encryption-key");
		var options = new DbContextOptionsBuilder<PosOpenDbContext>()
			.UseSqlite(connectionString, sqlite =>
			{
				sqlite.MigrationsAssembly(typeof(PosOpenDbContext).Assembly.FullName);
			})
			.Options;

		return new PosOpenDbContext(options);
	}

	public ValueTask<PosOpenDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
	{
		return ValueTask.FromResult(CreateDbContext());
	}

	public ValueTask DisposeAsync()
	{
		var databaseDirectory = Path.GetDirectoryName(_databasePath);
		if (!string.IsNullOrWhiteSpace(databaseDirectory) && Directory.Exists(databaseDirectory))
		{
			Directory.Delete(databaseDirectory, recursive: true);
		}

		return ValueTask.CompletedTask;
	}
}