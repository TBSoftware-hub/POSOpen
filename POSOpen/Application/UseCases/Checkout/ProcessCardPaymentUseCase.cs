using Microsoft.Extensions.Logging;
using POSOpen.Application.Abstractions.Repositories;
using POSOpen.Application.Abstractions.Services;
using POSOpen.Application.Results;
using POSOpen.Domain.Entities;
using POSOpen.Domain.Enums;

namespace POSOpen.Application.UseCases.Checkout;

public sealed class ProcessCardPaymentUseCase
{
	private readonly ICartSessionRepository _cartSessionRepository;
	private readonly ValidateCartCompatibilityUseCase _validateCartCompatibilityUseCase;
	private readonly ICheckoutPaymentAttemptRepository _checkoutPaymentAttemptRepository;
	private readonly ICardReaderDeviceService _cardReaderDeviceService;
	private readonly IUtcClock _clock;
	private readonly ILogger<ProcessCardPaymentUseCase> _logger;

	public ProcessCardPaymentUseCase(
		ICartSessionRepository cartSessionRepository,
		ValidateCartCompatibilityUseCase validateCartCompatibilityUseCase,
		ICheckoutPaymentAttemptRepository checkoutPaymentAttemptRepository,
		ICardReaderDeviceService cardReaderDeviceService,
		IUtcClock clock,
		ILogger<ProcessCardPaymentUseCase> logger)
	{
		_cartSessionRepository = cartSessionRepository;
		_validateCartCompatibilityUseCase = validateCartCompatibilityUseCase;
		_checkoutPaymentAttemptRepository = checkoutPaymentAttemptRepository;
		_cardReaderDeviceService = cardReaderDeviceService;
		_clock = clock;
		_logger = logger;
	}

	public async Task<AppResult<CheckoutPaymentResultDto>> ExecuteAsync(Guid cartSessionId, CancellationToken ct = default)
	{
		var cart = await _cartSessionRepository.GetByIdAsync(cartSessionId, ct);
		if (cart is null)
		{
			return AppResult<CheckoutPaymentResultDto>.Failure(
				CartCheckoutConstants.ErrorCartNotFound,
				CartCheckoutConstants.SafeCartNotFoundMessage);
		}

		if (cart.Status != CartStatus.Open)
		{
			return AppResult<CheckoutPaymentResultDto>.Failure(
				CartCheckoutConstants.ErrorCartNotOpen,
				CartCheckoutConstants.SafeCartNotOpenMessage);
		}

		if (cart.LineItems.Count == 0)
		{
			return AppResult<CheckoutPaymentResultDto>.Failure(
				CartCheckoutConstants.ErrorCartEmpty,
				CartCheckoutConstants.SafeCartEmptyMessage);
		}

		var compatibilityResult = await _validateCartCompatibilityUseCase.ExecuteAsync(cartSessionId);
		if (!compatibilityResult.IsSuccess || compatibilityResult.Payload is null)
		{
			return AppResult<CheckoutPaymentResultDto>.Failure(
				compatibilityResult.ErrorCode ?? CartCheckoutConstants.ErrorCartNotFound,
				compatibilityResult.UserMessage);
		}

		if (!compatibilityResult.Payload.IsValid)
		{
			var firstIssue = compatibilityResult.Payload.Issues.First();
			return AppResult<CheckoutPaymentResultDto>.Failure(firstIssue.Code, firstIssue.Message);
		}

		var currencyCode = cart.LineItems.FirstOrDefault()?.CurrencyCode ?? "USD";
		var authorizationResult = await _cardReaderDeviceService.AuthorizeAsync(
			new CardAuthorizationRequest(cart.Id, cart.TotalAmountCents, currencyCode),
			ct);

		if (!authorizationResult.IsSuccess || authorizationResult.Payload is null)
		{
			_logger.LogWarning(
				"Card reader authorization request failed for cart {CartSessionId}. ErrorCode: {ErrorCode}",
				cart.Id,
				authorizationResult.ErrorCode ?? CartCheckoutConstants.ErrorCardReaderUnavailable);
			return AppResult<CheckoutPaymentResultDto>.Failure(
				authorizationResult.ErrorCode ?? CartCheckoutConstants.ErrorCardReaderUnavailable,
				authorizationResult.UserMessage);
		}

		var attempt = CheckoutPaymentAttempt.Create(
			Guid.NewGuid(),
			cart.Id,
			cart.TotalAmountCents,
			currencyCode,
			authorizationResult.Payload.Status,
			authorizationResult.Payload.ProcessorReference,
			authorizationResult.Payload.DiagnosticCode,
			_clock.UtcNow);

		var persistedAttempt = await _checkoutPaymentAttemptRepository.AddAsync(attempt, ct);
		var result = new CheckoutPaymentResultDto(
			CheckoutPaymentAttemptDto.FromEntity(persistedAttempt),
			persistedAttempt.AuthorizationStatus == CheckoutPaymentAuthorizationStatus.Approved);

		var userMessage = persistedAttempt.AuthorizationStatus switch
		{
			CheckoutPaymentAuthorizationStatus.Approved => "Card authorized successfully.",
			CheckoutPaymentAuthorizationStatus.Declined => CartCheckoutConstants.SafeCardAuthorizationDeclinedMessage,
			CheckoutPaymentAuthorizationStatus.Unavailable => CartCheckoutConstants.SafeCardReaderUnavailableMessage,
			CheckoutPaymentAuthorizationStatus.Faulted => CartCheckoutConstants.SafeCardReaderFaultedMessage,
			CheckoutPaymentAuthorizationStatus.Cancelled => CartCheckoutConstants.SafeCardAuthorizationCancelledMessage,
			_ => CartCheckoutConstants.SafeCardReaderFaultedMessage,
		};

		if (persistedAttempt.AuthorizationStatus != CheckoutPaymentAuthorizationStatus.Approved)
		{
			_logger.LogWarning(
				"Card payment authorization did not approve for cart {CartSessionId}. Status: {Status}, DiagnosticCode: {DiagnosticCode}",
				cart.Id,
				persistedAttempt.AuthorizationStatus,
				persistedAttempt.DiagnosticCode);
		}

		return AppResult<CheckoutPaymentResultDto>.Success(result, userMessage);
	}
}