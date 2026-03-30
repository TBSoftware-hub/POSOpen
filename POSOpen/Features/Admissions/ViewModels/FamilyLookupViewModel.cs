using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using POSOpen.Application.UseCases.Admissions;

namespace POSOpen.Features.Admissions.ViewModels;

public partial class FamilyLookupViewModel : ObservableObject
{
	private readonly SearchFamiliesUseCase _searchFamiliesUseCase;
	private readonly ILogger<FamilyLookupViewModel> _logger;
	private CancellationTokenSource? _searchDebounce;

	public FamilyLookupViewModel(
		SearchFamiliesUseCase searchFamiliesUseCase,
		ILogger<FamilyLookupViewModel> logger)
	{
		_searchFamiliesUseCase = searchFamiliesUseCase;
		_logger = logger;
	}

	[ObservableProperty]
	private string _searchQuery = string.Empty;

	[ObservableProperty]
	private bool _isLoading;

	[ObservableProperty]
	private bool _hasNoResults;

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(HasError))]
	private string? _errorMessage;

	[ObservableProperty]
	private bool _showCreateNewProfile;

	public ObservableCollection<FamilySearchResultDto> SearchResults { get; } = new();

	public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

	partial void OnSearchQueryChanged(string value)
	{
		_searchDebounce?.Cancel();
		_searchDebounce?.Dispose();
		_searchDebounce = new CancellationTokenSource();
		_ = ExecuteSearchDebouncedAsync(_searchDebounce.Token);
	}

	private async Task ExecuteSearchDebouncedAsync(CancellationToken ct)
	{
		try
		{
			await Task.Delay(300, ct);
			await SearchAsync();
		}
		catch (OperationCanceledException)
		{
		}
	}

	[RelayCommand]
	private async Task SearchAsync()
	{
		var normalizedQuery = SearchQuery.Trim();
		if (normalizedQuery.Length < 2)
		{
			SearchResults.Clear();
			HasNoResults = false;
			ShowCreateNewProfile = false;
			IsLoading = false;
			return;
		}

		IsLoading = true;
		var result = await _searchFamiliesUseCase.ExecuteAsync(new SearchFamiliesQuery(normalizedQuery, FamilyLookupMode.Text));
		IsLoading = false;

		ApplyResult(result, normalizedQuery.Length >= 2);
	}

	[RelayCommand]
	private async Task ScanAsync()
	{
		var token = await global::Microsoft.Maui.Controls.Application.Current!.MainPage!.DisplayPromptAsync(
			"Scan QR Code",
			"Enter QR token:",
			"OK",
			"Cancel",
			maxLength: 64);

		if (string.IsNullOrWhiteSpace(token))
		{
			return;
		}

		SearchQuery = token.Trim();
		IsLoading = true;
		var result = await _searchFamiliesUseCase.ExecuteAsync(new SearchFamiliesQuery(SearchQuery, FamilyLookupMode.Scan));
		IsLoading = false;

		ApplyResult(result, queryLongEnough: true);

		if (result.IsSuccess && result.Payload?.Count == 1)
		{
			var family = result.Payload[0];
			await global::Microsoft.Maui.Controls.Shell.Current.GoToAsync($"{AdmissionsRoutes.FamilyProfile}?familyId={family.Id}");
		}
	}

	[RelayCommand]
	private Task SelectFamilyAsync(FamilySearchResultDto? family)
	{
		if (family is null)
		{
			return Task.CompletedTask;
		}

		return global::Microsoft.Maui.Controls.Shell.Current.GoToAsync(
			$"{AdmissionsRoutes.FastPathCheckIn}?familyId={family.Id}");
	}

	[RelayCommand]
	private async Task CreateNewProfileAsync()
	{
		var encodedQuery = Uri.EscapeDataString(SearchQuery.Trim());
		try
		{
			await global::Microsoft.Maui.Controls.Shell.Current.GoToAsync($"{AdmissionsRoutes.NewProfile}?hint={encodedQuery}");
		}
		catch (Exception ex)
		{
			_logger.LogWarning(ex, "New profile route is not available yet.");
		}
	}

	private void ApplyResult(
		POSOpen.Application.Results.AppResult<IReadOnlyList<FamilySearchResultDto>> result,
		bool queryLongEnough)
	{
		SearchResults.Clear();

		if (!result.IsSuccess || result.Payload is null)
		{
			ErrorMessage = result.UserMessage;
			HasNoResults = false;
			ShowCreateNewProfile = false;
			return;
		}

		foreach (var item in result.Payload)
		{
			SearchResults.Add(item);
		}

		ErrorMessage = null;

		HasNoResults = queryLongEnough && SearchResults.Count == 0;
		ShowCreateNewProfile = HasNoResults;
	}
}
