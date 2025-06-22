using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VehicleServiceCenter.Models;
using VehicleServiceCenter.Data;
using VehicleServiceCenter.Services;

namespace VehicleServiceCenter.PageModels
{
    public partial class MainPageModel : ObservableObject
    {
        private bool _isNavigatedTo;
        private readonly ServiceRepository _serviceRepository;
        private readonly ModalErrorHandler _errorHandler;
       

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
        private string _today = DateTime.Now.ToString("dddd, MMM d");

        public MainPageModel(
            ServiceRepository serviceRepository, 
            ModalErrorHandler errorHandler)
        {
            _serviceRepository = serviceRepository;
            _errorHandler = errorHandler;
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
        }

        private async Task LoadData()
        {
            try
            {
                IsBusy = true;
                ServiceRecords = await _serviceRepository.ListAsync();
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