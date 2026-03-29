using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using POSOpen.Application.Abstractions.Services;
using POSOpen.Application.UseCases.Security;

namespace POSOpen.Features.Security.ViewModels;

public partial class SecurityAuditViewModel : ObservableObject
{
	private readonly ListSecurityAuditTrailUseCase _listAuditTrailUseCase;
	private readonly IOperationContextFactory _operationContextFactory;
	private readonly ILogger<SecurityAuditViewModel> _logger;

	public SecurityAuditViewModel(
		ListSecurityAuditTrailUseCase listAuditTrailUseCase,
		IOperationContextFactory operationContextFactory,
		ILogger<SecurityAuditViewModel> logger)
	{
		_listAuditTrailUseCase = listAuditTrailUseCase;
		_operationContextFactory = operationContextFactory;
		_logger = logger;
		AuditRecords.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasRecords));
	}

	[ObservableProperty]
	private bool _isLoading;

	[ObservableProperty]
	private bool _hasError;

	[ObservableProperty]
	private string _errorMessage = string.Empty;

	public ObservableCollection<SecurityAuditRecordDto> AuditRecords { get; } = new();

	public bool HasRecords => AuditRecords.Count > 0;

	[RelayCommand]
	public async Task LoadAsync()
	{
		IsLoading = true;
		HasError = false;
		ErrorMessage = string.Empty;
		AuditRecords.Clear();

		try
		{
			var context = _operationContextFactory.CreateRoot();

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
		catch (Exception ex)
		{
			HasError = true;
			ErrorMessage = "The security audit trail is temporarily unavailable. Please try again.";
			_logger.LogError(ex, "Unexpected error while loading security audit trail.");
		}
		finally
		{
			IsLoading = false;
		}
	}
}
