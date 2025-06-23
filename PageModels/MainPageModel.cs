using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VehicleServiceCenter.Models;
using VehicleServiceCenter.Data;
using VehicleServiceCenter.Services;

namespace VehicleServiceCenter.PageModels
{    public partial class MainPageModel : ObservableObject
    {
        private bool _isNavigatedTo;
        private readonly ServiceRepository _serviceRepository;
        private readonly ModalErrorHandler _errorHandler;
        private readonly IPreferences _preferences;
       

        [ObservableProperty]
        private string _searchQuery = string.Empty;

        [ObservableProperty]
        private List<ServiceRecord> _serviceRecords = [];

        [ObservableProperty]
        private List<ServiceRecord> _filteredServiceRecords = [];

        [ObservableProperty]
        bool _isBusy;

        [ObservableProperty]
        bool _isRefreshing;

        [ObservableProperty]
        private string _today = DateTime.Now.ToString("dddd, MMM d");        public MainPageModel(
            ServiceRepository serviceRepository, 
            ModalErrorHandler errorHandler,
            IPreferences preferences)
        {
            _serviceRepository = serviceRepository;
            _errorHandler = errorHandler;
            _preferences = preferences;
        }

        partial void OnSearchQueryChanged(string value)
        {
            FilterServiceRecords();
        }

        private void FilterServiceRecords()
        {
            if (string.IsNullOrWhiteSpace(SearchQuery))
            {
                FilteredServiceRecords = ServiceRecords.ToList();
                return;
            }

            var query = SearchQuery.Trim().ToLower();
            FilteredServiceRecords = ServiceRecords.Where(r =>
                r.ReceiptNumber?.ToLower().Contains(query) == true ||
                r.VehicleNumber?.ToLower().Contains(query) == true ||
                r.MobileNumber?.ToLower().Contains(query) == true ||
                r.ReceiptDate.ToString("dd/MM/yyyy").Contains(query)
            ).ToList();
        }        private async Task LoadData()
        {
            try
            {
                IsBusy = true;
                
                // Get the default record limit from settings (for main page, use a smaller limit)
                var defaultLimit = _preferences.Get("DefaultRecordLimit", 20);
                var mainPageLimit = Math.Min(defaultLimit > 0 ? defaultLimit : 20, 10); // Max 10 for main page
                
                ServiceRecords = await _serviceRepository.ListAsync(mainPageLimit);
                FilterServiceRecords();
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task Refresh()
        {
            try
            {
                IsRefreshing = true;
                await LoadData();
            }
            catch (Exception e)
            {
                _errorHandler.HandleError(e);
            }
            finally
            {
                IsRefreshing = false;
            }
        }

        [RelayCommand]
        private void NavigatedTo() =>
            _isNavigatedTo = true;

        [RelayCommand]
        private void NavigatedFrom() =>
            _isNavigatedTo = false;

        [RelayCommand]
        private async Task Appearing()
        {
            await LoadData();
        }
    }
}