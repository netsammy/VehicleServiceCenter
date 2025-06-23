using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VehicleServiceCenter.Data;
using VehicleServiceCenter.Models;
using VehicleServiceCenter.Services;

namespace VehicleServiceCenter.PageModels;

public partial class ServiceRecordDetailPageModel : ObservableObject, IQueryAttributable
{
    private readonly IServiceRepository _serviceRepository;
    private readonly IErrorHandler _errorHandler;
    private ServiceRecord? _record;

    [ObservableProperty]
    private string _receiptNumber = string.Empty;

    [ObservableProperty]
    private DateTime _receiptDate = DateTime.Today;

    [ObservableProperty]
    private string _customerName = string.Empty;

    [ObservableProperty]
    private string _mobileNumber = string.Empty;

    [ObservableProperty]
    private string _address = string.Empty;

    [ObservableProperty]
    private string _vehicleNumber = string.Empty;

    [ObservableProperty]
    private string _vehicleMake = string.Empty;

    [ObservableProperty]
    private string _vehicleModel = string.Empty;

    [ObservableProperty]
    private double _currentReading;

    [ObservableProperty]
    private double _nextReading;

    [ObservableProperty]
    private ObservableCollection<ServiceItem> _items = new();

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

    [ObservableProperty]
    private bool _hasReceiptNumberError;

    [ObservableProperty]
    private string _receiptNumberError = string.Empty;

    [ObservableProperty]
    private bool _hasVehicleNumberError;    [ObservableProperty]
    private string _vehicleNumberError = string.Empty;    // Manual command property for testing
    public ICommand RemoveItemCommand { get; private set; } = null!;    public ServiceRecordDetailPageModel(IServiceRepository serviceRepository, IErrorHandler errorHandler)
    {
        _serviceRepository = serviceRepository;
        _errorHandler = errorHandler;
        Items.CollectionChanged += Items_CollectionChanged;
        
        // Initialize the command manually
        RemoveItemCommand = new AsyncRelayCommand<ServiceItem>(RemoveItemAsync);
    }

    private async Task GenerateReceiptNumber()
    {
        try
        {
            ReceiptNumber = await _serviceRepository.GenerateReceiptNumberAsync();
        }
        catch (Exception ex)
        {
            _errorHandler.HandleError(ex);
        }
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("id", out var id))
        {
            LoadData(Convert.ToInt32(id));
        }
        else
        {
            _record = new ServiceRecord
            {
                ReceiptDate = DateTime.Today
            };

            // Generate receipt number for new records
            _ = GenerateReceiptNumber();
            AddItem();
        }
    }

    private async void LoadData(int id)
    {
        try
        {
            IsBusy = true;

            _record = await _serviceRepository.GetAsync(id);
            if (_record == null)
            {
                await Shell.Current.GoToAsync("..");
                return;
            }

            ReceiptNumber = _record.ReceiptNumber;
            ReceiptDate = _record.ReceiptDate;
            CustomerName = _record.CustomerName;
            MobileNumber = _record.MobileNumber;
            Address = _record.Address;
            VehicleNumber = _record.VehicleNumber;
            VehicleMake = _record.VehicleMake;
            VehicleModel = _record.VehicleModel;
            CurrentReading = _record.CurrentReading;
            NextReading = _record.NextReading;
            Items = new ObservableCollection<ServiceItem>(_record.Items);
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

    [RelayCommand]
    private void AddItem()
    {
        var newItem = new ServiceItem
        {
            Quantity = 1,  // Set default quantity to 1
            Rate = 0      // Set default rate to 0
        };
        newItem.PropertyChanged += Item_PropertyChanged;
        Items.Add(newItem);
        UpdateTotal();
    }    private async Task RemoveItemAsync(ServiceItem? item)
    {
        if (item == null) return;

        bool answer = await Shell.Current.DisplayAlert(
            "Confirm Delete",
            "Do you want to delete this service item?",
            "Yes",
            "No");

        if (answer)
        {
            Items.Remove(item);
            // Total will be updated via collection changed event
        }
    }

    [RelayCommand]
    private async Task Save()
    {
        if (_record == null)
        {
            _errorHandler.HandleError(new Exception("Invalid state: record is null"));
            return;
        }

        var errors = ValidateFields();
        if (errors.Any())
        {
            _errorHandler.HandleError(new Exception(string.Join("\n", errors)));
            return;
        }

        try
        {
            IsBusy = true;

            _record.ReceiptNumber = ReceiptNumber;
            _record.ReceiptDate = ReceiptDate;
            _record.CustomerName = CustomerName;
            _record.MobileNumber = MobileNumber;
            _record.Address = Address;
            _record.VehicleNumber = VehicleNumber;
            _record.VehicleMake = VehicleMake;
            _record.VehicleModel = VehicleModel;
            _record.CurrentReading = CurrentReading;
            _record.NextReading = NextReading;
            _record.Items = Items.ToList();
            _record.Total = Total;
            _record.MechanicName = MechanicName;
            _record.MechanicContact = MechanicContact;

            await _serviceRepository.SaveAsync(_record);
            await Shell.Current.GoToAsync("..");
            await Shell.Current.DisplayAlert("Success", "Service record saved successfully", "OK");
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
            await Shell.Current.DisplayAlert("Success", "Service record deleted successfully", "OK");
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

    private void Items_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems != null)
        {
            foreach (ServiceItem item in e.OldItems)
            {
                item.PropertyChanged -= Item_PropertyChanged;
            }
        }

        if (e.NewItems != null)
        {
            foreach (ServiceItem item in e.NewItems)
            {
                item.PropertyChanged += Item_PropertyChanged;
            }
        }

        UpdateTotal();
    }

    partial void OnReceiptNumberChanged(string value)
    {
        HasReceiptNumberError = string.IsNullOrWhiteSpace(value);
        ReceiptNumberError = HasReceiptNumberError ? "Receipt number is required" : string.Empty;
    }

    partial void OnVehicleNumberChanged(string value)
    {
        HasVehicleNumberError = string.IsNullOrWhiteSpace(value);
        VehicleNumberError = HasVehicleNumberError ? "Vehicle number is required" : string.Empty;
    }

    private void Item_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ServiceItem.Amount))
        {
            UpdateTotal();
        }
    }

    private void UpdateTotal()
    {
        Total = Items.Sum(x => x.Amount);
    }

    private List<string> ValidateFields()
    {
        var errors = new List<string>();

        if (HasReceiptNumberError)
        {
            errors.Add(ReceiptNumberError);
        }

        if (HasVehicleNumberError)
        {
            errors.Add(VehicleNumberError);
        }

        if (!Items.Any())
        {
            errors.Add("At least one service item is required");
        }

        return errors;
    }
}