using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VehicleServiceCenter.Data;
using VehicleServiceCenter.Models;
using VehicleServiceCenter.Services;

namespace VehicleServiceCenter.PageModels;

public partial class ServiceRecordListPageModel : ObservableObject
{    private readonly IServiceRepository _serviceRepository;
    private readonly IErrorHandler _errorHandler;

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

    partial void OnSearchQueryChanged(string value)
    {
        FilterRecords();
    }

    private void FilterRecords()
    {
        if (string.IsNullOrWhiteSpace(SearchQuery))
        {
            FilteredRecords = Records.ToList();
            return;
        }        var query = SearchQuery.Trim().ToLowerInvariant();
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

    public ServiceRecordListPageModel(IServiceRepository serviceRepository, IErrorHandler errorHandler)
    {
        _serviceRepository = serviceRepository;
        _errorHandler = errorHandler;
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
    }

    private async Task LoadData()
    {
        try
        {
            IsBusy = true;
            Records = await _serviceRepository.ListAsync();
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
        => Shell.Current.GoToAsync($"service?id={record.ID}");

    [RelayCommand]
    private Task AddRecord()
        => Shell.Current.GoToAsync("service");
}