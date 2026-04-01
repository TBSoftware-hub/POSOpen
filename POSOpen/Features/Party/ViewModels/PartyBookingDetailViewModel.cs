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
	private readonly IOperationContextFactory _operationContextFactory;

	public PartyBookingDetailViewModel(
		GetPartyBookingTimelineUseCase getPartyBookingTimelineUseCase,
		RecordPartyDepositCommitmentUseCase recordPartyDepositCommitmentUseCase,
		MarkPartyBookingCompletedUseCase markPartyBookingCompletedUseCase,
		IOperationContextFactory operationContextFactory)
	{
		_getPartyBookingTimelineUseCase = getPartyBookingTimelineUseCase;
		_recordPartyDepositCommitmentUseCase = recordPartyDepositCommitmentUseCase;
		_markPartyBookingCompletedUseCase = markPartyBookingCompletedUseCase;
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

	public ObservableCollection<PartyBookingTimelineMilestoneDto> Milestones { get; } = [];

	public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

	public bool CanSubmitDeposit => !DepositCommitted && !IsBusy;

	public async Task LoadAsync(Guid bookingId)
	{
		BookingId = bookingId;
		await RefreshTimelineAsync();
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
