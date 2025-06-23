using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VehicleServiceCenter.Data;
using VehicleServiceCenter.Models;
using VehicleServiceCenter.Services;

namespace VehicleServiceCenter.PageModels;

public partial class ServiceRecordListPageModel : ObservableObject
{
    private readonly IServiceRepository _serviceRepository;
    private readonly IErrorHandler _errorHandler;
    private readonly IPreferences _preferences;

    [ObservableProperty]
    private List<ServiceRecord> _records = [];

    [ObservableProperty]
    private List<ServiceRecord> _filteredRecords = [];

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private bool _isRefreshing;

    [ObservableProperty]
    private bool _isLoadingMore;

    [ObservableProperty]
    private bool _hasMoreRecords;

    [ObservableProperty]
    private bool _isLimitedView;

    private int? _currentLimit;    [ObservableProperty]
    private bool _isSearchActive;    partial void OnSearchQueryChanged(string value)
    {
        _ = FilterRecordsAsync();
    }

    private void FilterRecords()
    {
        // Create a fire-and-forget task for the async method
        _ = FilterRecordsAsync();
    }

    private async Task FilterRecordsAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchQuery))
        {
            // Clear search - revert to limited view if needed
            if (IsSearchActive)
            {
                IsSearchActive = false;
                await LoadData(); // Reload with limits
                return;
            }
            
            FilteredRecords = Records.ToList();
            return;
        }
        
        try
        {
            // If we're searching, load all records to search through everything
            if (!IsSearchActive)
            {
                IsSearchActive = true;
                IsBusy = true;
                
                // Load all records for searching
                Records = await _serviceRepository.ListAsync();
            }
            
            var query = SearchQuery.Trim().ToLowerInvariant();
            FilteredRecords = Records.Where(r =>
            {
                // Check primary fields
                if (r.ReceiptNumber?.ToLowerInvariant().Contains(query) == true ||
                    r.VehicleNumber?.ToLowerInvariant().Contains(query) == true ||
                    r.MobileNumber?.ToLowerInvariant().Contains(query) == true)
                    return true;

                // Check date in multiple formats
                var dateStr1 = r.ReceiptDate.ToString("MMM dd, yyyy").ToLowerInvariant(); // Jun 21, 2025
                var dateStr2 = r.ReceiptDate.ToString("dd/MM").ToLowerInvariant();        // 21/06
                var dateStr3 = r.ReceiptDate.ToString("dd/MM/yy").ToLowerInvariant();     // 21/06/25
                var dateStr4 = r.ReceiptDate.ToString("dd/MM/yyyy").ToLowerInvariant();   // 21/06/2025

                return dateStr1.Contains(query) || 
                       dateStr2.Contains(query) || 
                       dateStr3.Contains(query) || 
                       dateStr4.Contains(query);
            }).ToList();
        }
        finally
        {
            IsBusy = false;
        }
    }public ServiceRecordListPageModel(IServiceRepository serviceRepository, IErrorHandler errorHandler, IPreferences preferences)
    {
        _serviceRepository = serviceRepository;
        _errorHandler = errorHandler;
        _preferences = preferences;
    }

    [RelayCommand]
    private async Task Appearing()
    {
        await LoadData();
    }

    [RelayCommand]
    private async Task Refresh()
    {
        IsRefreshing = true;
        await LoadData();
        IsRefreshing = false;
    }    private async Task LoadData()
    {
        try
        {
            IsBusy = true;
            
            // Get the default record limit from settings
            var defaultLimit = _preferences.Get("DefaultRecordLimit", 20);
            _currentLimit = defaultLimit > 0 ? defaultLimit : null;
            
            Records = await _serviceRepository.ListAsync(_currentLimit);
            
            // Check if we might have more records
            HasMoreRecords = _currentLimit.HasValue && Records.Count >= _currentLimit.Value;
            IsLimitedView = _currentLimit.HasValue;
            
            FilterRecords(); // Apply any existing search filter
        }
        catch (Exception ex)
        {
            _errorHandler.HandleError(ex);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private Task NavigateToRecord(ServiceRecord record)
        => Shell.Current.GoToAsync($"service?id={record.ID}");    [RelayCommand]
    private Task AddRecord()
        => Shell.Current.GoToAsync("service");

    [RelayCommand]
    private async Task LoadMoreRecords()
    {
        if (IsLoadingMore || !HasMoreRecords)
            return;

        try
        {
            IsLoadingMore = true;
            
            // Load all records when "Load More" is clicked
            var allRecords = await _serviceRepository.ListAsync();
            Records = allRecords;
            
            HasMoreRecords = false;
            IsLimitedView = false;
            _currentLimit = null;
            
            FilterRecords(); // Apply any existing search filter
        }
        catch (Exception ex)
        {
            _errorHandler.HandleError(ex);
        }
        finally
        {
            IsLoadingMore = false;
        }
    }
}