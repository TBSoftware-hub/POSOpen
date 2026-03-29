namespace POSOpen.Tests;

public static class TestDatabasePaths
{
	public static string Create()
	{
		var directory = Path.Combine(Path.GetTempPath(), "POSOpen.Tests", Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(directory);
		return Path.Combine(directory, "posopen-test.db");
	}
}