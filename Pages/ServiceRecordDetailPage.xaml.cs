namespace VehicleServiceCenter.Pages;

public partial class ServiceRecordDetailPage : ContentPage
{
    public ServiceRecordDetailPage(PageModels.ServiceRecordDetailPageModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}