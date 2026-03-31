using POSOpen.Domain.Entities;

namespace POSOpen.Domain.Policies;

/// <summary>
/// A single cart compatibility rule. Register each implementation as
/// <c>AddTransient&lt;ICartCompatibilityRule, ConcreteRule&gt;()</c> so that
/// <see cref="POSOpen.Application.UseCases.Checkout.ValidateCartCompatibilityUseCase"/>
/// discovers them via <c>IEnumerable&lt;ICartCompatibilityRule&gt;</c> injection.
/// </summary>
public interface ICartCompatibilityRule
{
	IReadOnlyList<CartValidationIssue> Evaluate(CartSession cart);
}
