namespace POSOpen.Application.Results;

public sealed record AppResult<TPayload>(
	bool IsSuccess,
	string? ErrorCode,
	string UserMessage,
	string? DiagnosticMessage,
	TPayload? Payload)
{
	public static AppResult<TPayload> Success(TPayload payload, string userMessage) =>
		new(true, null, userMessage, null, payload);

	public static AppResult<TPayload> Failure(string errorCode, string userMessage, string? diagnosticMessage = null) =>
		new(false, errorCode, userMessage, diagnosticMessage, default);
}