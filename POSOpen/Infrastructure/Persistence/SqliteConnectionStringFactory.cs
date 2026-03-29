using Microsoft.Data.Sqlite;

namespace POSOpen.Infrastructure.Persistence;

public static class SqliteConnectionStringFactory
{
	public static string Create(string databasePath, string password)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);
		ArgumentException.ThrowIfNullOrWhiteSpace(password);

		SqliteProviderBootstrapper.EnsureInitialized();

		var databaseDirectory = Path.GetDirectoryName(databasePath);
		if (!string.IsNullOrWhiteSpace(databaseDirectory))
		{
			Directory.CreateDirectory(databaseDirectory);
		}

		return new SqliteConnectionStringBuilder
		{
			DataSource = databasePath,
			Mode = SqliteOpenMode.ReadWriteCreate,
			Password = password,
			ForeignKeys = true,
			Pooling = false
		}.ToString();
	}
}