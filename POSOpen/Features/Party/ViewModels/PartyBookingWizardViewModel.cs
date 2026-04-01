using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using POSOpen.Application.Abstractions.Services;
using POSOpen.Application.UseCases.Party;
using POSOpen.Domain.Enums;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;

namespace POSOpen.Features.Party.ViewModels;

public sealed partial class PartyBookingWizardViewModel : ObservableObject
{
	private readonly GetBookingAvailabilityUseCase _getBookingAvailabilityUseCase;
	private readonly CreateDraftPartyBookingUseCase _createDraftPartyBookingUseCase;
	private readonly ConfirmPartyBookingUseCase _confirmPartyBookingUseCase;
	private readonly IOperationContextFactory _operationContextFactory;
	private readonly ILogger<PartyBookingWizardViewModel> _logger;

	private Guid? _bookingId;

	public Guid? BookingId => _bookingId;

	public PartyBookingWizardViewModel(
		GetBookingAvailabilityUseCase getBookingAvailabilityUseCase,
		CreateDraftPartyBookingUseCase createDraftPartyBookingUseCase,
		ConfirmPartyBookingUseCase confirmPartyBookingUseCase,
		IOperationContextFactory operationContextFactory,
		ILogger<PartyBookingWizardViewModel> logger)
	{
		_getBookingAvailabilityUseCase = getBookingAvailabilityUseCase;
		_createDraftPartyBookingUseCase = createDraftPartyBookingUseCase;
		_confirmPartyBookingUseCase = confirmPartyBookingUseCase;
		_operationContextFactory = operationContextFactory;
		_logger = logger;

		AvailablePackages.Add("basic-party");
		AvailablePackages.Add("deluxe-party");
		AvailablePackages.Add("vip-party");
		SelectedPackageId = AvailablePackages[0];
	}

	[ObservableProperty]
	private DateTime _partyDateUtc = DateTime.UtcNow.Date.AddDays(1);

	[ObservableProperty]
	private BookingSlotAvailabilityDto? _selectedSlot;

	[ObservableProperty]
	private string? _selectedPackageId;

	[ObservableProperty]
	private int _currentStepIndex;

	[ObservableProperty]
	private string _processingState = "Idle";

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(HasError))]
	private string? _errorMessage;

	[ObservableProperty]
	private string _statusMessage = string.Empty;

	[ObservableProperty]
	private bool _isBusy;

	[ObservableProperty]
	private bool _isBookingConfirmed;

	public ObservableCollection<BookingSlotAvailabilityDto> AvailableSlots { get; } = [];

	public ObservableCollection<string> AvailablePackages { get; } = [];

	public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

	public string? SelectedSlotId => SelectedSlot?.SlotId;

	public bool IsDateStep => CurrentStepIndex == 0;

	public bool IsTimeStep => CurrentStepIndex == 1;

	public bool IsPackageStep => CurrentStepIndex == 2;

	public bool IsReviewStep => CurrentStepIndex == 3;

	partial void OnCurrentStepIndexChanged(int value)
	{
		OnPropertyChanged(nameof(IsDateStep));
		OnPropertyChanged(nameof(IsTimeStep));
		OnPropertyChanged(nameof(IsPackageStep));
		OnPropertyChanged(nameof(IsReviewStep));
	}

	partial void OnSelectedSlotChanged(BookingSlotAvailabilityDto? value)
	{
		OnPropertyChanged(nameof(SelectedSlotId));
	}

	[RelayCommand]
	private async Task InitializeAsync()
	{
		SetBusyState("Loading");
		ErrorMessage = null;
		StatusMessage = string.Empty;
		CurrentStepIndex = 0;

		try
		{
			await LoadAvailabilityAsync();
			SetSuccessState(PartyBookingConstants.WizardInitialMessage);
		}
		catch (Exception ex)
		{
			SetErrorState(PartyBookingConstants.SafeCommitFailedMessage);
			_logger.LogError(ex, "Unexpected error initializing party booking wizard.");
		}
		finally
		{
			IsBusy = false;
		}
	}

	[RelayCommand]
	private async Task ContinueFromDateAsync()
	{
		if (IsBusy)
		{
			return;
		}

		if (PartyDateUtc.Date < DateTime.UtcNow.Date)
		{
			SetErrorState(PartyBookingConstants.SafeDateInvalidMessage);
			return;
		}

		SetBusyState("Loading");
		try
		{
			await LoadAvailabilityAsync();
			CurrentStepIndex = 1;
			SetSuccessState(PartyBookingConstants.WizardSelectTimeMessage);
		}
		catch (Exception ex)
		{
			SetErrorState(PartyBookingConstants.SafeCommitFailedMessage);
			_logger.LogError(ex, "Unexpected error loading availability for party booking wizard.");
		}
		finally
		{
			IsBusy = false;
		}
	}

	[RelayCommand]
	private void ContinueFromTime()
	{
		if (IsBusy)
		{
			return;
		}

		if (SelectedSlot is null)
		{
			SetErrorState(PartyBookingConstants.SafeSlotRequiredMessage);
			return;
		}

		if (!SelectedSlot.IsAvailable)
		{
			SetErrorState(PartyBookingConstants.SafeSlotUnavailableMessage);
			return;
		}

		CurrentStepIndex = 2;
		SetSuccessState(PartyBookingConstants.WizardSelectPackageMessage);
	}

	[RelayCommand]
	private async Task ContinueFromPackageAsync()
	{
		if (IsBusy)
		{
			return;
		}

		if (string.IsNullOrWhiteSpace(SelectedPackageId))
		{
			SetErrorState(PartyBookingConstants.SafePackageRequiredMessage);
			return;
		}

		if (SelectedSlot is null)
		{
			SetErrorState(PartyBookingConstants.SafeSlotRequiredMessage);
			return;
		}

		SetBusyState("Loading");
		try
		{
			var operation = _operationContextFactory.CreateRoot();
			var result = await _createDraftPartyBookingUseCase.ExecuteAsync(
				new CreateDraftPartyBookingCommand(
					_bookingId,
					PartyDateUtc,
					SelectedSlot.SlotId,
					SelectedPackageId,
					operation));

			if (!result.IsSuccess || result.Payload is null)
			{
				SetErrorState(result.UserMessage);
				return;
			}

			_bookingId = result.Payload.BookingId;
			CurrentStepIndex = 3;
			SetSuccessState(result.UserMessage);
		}
		finally
		{
			IsBusy = false;
		}
	}

	[RelayCommand]
	private async Task ConfirmAsync()
	{
		if (IsBusy || _bookingId is null)
		{
			return;
		}

		SetBusyState("Loading");
		try
		{
			var operation = _operationContextFactory.CreateRoot();
			var result = await _confirmPartyBookingUseCase.ExecuteAsync(
				new ConfirmPartyBookingCommand(_bookingId.Value, operation));

			if (!result.IsSuccess || result.Payload is null)
			{
				SetErrorState(result.UserMessage);
				return;
			}

			IsBookingConfirmed = true;
			SetSuccessState(result.UserMessage);
		}
		finally
		{
			IsBusy = false;
		}
	}

	[RelayCommand]
	private void Back()
	{
		if (IsBusy)
		{
			return;
		}

		if (CurrentStepIndex > 0)
		{
			CurrentStepIndex--;
		}
	}

	private async Task LoadAvailabilityAsync()
	{
		var availability = await _getBookingAvailabilityUseCase.ExecuteAsync(PartyDateUtc);
		if (!availability.IsSuccess || availability.Payload is null)
		{
			SetErrorState(availability.UserMessage);
			return;
		}

		AvailableSlots.Clear();
		foreach (var slot in availability.Payload.Slots)
		{
			AvailableSlots.Add(slot);
		}

		if (SelectedSlot is null || AvailableSlots.All(x => x.SlotId != SelectedSlot.SlotId))
		{
			SelectedSlot = AvailableSlots.FirstOrDefault(x => x.IsAvailable);
		}
	}

	private void SetBusyState(string state)
	{
		IsBusy = true;
		ErrorMessage = null;
		ProcessingState = state;
	}

	private void SetErrorState(string message)
	{
		ProcessingState = "Error";
		ErrorMessage = message;
		StatusMessage = message;
	}

	private void SetSuccessState(string message)
	{
		ProcessingState = "Success";
		ErrorMessage = null;
		StatusMessage = message;
	}
}
