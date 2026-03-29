using System.Globalization;

namespace POSOpen.Shared.Converters;

public sealed class InverseBoolConverter : IValueConverter
{
	public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		return value is bool boolValue ? !boolValue : true;
	}

	public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		return value is bool boolValue ? !boolValue : true;
	}
}
