using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VehicleServiceCenter.Data;
using VehicleServiceCenter.Models;
using VehicleServiceCenter.Services;

namespace VehicleServiceCenter.PageModels;

public partial class ServiceRecordListPageModel : ObservableObject
{
    private readonly ServiceRepository _serviceRepository;
    private readonly ModalErrorHandler _errorHandler;

    [ObservableProperty]
    private List<ServiceRecord> _records = [];

    [ObservableProperty]
    private bool _isBusy;

    public ServiceRecordListPageModel(ServiceRepository serviceRepository, ModalErrorHandler errorHandler)
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

    [ObservableProperty]
    private bool _isRefreshing;

    private async Task LoadData()
    {
        try
        {
            IsBusy = true;
            Records = await _serviceRepository.ListAsync();
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