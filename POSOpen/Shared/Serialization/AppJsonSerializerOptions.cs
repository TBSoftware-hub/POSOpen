using System.Text.Json;
using System.Text.Json.Serialization;

namespace POSOpen.Shared.Serialization;

public static class AppJsonSerializerOptions
{
	public static JsonSerializerOptions Default { get; } = Create();

	private static JsonSerializerOptions Create()
	{
		return new JsonSerializerOptions(JsonSerializerDefaults.Web)
		{
			DefaultIgnoreCondition = JsonIgnoreCondition.Never,
			WriteIndented = false
		};
	}
}