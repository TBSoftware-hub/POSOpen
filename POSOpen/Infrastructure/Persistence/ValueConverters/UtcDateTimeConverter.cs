using System.Globalization;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace POSOpen.Infrastructure.Persistence.ValueConverters;

public sealed class UtcDateTimeConverter : ValueConverter<DateTime, string>
{
	public static readonly UtcDateTimeConverter Instance = new();

	public UtcDateTimeConverter()
		: base(
			value => value.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture),
			value => DateTime.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal))
	{
	}
}

public sealed class NullableUtcDateTimeConverter : ValueConverter<DateTime?, string?>
{
	public static readonly NullableUtcDateTimeConverter Instance = new();

	public NullableUtcDateTimeConverter()
		: base(
			value => value.HasValue ? value.Value.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture) : null,
			value => string.IsNullOrWhiteSpace(value)
				? null
				: DateTime.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal))
	{
	}
}