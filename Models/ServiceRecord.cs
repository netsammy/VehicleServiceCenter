using System.ComponentModel.DataAnnotations;
using CommunityToolkit.Mvvm.ComponentModel;

namespace VehicleServiceCenter.Models;

public partial class ServiceRecord : ObservableObject
{
    private int _id;
    public int ID
    {
        get => _id;
        set => SetProperty(ref _id, value);
    }

    [ObservableProperty]
    private string _receiptNumber = string.Empty;

    [ObservableProperty]
    private DateTime _date;

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
}