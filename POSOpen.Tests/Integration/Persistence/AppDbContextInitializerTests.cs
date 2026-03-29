using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using POSOpen.Infrastructure.Persistence;

namespace POSOpen.Tests.Integration.Persistence;

public sealed class AppDbContextInitializerTests
{
	[Fact]
	public async Task InitializeAsync_creates_encrypted_database_and_schema()
	{
		var databasePath = TestDatabasePaths.Create();
		await using var dbContextFactory = new TestDbContextFactory(databasePath);
		var initializer = new AppDbContextInitializer(dbContextFactory, NullLogger<AppDbContextInitializer>.Instance);

		await initializer.InitializeAsync();

		File.Exists(databasePath).Should().BeTrue();

		await using var verificationContext = await dbContextFactory.CreateDbContextAsync();
		var tables = await verificationContext.Database
			.SqlQueryRaw<string>("SELECT name FROM sqlite_master WHERE type = 'table' ORDER BY name;")
			.ToListAsync();

		tables.Should().Contain("OperationLogEntries");
		tables.Should().Contain("OutboxMessages");
	}
}