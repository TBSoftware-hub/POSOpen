using POSOpen.Domain.Enums;

namespace POSOpen.Application.UseCases.Inventory;

internal static class InventorySubstitutionPolicyRoleCodec
{
	public static string NormalizeToCsv(IReadOnlyCollection<StaffRole> roles)
	{
		ArgumentNullException.ThrowIfNull(roles);

		var normalized = roles
			.Distinct()
			.OrderBy(static role => role)
			.Select(static role => role.ToString())
			.ToArray();

		return string.Join(',', normalized);
	}

	public static IReadOnlyList<StaffRole> ParseCsv(string rolesCsv)
	{
		if (string.IsNullOrWhiteSpace(rolesCsv))
		{
			return [];
		}

		var output = new List<StaffRole>();
		foreach (var value in rolesCsv.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
		{
			if (Enum.TryParse<StaffRole>(value, out var parsed))
			{
				output.Add(parsed);
			}
		}

		return output
			.Distinct()
			.OrderBy(static role => role)
			.ToArray();
	}
}
