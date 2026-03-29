using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using POSOpen.Application.UseCases.Security;
using POSOpen.Shared.Operational;

namespace POSOpen.Features.Security.ViewModels;

public partial class SecurityAuditViewModel : ObservableObject
{
	private readonly ListSecurityAuditTrailUseCase _listAuditTrailUseCase;
	private readonly ILogger<SecurityAuditViewModel> _logger;

	public SecurityAuditViewModel(
		ListSecurityAuditTrailUseCase listAuditTrailUseCase,
		ILogger<SecurityAuditViewModel> logger)
	{
		_listAuditTrailUseCase = listAuditTrailUseCase;
		_logger = logger;
	}

	[ObservableProperty]
	private bool _isLoading;

	[ObservableProperty]
	private bool _hasError;

	[ObservableProperty]
	private string _errorMessage = string.Empty;

	public ObservableCollection<SecurityAuditRecordDto> AuditRecords { get; } = new();

	[RelayCommand]
	public async Task LoadAsync()
	{
		IsLoading = true;
		HasError = false;
		ErrorMessage = string.Empty;
		AuditRecords.Clear();

		try
		{
			var context = new OperationContext(
				Guid.NewGuid(),
				Guid.NewGuid(),
				null,
				DateTime.UtcNow);

			var result = await _listAuditTrailUseCase.ExecuteAsync(context);

			if (result.IsSuccess && result.Payload is not null)
			{
				foreach (var record in result.Payload)
				{
					AuditRecords.Add(record);
				}
			}
			else
			{
				HasError = true;
				ErrorMessage = result.UserMessage;
				_logger.LogWarning("Security audit trail load failed: {ErrorCode}", result.ErrorCode);
			}
		}
		finally
		{
			IsLoading = false;
		}
	}
}
