namespace POSOpen.Application.UseCases.Party;

internal static class BookingRiskEvaluator
{
	public static IReadOnlyList<BookingRiskIndicatorDto> EvaluateRisks(IEnumerable<AddOnSelectionItemCommand> selections)
	{
		var risks = new List<BookingRiskIndicatorDto>();
		foreach (var selection in selections)
		{
			var (isAtRisk, severity, reason) = GetRiskInfo(selection.OptionId);
			if (!isAtRisk || severity is null || reason is null)
			{
				continue;
			}

			risks.Add(new BookingRiskIndicatorDto(selection.OptionId, severity, reason));
		}

		return risks;
	}

	public static (bool IsAtRisk, string? Severity, string? Reason) GetRiskInfo(string optionId)
	{
		if (!PartyBookingConstants.AtRiskOptionMeta.TryGetValue(optionId, out var meta))
		{
			return (false, null, null);
		}

		return (true, meta.Severity, meta.Reason);
	}
}
