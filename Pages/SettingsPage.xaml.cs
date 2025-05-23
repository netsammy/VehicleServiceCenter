using VehicleServiceCenter.PageModels;

namespace VehicleServiceCenter.Pages;

public partial class SettingsPage : ContentPage
{
    private readonly SettingsPageModel _viewModel;

    public SettingsPage(SettingsPageModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        BindingContext = _viewModel;
    }

    private void OnThemeSelectionChanged(object sender, EventArgs e)
    {
        if (sender is Picker picker && _viewModel != null)
        {
            string selectedTheme = picker.SelectedItem?.ToString() ?? "Light";
            _viewModel.UpdateTheme(selectedTheme);
        }
    }
}