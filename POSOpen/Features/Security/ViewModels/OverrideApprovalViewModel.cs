using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using POSOpen.Application.Abstractions.Security;
using POSOpen.Application.UseCases.Security;
using POSOpen.Shared.Operational;

namespace POSOpen.Features.Security.ViewModels;

public partial class OverrideApprovalViewModel : ObservableObject
{
	private readonly SubmitOverrideUseCase _submitOverrideUseCase;
	private readonly ICurrentSessionService _currentSessionService;
	private readonly ILogger<OverrideApprovalViewModel> _logger;

	public OverrideApprovalViewModel(
		SubmitOverrideUseCase submitOverrideUseCase,
		ICurrentSessionService currentSessionService,
		ILogger<OverrideApprovalViewModel> logger)
	{
		_submitOverrideUseCase = submitOverrideUseCase;
		_currentSessionService = currentSessionService;
		_logger = logger;
	}

	[ObservableProperty]
	private string _actionKey = string.Empty;

	[ObservableProperty]
	private string _targetReference = string.Empty;

	[ObservableProperty]
	private string _actionContext = string.Empty;

	[ObservableProperty]
	private string _reason = string.Empty;

	[ObservableProperty]
	private bool _isProcessing;

	[ObservableProperty]
	private bool _hasError;

	[ObservableProperty]
	private string _errorMessage = string.Empty;

	[ObservableProperty]
	private bool _isReasonMissing;

	[RelayCommand]
	public async Task SubmitOverrideAsync()
	{
		// Clear previous error state
		HasError = false;
		ErrorMessage = string.Empty;
		IsReasonMissing = false;

		// Validate reason input
		if (string.IsNullOrWhiteSpace(Reason))
		{
			IsReasonMissing = true;
			ErrorMessage = "A reason is required to proceed with this override action.";
			return;
		}

		IsProcessing = true;

		try
		{
			// Create operation context
			var context = new OperationContext(
				Guid.NewGuid(),
				Guid.NewGuid(),
				null,
				DateTime.UtcNow);

			// Execute override use case
			var command = new SubmitOverrideCommand(
				ActionKey,
				TargetReference,
				Reason,
				context);

			var result = await _submitOverrideUseCase.ExecuteAsync(command);

			if (result.IsSuccess)
			{
				_logger.LogInformation(
					"Override action succeeded for ActionKey={ActionKey}, OperationId={OperationId}",
					ActionKey,
					context.OperationId);

				// Navigate away on success
				await Shell.Current.GoToAsync("..");
			}
			else
			{
				HasError = true;
				ErrorMessage = result.UserMessage;

				// Preserve entered reason on transient failures
				_logger.LogWarning(
					"Override action failed: ErrorCode={ErrorCode}, ActionKey={ActionKey}",
					result.ErrorCode,
					ActionKey);
			}
		}
		catch (Exception ex)
		{
			_logger.LogError(
				ex,
				"Exception during override approval: ActionKey={ActionKey}",
				ActionKey);

			HasError = true;
			ErrorMessage = "An unexpected error occurred. Please try again.";
		}
		finally
		{
			IsProcessing = false;
		}
	}

	public void InitializeContext(string actionKey, string targetReference)
	{
		ActionKey = actionKey;
		TargetReference = targetReference;
		ActionContext = $"Action: {actionKey} | Target: {targetReference}";
		
		// Reset input state
		Reason = string.Empty;
		HasError = false;
		ErrorMessage = string.Empty;
		IsReasonMissing = false;
	}
}
