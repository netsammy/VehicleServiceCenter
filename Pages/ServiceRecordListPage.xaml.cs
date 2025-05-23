namespace VehicleServiceCenter.Pages;

public partial class ServiceRecordListPage : ContentPage
{
    public ServiceRecordListPage(PageModels.ServiceRecordListPageModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}