using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System.Security.Cryptography;
using System.Text;

namespace POSOpen.Infrastructure.Persistence.DesignTime;

public sealed class PosOpenDesignTimeDbContextFactory : IDesignTimeDbContextFactory<PosOpenDbContext>
{
	public PosOpenDbContext CreateDbContext(string[] args)
	{
		var password = Environment.GetEnvironmentVariable("POSOPEN_SQLITE_PASSWORD")
			?? "posopen-design-time-dev-only";

		var databaseDirectory = Path.Combine(Path.GetTempPath(), "POSOpen", "DesignTime");
		Directory.CreateDirectory(databaseDirectory);

		var keyHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(password)))[..12];
		var databasePath = Path.Combine(databaseDirectory, $"posopen-design-{keyHash}.db");

		var connectionString = SqliteConnectionStringFactory.Create(databasePath, password);
		var options = new DbContextOptionsBuilder<PosOpenDbContext>()
			.UseSqlite(connectionString, sqlite =>
			{
				sqlite.MigrationsAssembly(typeof(PosOpenDbContext).Assembly.FullName);
			})
			.Options;

		return new PosOpenDbContext(options);
	}
}