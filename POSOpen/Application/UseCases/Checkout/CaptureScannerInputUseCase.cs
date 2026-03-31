using Microsoft.Extensions.Logging;
using POSOpen.Application.Abstractions.Repositories;
using POSOpen.Application.Abstractions.Services;
using POSOpen.Application.Results;
using POSOpen.Domain.Enums;

namespace POSOpen.Application.UseCases.Checkout;

public sealed class CaptureScannerInputUseCase
{
	private readonly ICartSessionRepository _cartSessionRepository;
	private readonly IFamilyProfileRepository _familyProfileRepository;
	private readonly IScannerDeviceService _scannerDeviceService;
	private readonly IAdmissionPricingService _admissionPricingService;
	private readonly AddCartLineItemUseCase _addCartLineItemUseCase;
	private readonly ILogger<CaptureScannerInputUseCase> _logger;

	public CaptureScannerInputUseCase(
		ICartSessionRepository cartSessionRepository,
		IFamilyProfileRepository familyProfileRepository,
		IScannerDeviceService scannerDeviceService,
		IAdmissionPricingService admissionPricingService,
		AddCartLineItemUseCase addCartLineItemUseCase,
		ILogger<CaptureScannerInputUseCase> logger)
	{
		_cartSessionRepository = cartSessionRepository;
		_familyProfileRepository = familyProfileRepository;
		_scannerDeviceService = scannerDeviceService;
		_admissionPricingService = admissionPricingService;
		_addCartLineItemUseCase = addCartLineItemUseCase;
		_logger = logger;
	}

	public async Task<AppResult<ScannerCaptureResultDto>> ExecuteAsync(Guid cartSessionId, CancellationToken ct = default)
	{
		var captureResult = await _scannerDeviceService.CaptureAsync(ct);
		if (!captureResult.IsSuccess || captureResult.Payload is null)
		{
			_logger.LogWarning(
				"Scanner capture failed for cart {CartSessionId}. ErrorCode: {ErrorCode}",
				cartSessionId,
				captureResult.ErrorCode ?? CartCheckoutConstants.ErrorScannerUnavailable);
			return AppResult<ScannerCaptureResultDto>.Failure(
				captureResult.ErrorCode ?? CartCheckoutConstants.ErrorScannerUnavailable,
				captureResult.UserMessage);
		}

		var cart = await _cartSessionRepository.GetByIdAsync(cartSessionId, ct);
		if (cart is null)
		{
			return AppResult<ScannerCaptureResultDto>.Failure(
				CartCheckoutConstants.ErrorCartNotFound,
				CartCheckoutConstants.SafeCartNotFoundMessage);
		}

		if (captureResult.Payload.Status != ScannerCaptureStatus.Captured)
		{
			_logger.LogWarning(
				"Scanner capture completed with non-success status for cart {CartSessionId}. Status: {Status}, DiagnosticCode: {DiagnosticCode}",
				cartSessionId,
				captureResult.Payload.Status,
				captureResult.Payload.DiagnosticCode);
			return AppResult<ScannerCaptureResultDto>.Success(
				new ScannerCaptureResultDto(
					ScannerResolutionAction.Unresolved,
					GetOrCreateCartSessionUseCase.MapToDto(cart),
					null,
					captureResult.UserMessage,
					captureResult.Payload.DiagnosticCode),
				captureResult.UserMessage);
		}

		var token = captureResult.Payload.Token?.Trim();
		if (string.IsNullOrWhiteSpace(token))
		{
			_logger.LogWarning(
				"Scanner token was empty or whitespace for cart {CartSessionId}. DiagnosticCode: {DiagnosticCode}",
				cartSessionId,
				DeviceDiagnosticCode.ScannerUnresolvedToken);
			return AppResult<ScannerCaptureResultDto>.Success(
				new ScannerCaptureResultDto(
					ScannerResolutionAction.Unresolved,
					GetOrCreateCartSessionUseCase.MapToDto(cart),
					null,
					CartCheckoutConstants.SafeScannerUnresolvedMessage,
					DeviceDiagnosticCode.ScannerUnresolvedToken),
				CartCheckoutConstants.SafeScannerUnresolvedMessage);
		}

		var family = await _familyProfileRepository.GetByScanTokenAsync(token, ct);
		if (family is not null)
		{
			var existingAdmission = cart.LineItems.FirstOrDefault(i => i.ReferenceId == family.Id);
			if (existingAdmission is not null)
			{
				return AppResult<ScannerCaptureResultDto>.Success(
					new ScannerCaptureResultDto(
						ScannerResolutionAction.SelectedExistingItem,
						GetOrCreateCartSessionUseCase.MapToDto(cart),
						existingAdmission.Id,
						$"Selected existing cart item for {family.PrimaryContactFirstName} {family.PrimaryContactLastName}.",
						null),
					"Selected existing cart item from scan.");
			}

			var pricing = await _admissionPricingService.GetAdmissionTotalAsync(family.Id, ct);
			var addResult = await _addCartLineItemUseCase.ExecuteAsync(
				new AddCartLineItemCommand(
					cartSessionId,
					$"Admission - {family.PrimaryContactFirstName} {family.PrimaryContactLastName}",
					FulfillmentContext.Admission,
					family.Id,
					1,
					pricing.AmountCents,
					pricing.CurrencyCode),
				ct);

			if (!addResult.IsSuccess || addResult.Payload is null)
			{
				return AppResult<ScannerCaptureResultDto>.Failure(
					addResult.ErrorCode ?? CartCheckoutConstants.ErrorScannerUnresolved,
					addResult.UserMessage);
			}

			var addedItem = addResult.Payload.LineItems.LastOrDefault(
				i => i.ReferenceId == family.Id && i.FulfillmentContext == FulfillmentContext.Admission);

			return AppResult<ScannerCaptureResultDto>.Success(
				new ScannerCaptureResultDto(
					ScannerResolutionAction.AddedToCart,
					addResult.Payload,
					addedItem?.Id,
					$"Added admission for {family.PrimaryContactFirstName} {family.PrimaryContactLastName} from scan.",
					null),
				"Added admission from scan.");
		}

		if (Guid.TryParse(token, out var referenceId))
		{
			var existingItem = cart.LineItems.FirstOrDefault(i => i.ReferenceId == referenceId);
			if (existingItem is not null)
			{
				return AppResult<ScannerCaptureResultDto>.Success(
					new ScannerCaptureResultDto(
						ScannerResolutionAction.SelectedExistingItem,
						GetOrCreateCartSessionUseCase.MapToDto(cart),
						existingItem.Id,
						"Selected existing cart item from scan.",
						null),
					"Selected existing cart item from scan.");
			}
		}

		return AppResult<ScannerCaptureResultDto>.Success(
			new ScannerCaptureResultDto(
				ScannerResolutionAction.Unresolved,
				GetOrCreateCartSessionUseCase.MapToDto(cart),
				null,
				CartCheckoutConstants.SafeScannerUnresolvedMessage,
				DeviceDiagnosticCode.ScannerUnresolvedToken),
			CartCheckoutConstants.SafeScannerUnresolvedMessage);
	}
}