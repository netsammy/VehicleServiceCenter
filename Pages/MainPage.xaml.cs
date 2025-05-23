using VehicleServiceCenter.Models;
using VehicleServiceCenter.PageModels;

namespace VehicleServiceCenter.Pages
{
    public partial class MainPage : ContentPage
    {
        public MainPage(MainPageModel model)
        {
            InitializeComponent();
            BindingContext = model;
        }
    }
}