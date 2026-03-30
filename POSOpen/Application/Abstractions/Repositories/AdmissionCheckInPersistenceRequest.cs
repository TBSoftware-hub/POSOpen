using POSOpen.Domain.Entities;
using POSOpen.Shared.Operational;

namespace POSOpen.Application.Abstractions.Repositories;

public sealed record AdmissionCheckInPersistenceRequest(
	AdmissionCheckInRecord Record,
	string OperationLogEventType,
	object OperationLogPayload,
	OperationContext OperationContext,
	string? OutboxEventType,
	object? OutboxPayload);
