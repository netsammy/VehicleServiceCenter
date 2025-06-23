using VehicleServiceCenter.Models;
using VehicleServiceCenter.PageModels;
using CommunityToolkit.Mvvm.Input;

namespace VehicleServiceCenter.Pages;

public partial class ServiceRecordDetailPage : ContentPage
{
    public ServiceRecordDetailPage(ServiceRecordDetailPageModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }    private async void OnDeleteButtonClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is ServiceItem item)
        {
            var viewModel = BindingContext as ServiceRecordDetailPageModel;
            if (viewModel?.RemoveItemCommand != null)
            {
                System.Diagnostics.Debug.WriteLine($"Button clicked for item: {item.Name}");
                if (viewModel.RemoveItemCommand.CanExecute(item))
                {
                    if (viewModel.RemoveItemCommand is IAsyncRelayCommand<ServiceItem> asyncCommand)
                    {
                        await asyncCommand.ExecuteAsync(item);
                    }
                    else
                    {
                        viewModel.RemoveItemCommand.Execute(item);
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Command cannot execute");
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Command or ViewModel is null");
            }
        }
        else
        {
            System.Diagnostics.Debug.WriteLine("Button or CommandParameter is invalid");
        }
    }
}