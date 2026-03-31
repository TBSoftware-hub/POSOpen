using POSOpen.Application.Abstractions.Repositories;
using POSOpen.Application.Results;
using POSOpen.Domain.Policies;

namespace POSOpen.Application.UseCases.Checkout;

/// <summary>
/// Evaluates all registered <see cref="ICartCompatibilityRule"/> instances against a cart
/// session. Rules are discovered via DI's <c>IEnumerable&lt;ICartCompatibilityRule&gt;</c>
/// registration — add new rules without touching this class.
/// </summary>
public sealed class ValidateCartCompatibilityUseCase(
	ICartSessionRepository repository,
	IEnumerable<ICartCompatibilityRule> rules)
{
	public async Task<AppResult<CartValidationResultDto>> ExecuteAsync(Guid cartSessionId)
	{
		var cart = await repository.GetByIdAsync(cartSessionId);
		if (cart is null)
			return AppResult<CartValidationResultDto>.Failure(
				CartCheckoutConstants.ErrorCartNotFound,
				CartCheckoutConstants.SafeCartNotFoundMessage);

		var issues = rules
			.SelectMany(r => r.Evaluate(cart))
			.Select(i => new CartValidationIssueDto(
				i.Code, i.Severity, i.Message, i.FixLabel, i.FixAction))
			.ToList();

		var resultDto = new CartValidationResultDto(
			IsValid: issues.Count == 0,
			Issues: issues);

		return AppResult<CartValidationResultDto>.Success(resultDto, "Cart validation complete.");
	}
}
