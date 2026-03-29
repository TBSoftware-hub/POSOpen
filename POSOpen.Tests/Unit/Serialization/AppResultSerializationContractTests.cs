using System.Text.Json;
using FluentAssertions;
using POSOpen.Application.Results;
using POSOpen.Shared.Serialization;

namespace POSOpen.Tests.Unit.Serialization;

public sealed class AppResultSerializationContractTests
{
	[Fact]
	public void Success_result_serializes_to_expected_contract_shape()
	{
		var result = AppResult<string>.Success("terminal-ready", "Foundation ready.");
		var json = JsonSerializer.Serialize(result, AppJsonSerializerOptions.Default);

		json.Should().Contain("\"isSuccess\":true");
		json.Should().Contain("\"errorCode\":null");
		json.Should().Contain("\"userMessage\":\"Foundation ready.\"");
		json.Should().Contain("\"diagnosticMessage\":null");
		json.Should().Contain("\"payload\":\"terminal-ready\"");
	}
}