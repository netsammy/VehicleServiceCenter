using CommunityToolkit.Mvvm.ComponentModel;

namespace VehicleServiceCenter.Models;

public partial class ServiceItem : ObservableObject
{
    private int _id;
    public int ID
    {
        get => _id;
        set => SetProperty(ref _id, value);
    }

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _grade = string.Empty;

    private double _quantity;
    public double Quantity
    {
        get => _quantity;
        set
        {
            if (SetProperty(ref _quantity, value))
            {
                UpdateAmount();
            }
        }
    }    private double _rate;
    public double Rate
    {
        get => _rate;
        set
        {
            var oldValue = _rate;
            if (SetProperty(ref _rate, value))
            {
                try 
                {
                    UpdateAmount();
                }
                catch
                {
                    // If calculation fails, revert the value
                    _rate = oldValue;
                    OnPropertyChanged(nameof(Rate));
                }
            }
        }
    }

    [ObservableProperty]
    private double _amount;

    private int _serviceRecordId;
    public int ServiceRecordId
    {
        get => _serviceRecordId;
        set => SetProperty(ref _serviceRecordId, value);
    }    private void UpdateAmount()
    {
        try
        {
            Amount = Math.Round(Quantity * Rate, 2); // Round to 2 decimal places
        }
        catch
        {
            Amount = 0; // Handle any calculation errors
        }
    }
}