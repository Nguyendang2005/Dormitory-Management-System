using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using DormCare.Business.DTOs;
using DormCare.Business.Services;
using DormCare.WPF.Commands;
using DormCare.WPF.Services;

namespace DormCare.WPF.ViewModels
{
    public class BedViewModel : BaseViewModel
    {
        private readonly BedService _bedService;
        private readonly DialogService _dialogService;
        private readonly int _roomId;
        private readonly SemaphoreSlim _semaphore = new(1, 1);

        private bool _isInitializing = false;

        private ObservableCollection<BedDto> _beds = new();
        public ObservableCollection<BedDto> Beds
        {
            get => _beds;
            set => SetProperty(ref _beds, value);
        }

        private BedDto? _selectedBed;
        public BedDto? SelectedBed
        {
            get => _selectedBed;
            set => SetProperty(ref _selectedBed, value);
        }

        // View Mode Toggle (Card Grid vs DataGrid Table)
        private bool _isCardView = true;
        public bool IsCardView
        {
            get => _isCardView;
            set
            {
                if (SetProperty(ref _isCardView, value))
                {
                    OnPropertyChanged(nameof(IsTableView));
                }
            }
        }
        public bool IsTableView => !_isCardView;

        // Real Database Metric Properties
        private int _totalBedsCount;
        public int TotalBedsCount
        {
            get => _totalBedsCount;
            set => SetProperty(ref _totalBedsCount, value);
        }

        private int _availableBedsCount;
        public int AvailableBedsCount
        {
            get => _availableBedsCount;
            set => SetProperty(ref _availableBedsCount, value);
        }

        private int _occupiedBedsCount;
        public int OccupiedBedsCount
        {
            get => _occupiedBedsCount;
            set => SetProperty(ref _occupiedBedsCount, value);
        }

        private int _maintenanceBedsCount;
        public int MaintenanceBedsCount
        {
            get => _maintenanceBedsCount;
            set => SetProperty(ref _maintenanceBedsCount, value);
        }

        // Smooth Filters
        public ObservableCollection<string> StatusFilterOptions { get; } = new()
        {
            "Tất cả trạng thái",
            "Available",
            "Occupied",
            "Maintenance"
        };

        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    if (!_isInitializing) _ = LoadFilteredBedsAsync();
                }
            }
        }

        private string _selectedStatusFilter = "Tất cả trạng thái";
        public string SelectedStatusFilter
        {
            get => _selectedStatusFilter;
            set
            {
                if (SetProperty(ref _selectedStatusFilter, value))
                {
                    if (!_isInitializing) _ = LoadFilteredBedsAsync();
                }
            }
        }

        public ICommand RefreshCommand { get; }
        public ICommand ToggleViewModeCommand { get; }
        public ICommand SetAvailableCommand { get; }
        public ICommand SetOccupiedCommand { get; }
        public ICommand SetMaintenanceCommand { get; }

        public BedViewModel(BedService bedService, DialogService dialogService, int roomId = 0)
        {
            Title = "Quản Lý Giường Ký Túc Xá";
            _bedService = bedService;
            _dialogService = dialogService;
            _roomId = roomId;

            RefreshCommand = new AsyncRelayCommand(LoadBedsAsync);
            ToggleViewModeCommand = new RelayCommand(_ => IsCardView = !IsCardView);

            SetAvailableCommand = new AsyncRelayCommand(param => ExecuteChangeStatusAsync(param, "Available"));
            SetOccupiedCommand = new AsyncRelayCommand(param => ExecuteChangeStatusAsync(param, "Occupied"));
            SetMaintenanceCommand = new AsyncRelayCommand(param => ExecuteChangeStatusAsync(param, "Maintenance"));

            _bedService.BedUpdated += async (s, e) => await LoadBedsAsync();

            _ = InitializeDataAsync();
        }

        private async Task InitializeDataAsync()
        {
            try
            {
                _isInitializing = true;
                await LoadBedsAsync();
            }
            finally
            {
                _isInitializing = false;
            }
        }

        public async Task LoadBedsAsync()
        {
            if (!await _semaphore.WaitAsync(0)) return;
            try
            {
                IsBusy = true;

                var stats = await _bedService.GetBedStatsAsync();
                TotalBedsCount = stats.TotalBedsCount;
                AvailableBedsCount = stats.AvailableBedsCount;
                OccupiedBedsCount = stats.OccupiedBedsCount;
                MaintenanceBedsCount = stats.MaintenanceBedsCount;

                var filtered = await _bedService.SearchAndFilterBedsAsync(SelectedStatusFilter, SearchText);
                Beds = new ObservableCollection<BedDto>(filtered);
            }
            catch (Exception ex)
            {
                _dialogService.ShowError($"Không thể tải dữ liệu giường từ Database:\n{ex.Message}", "Lỗi Cơ Sở Dữ Liệu");
            }
            finally
            {
                IsBusy = false;
                _semaphore.Release();
            }
        }

        private async Task LoadFilteredBedsAsync()
        {
            if (!await _semaphore.WaitAsync(0)) return;
            try
            {
                var filtered = await _bedService.SearchAndFilterBedsAsync(SelectedStatusFilter, SearchText);
                Beds = new ObservableCollection<BedDto>(filtered);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error filtering beds: {ex.Message}");
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private async Task ExecuteChangeStatusAsync(object? parameter, string newStatus)
        {
            var target = parameter as BedDto ?? SelectedBed;
            if (target == null) return;

            IsBusy = true;
            var result = await _bedService.UpdateBedStatusAsync(target.BedId, newStatus);
            IsBusy = false;

            if (result.IsSuccess)
            {
                _dialogService.ShowInformation(result.Message, "Thành Công");
                await LoadBedsAsync();
            }
            else
            {
                _dialogService.ShowError(result.Message, "Thất Bại");
            }
        }
    }
}
