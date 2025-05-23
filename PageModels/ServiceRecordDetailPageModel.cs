using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VehicleServiceCenter.Data;
using VehicleServiceCenter.Models;
using VehicleServiceCenter.Services;

namespace VehicleServiceCenter.PageModels;

public partial class ServiceRecordDetailPageModel : ObservableObject, IQueryAttributable
{
    private readonly ServiceRepository _serviceRepository;
    private readonly ModalErrorHandler _errorHandler;
    private ServiceRecord? _record;

    [ObservableProperty]
    private string _receiptNumber = string.Empty;

    [ObservableProperty]
    private DateTime _date = DateTime.Now;

    [ObservableProperty]
    private string _customerName = string.Empty;

    [ObservableProperty]
    private string _mobileNumber = string.Empty;

    [ObservableProperty]
    private string _address = string.Empty;

    [ObservableProperty]
    private string _vehicleNumber = string.Empty;

    [ObservableProperty]
    private double _currentReading;

    [ObservableProperty]
    private double _nextReading;

    [ObservableProperty]
    private List<ServiceItem> _items = [];

    [ObservableProperty]
    private double _total;

    [ObservableProperty]
    private string _mechanicName = string.Empty;

    [ObservableProperty]
    private string _mechanicContact = string.Empty;

    [ObservableProperty]
    private bool _canDelete;

    [ObservableProperty]
    private bool _isBusy;

    public ServiceRecordDetailPageModel(ServiceRepository serviceRepository, ModalErrorHandler errorHandler)
    {
        _serviceRepository = serviceRepository;
        _errorHandler = errorHandler;
    }

    private async Task LoadData(int id)
    {
        try
        {
            IsBusy = true;
            _record = await _serviceRepository.GetAsync(id);

            if (_record == null)
            {
                _errorHandler.HandleError(new Exception($"Service record with id {id} not found."));
                return;
            }

            ReceiptNumber = _record.ReceiptNumber;
            Date = _record.Date;
            CustomerName = _record.CustomerName;
            MobileNumber = _record.MobileNumber;
            Address = _record.Address;
            VehicleNumber = _record.VehicleNumber;
            CurrentReading = _record.CurrentReading;
            NextReading = _record.NextReading;
            Items = _record.Items;
            Total = _record.Total;
            MechanicName = _record.MechanicName;
            MechanicContact = _record.MechanicContact;
            CanDelete = true;
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

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.ContainsKey("id"))
        {
            var id = Convert.ToInt32(query["id"]);
            LoadData(id).FireAndForgetSafeAsync(_errorHandler);
        }
        else
        {
            _record = new ServiceRecord
            {
                Date = DateTime.Now,
                ReceiptNumber = GenerateReceiptNumber()
            };
        }
    }

    private string GenerateReceiptNumber()
    {
        // Generate a receipt number in format: YYYYMMDD-XXX where XXX is a random number
        return $"{DateTime.Now:yyyyMMdd}-{Random.Shared.Next(100, 999)}";
    }

    [RelayCommand]
    private void AddItem()
    {
        var item = new ServiceItem();
        Items = new List<ServiceItem>(Items) { item };
    }

    [RelayCommand]
    private void RemoveItem(ServiceItem item)
    {
        var items = Items.ToList();
        items.Remove(item);
        Items = items;
        UpdateTotal();
    }

    [RelayCommand]
    private async Task Save()
    {
        if (_record == null)
        {
            _errorHandler.HandleError(new Exception("Invalid state: record is null"));
            return;
        }

        try
        {
            IsBusy = true;

            _record.ReceiptNumber = ReceiptNumber;
            _record.Date = Date;
            _record.CustomerName = CustomerName;
            _record.MobileNumber = MobileNumber;
            _record.Address = Address;
            _record.VehicleNumber = VehicleNumber;
            _record.CurrentReading = CurrentReading;
            _record.NextReading = NextReading;
            _record.Items = Items;
            _record.Total = Total;
            _record.MechanicName = MechanicName;
            _record.MechanicContact = MechanicContact;

            await _serviceRepository.SaveAsync(_record);
            await Shell.Current.GoToAsync("..");
            await AppShell.DisplayToastAsync("Service record saved successfully");
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

    [RelayCommand(CanExecute = nameof(CanDelete))]
    private async Task Delete()
    {
        if (_record == null)
            return;

        try
        {
            IsBusy = true;
            await _serviceRepository.DeleteAsync(_record);
            await Shell.Current.GoToAsync("..");
            await AppShell.DisplayToastAsync("Service record deleted successfully");
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

    partial void OnItemsChanged(List<ServiceItem> value)
    {
        UpdateTotal();
    }

    private void UpdateTotal()
    {
        Total = Items.Sum(i => i.Amount);
    }
}