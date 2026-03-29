using FluentAssertions;
using POSOpen.Infrastructure.Services;

namespace POSOpen.Tests.Unit.Operational;

public sealed class OperationContextFactoryTests
{
	[Fact]
	public void CreateRoot_generates_operation_and_correlation_ids_with_utc_timestamp()
	{
		var timestamp = new DateTime(2026, 3, 28, 9, 30, 0, DateTimeKind.Utc);
		var factory = new OperationContextFactory(new TestUtcClock(timestamp));

		var operationContext = factory.CreateRoot();

		operationContext.OperationId.Should().NotBe(Guid.Empty);
		operationContext.CorrelationId.Should().NotBe(Guid.Empty);
		operationContext.CausationId.Should().BeNull();
		operationContext.OccurredUtc.Should().Be(timestamp);
		operationContext.OccurredUtc.Kind.Should().Be(DateTimeKind.Utc);
	}
}