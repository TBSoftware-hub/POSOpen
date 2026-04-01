using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using POSOpen.Application.Abstractions.Services;
using POSOpen.Application.UseCases.Party;

namespace POSOpen.Features.Party.ViewModels;

public sealed partial class PartyBookingDetailViewModel : ObservableObject
{
	private readonly GetPartyBookingTimelineUseCase _getPartyBookingTimelineUseCase;
	private readonly RecordPartyDepositCommitmentUseCase _recordPartyDepositCommitmentUseCase;
	private readonly MarkPartyBookingCompletedUseCase _markPartyBookingCompletedUseCase;
	private readonly GetRoomOptionsUseCase _getRoomOptionsUseCase;
	private readonly AssignPartyRoomUseCase _assignPartyRoomUseCase;
	private readonly GetBookingAddOnOptionsUseCase _getBookingAddOnOptionsUseCase;
	private readonly UpdateBookingAddOnSelectionsUseCase _updateBookingAddOnSelectionsUseCase;
	private readonly IOperationContextFactory _operationContextFactory;

	public PartyBookingDetailViewModel(
		GetPartyBookingTimelineUseCase getPartyBookingTimelineUseCase,
		RecordPartyDepositCommitmentUseCase recordPartyDepositCommitmentUseCase,
		MarkPartyBookingCompletedUseCase markPartyBookingCompletedUseCase,
		GetRoomOptionsUseCase getRoomOptionsUseCase,
		AssignPartyRoomUseCase assignPartyRoomUseCase,
		GetBookingAddOnOptionsUseCase getBookingAddOnOptionsUseCase,
		UpdateBookingAddOnSelectionsUseCase updateBookingAddOnSelectionsUseCase,
		IOperationContextFactory operationContextFactory)
	{
		_getPartyBookingTimelineUseCase = getPartyBookingTimelineUseCase;
		_recordPartyDepositCommitmentUseCase = recordPartyDepositCommitmentUseCase;
		_markPartyBookingCompletedUseCase = markPartyBookingCompletedUseCase;
		_getRoomOptionsUseCase = getRoomOptionsUseCase;
		_assignPartyRoomUseCase = assignPartyRoomUseCase;
		_getBookingAddOnOptionsUseCase = getBookingAddOnOptionsUseCase;
		_updateBookingAddOnSelectionsUseCase = updateBookingAddOnSelectionsUseCase;
		_operationContextFactory = operationContextFactory;
	}

	[ObservableProperty]
	private Guid _bookingId;

	[ObservableProperty]
	private string _processingState = "Idle";

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(CanSubmitDeposit))]
	private bool _isBusy;

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(HasError))]
	private string? _errorMessage;

	[ObservableProperty]
	private string _statusMessage = string.Empty;

	[ObservableProperty]
	private string _depositAmountInput = string.Empty;

	[ObservableProperty]
	private string _depositCurrency = "USD";

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(CanSubmitDeposit))]
	private bool _depositCommitted;

	[ObservableProperty]
	private ObservableCollection<RoomOptionItemDto> _roomOptions = [];

	[ObservableProperty]
	private RoomOptionItemDto? _selectedRoom;

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(HasConflict))]
	private string? _conflictMessage;

	[ObservableProperty]
	private ObservableCollection<string> _alternativeRooms = [];

	[ObservableProperty]
	private ObservableCollection<string> _alternativeSlots = [];

	[ObservableProperty]
	private string? _assignedRoomId;

	[ObservableProperty]
	private ObservableCollection<AddOnOptionItemDto> _cateringOptions = [];

	[ObservableProperty]
	private ObservableCollection<AddOnOptionItemDto> _decorOptions = [];

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(HasRisks))]
	private ObservableCollection<BookingRiskIndicatorDto> _riskIndicators = [];

	[ObservableProperty]
	private long _addOnTotalAmountCents;

	[ObservableProperty]
	private bool _isAddOnBusy;

	[ObservableProperty]
	private string? _addOnStatusMessage;

	public ObservableCollection<PartyBookingTimelineMilestoneDto> Milestones { get; } = [];

	private DateTime _partyDateUtc;
	private string _partySlotId = string.Empty;

	public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

	public bool HasConflict => !string.IsNullOrWhiteSpace(ConflictMessage);

	public bool HasRisks => RiskIndicators.Count > 0;

	public bool CanSubmitDeposit => !DepositCommitted && !IsBusy;

	public async Task LoadAsync(Guid bookingId)
	{
		BookingId = bookingId;
		await RefreshTimelineAsync();
		await LoadRoomOptionsCommand.ExecuteAsync(null);
		await LoadAddOnOptionsCommand.ExecuteAsync(null);
	}

	[RelayCommand]
	private async Task RefreshTimelineAsync()
	{
		if (IsBusy || BookingId == Guid.Empty)
		{
			return;
		}

		SetBusyState();
		try
		{
			var timelineResult = await _getPartyBookingTimelineUseCase.ExecuteAsync(BookingId);
			if (!timelineResult.IsSuccess || timelineResult.Payload is null)
			{
				SetErrorState(timelineResult.UserMessage);
				return;
			}

			Milestones.Clear();
			foreach (var milestone in timelineResult.Payload.Milestones)
			{
				Milestones.Add(milestone);
			}

			_partyDateUtc = timelineResult.Payload.PartyDateUtc;
			_partySlotId = timelineResult.Payload.SlotId;
			DepositCommitted = timelineResult.Payload.IsDepositCommitted;
			SetSuccessState(timelineResult.UserMessage);
		}
		finally
		{
			IsBusy = false;
		}
	}

	[RelayCommand]
	private async Task SubmitDepositCommitmentAsync()
	{
		if (!CanSubmitDeposit || BookingId == Guid.Empty)
		{
			return;
		}

		if (!long.TryParse(DepositAmountInput, out var amountCents) || amountCents <= 0)
		{
			SetErrorState(PartyBookingConstants.SafeDepositAmountInvalidMessage);
			return;
		}

		SetBusyState();
		try
		{
			var operation = _operationContextFactory.CreateRoot();
			var result = await _recordPartyDepositCommitmentUseCase.ExecuteAsync(
				new RecordPartyDepositCommitmentCommand(
					BookingId,
					amountCents,
					DepositCurrency,
					operation));

			if (!result.IsSuccess)
			{
				SetErrorState(result.UserMessage);
				return;
			}

			SetSuccessState(result.UserMessage);
			await RefreshTimelineAsync();
		}
		finally
		{
			IsBusy = false;
		}
	}

	[RelayCommand]
	private async Task MarkCompletedAsync()
	{
		if (IsBusy || BookingId == Guid.Empty)
		{
			return;
		}

		SetBusyState();
		try
		{
			var operation = _operationContextFactory.CreateRoot();
			var result = await _markPartyBookingCompletedUseCase.ExecuteAsync(
				new MarkPartyBookingCompletedCommand(BookingId, operation));

			if (!result.IsSuccess)
			{
				SetErrorState(result.UserMessage);
				return;
			}

			SetSuccessState(result.UserMessage);
			await RefreshTimelineAsync();
		}
		finally
		{
			IsBusy = false;
		}
	}

	[RelayCommand]
	private async Task LoadRoomOptionsAsync()
	{
		if (BookingId == Guid.Empty)
		{
			return;
		}

		try
		{
			var query = new GetRoomOptionsQuery(_partyDateUtc, _partySlotId);
			var result = await _getRoomOptionsUseCase.ExecuteAsync(query);
			RoomOptions.Clear();
			ConflictMessage = null;
			AlternativeRooms.Clear();
			AlternativeSlots.Clear();
			if (result.IsSuccess && result.Payload is not null)
			{
				foreach (var room in result.Payload.Rooms)
				{
					RoomOptions.Add(room);
				}
			}
		}
		catch (Exception ex)
		{
			System.Diagnostics.Trace.TraceError(
				$"Failed to load room options for booking '{BookingId}': {ex}");
		}
	}

	[RelayCommand]
	private async Task AssignRoomAsync()
	{
		if (SelectedRoom is null || BookingId == Guid.Empty)
		{
			return;
		}

		SetBusyState();
		try
		{
			var operation = _operationContextFactory.CreateRoot();
			var result = await _assignPartyRoomUseCase.ExecuteAsync(
				new AssignPartyRoomCommand(BookingId, SelectedRoom.RoomId, operation));

			if (!result.IsSuccess)
			{
				if (result.ErrorCode == PartyBookingConstants.ErrorRoomConflict && result.Payload is not null)
				{
					ConflictMessage = result.UserMessage;
					AlternativeRooms.Clear();
					foreach (var r in result.Payload.AlternativeRooms ?? [])
					{
						AlternativeRooms.Add(r);
					}
					AlternativeSlots.Clear();
					foreach (var s in result.Payload.AlternativeSlots ?? [])
					{
						AlternativeSlots.Add(s);
					}
				}
				else
				{
					SetErrorState(result.UserMessage);
				}
				return;
			}

			ConflictMessage = null;
			AlternativeRooms.Clear();
			AlternativeSlots.Clear();
			AssignedRoomId = result.Payload?.AssignedRoomId;
			SetSuccessState(result.UserMessage);
			await RefreshTimelineAsync();
		}
		finally
		{
			IsBusy = false;
		}
	}

	[RelayCommand]
	private async Task LoadAddOnOptionsAsync()
	{
		if (BookingId == Guid.Empty)
		{
			return;
		}

		IsAddOnBusy = true;
		try
		{
			var result = await _getBookingAddOnOptionsUseCase.ExecuteAsync(new GetBookingAddOnOptionsQuery(BookingId));
			if (!result.IsSuccess || result.Payload is null)
			{
				AddOnStatusMessage = result.UserMessage;
				return;
			}

			ApplyAddOnPayload(result.Payload, []);
			AddOnStatusMessage = result.UserMessage;
		}
		finally
		{
			IsAddOnBusy = false;
		}
	}

	[RelayCommand]
	private void ToggleAddOnOption(string optionId)
	{
		if (string.IsNullOrWhiteSpace(optionId))
		{
			return;
		}

		if (TryToggleOption(CateringOptions, optionId))
		{
			RecalculateAddOnTotal();
			return;
		}

		if (TryToggleOption(DecorOptions, optionId))
		{
			RecalculateAddOnTotal();
		}
	}

	[RelayCommand]
	private async Task SaveAddOnSelectionsAsync()
	{
		if (BookingId == Guid.Empty || IsAddOnBusy)
		{
			return;
		}

		var selected = CateringOptions
			.Where(option => option.IsSelected)
			.Select(option => new AddOnSelectionItemCommand(option.OptionId, option.AddOnType, 1))
			.Concat(DecorOptions
				.Where(option => option.IsSelected)
				.Select(option => new AddOnSelectionItemCommand(option.OptionId, option.AddOnType, 1)))
			.ToArray();

		IsAddOnBusy = true;
		try
		{
			var operation = _operationContextFactory.CreateRoot();
			var result = await _updateBookingAddOnSelectionsUseCase.ExecuteAsync(
				new UpdateBookingAddOnSelectionsCommand(BookingId, selected, operation));

			if (!result.IsSuccess || result.Payload is null)
			{
				AddOnStatusMessage = result.UserMessage;
				return;
			}

			ApplyAddOnPayload(
				new BookingAddOnOptionsDto(
					result.Payload.BookingId,
					result.Payload.CateringOptions,
					result.Payload.DecorOptions,
					result.Payload.AddOnTotalAmountCents),
				result.Payload.RiskIndicators);

			Milestones.Clear();
			foreach (var milestone in result.Payload.UpdatedMilestones)
			{
				Milestones.Add(milestone);
			}

			AddOnStatusMessage = result.UserMessage;
		}
		finally
		{
			IsAddOnBusy = false;
		}
	}

	private static bool TryToggleOption(ObservableCollection<AddOnOptionItemDto> options, string optionId)
	{
		for (var index = 0; index < options.Count; index++)
		{
			if (!string.Equals(options[index].OptionId, optionId, StringComparison.Ordinal))
			{
				continue;
			}

			var current = options[index];
			options[index] = current with
			{
				IsSelected = !current.IsSelected,
				Quantity = !current.IsSelected ? 1 : 0,
			};
			return true;
		}

		return false;
	}

	private void ApplyAddOnPayload(BookingAddOnOptionsDto payload, IReadOnlyList<BookingRiskIndicatorDto> riskIndicators)
	{
		CateringOptions = new ObservableCollection<AddOnOptionItemDto>(payload.CateringOptions);
		DecorOptions = new ObservableCollection<AddOnOptionItemDto>(payload.DecorOptions);
		var resolvedRisks = riskIndicators.Count > 0
			? riskIndicators
			: payload.CateringOptions
				.Concat(payload.DecorOptions)
				.Where(option => option.IsSelected && option.IsAtRisk && !string.IsNullOrWhiteSpace(option.RiskSeverity) && !string.IsNullOrWhiteSpace(option.RiskReason))
				.Select(option => new BookingRiskIndicatorDto(option.OptionId, option.RiskSeverity!, option.RiskReason!))
				.ToArray();

		RiskIndicators = new ObservableCollection<BookingRiskIndicatorDto>(resolvedRisks);
		AddOnTotalAmountCents = payload.AddOnTotalAmountCents;
	}

	private void RecalculateAddOnTotal()
	{
		var selected = CateringOptions
			.Concat(DecorOptions)
			.Where(option => option.IsSelected)
			.ToArray();

		AddOnTotalAmountCents = selected.Sum(option => option.PriceCents * option.Quantity);
		RiskIndicators = new ObservableCollection<BookingRiskIndicatorDto>(
			selected
				.Where(option => option.IsAtRisk && !string.IsNullOrWhiteSpace(option.RiskSeverity) && !string.IsNullOrWhiteSpace(option.RiskReason))
				.Select(option => new BookingRiskIndicatorDto(option.OptionId, option.RiskSeverity!, option.RiskReason!)));
	}

	private void SetBusyState()
	{
		IsBusy = true;
		ErrorMessage = null;
		ProcessingState = "Loading";
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
