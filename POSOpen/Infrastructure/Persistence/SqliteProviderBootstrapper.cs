namespace POSOpen.Infrastructure.Persistence;

public static class SqliteProviderBootstrapper
{
	private static readonly object SyncRoot = new();
	private static bool _initialized;

	public static void EnsureInitialized()
	{
		if (_initialized)
		{
			return;
		}

		lock (SyncRoot)
		{
			if (_initialized)
			{
				return;
			}

			SQLitePCL.raw.SetProvider(new SQLitePCL.SQLite3Provider_e_sqlcipher());
			SQLitePCL.Batteries_V2.Init();
			_initialized = true;
		}
	}
}