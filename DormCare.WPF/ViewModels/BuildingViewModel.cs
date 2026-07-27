using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using DormCare.Business.DTOs;
using DormCare.Business.Services;
using DormCare.Domain.Entities;
using DormCare.WPF.Commands;
using DormCare.WPF.Services;
using DormCare.WPF.Views.Manager;

namespace DormCare.WPF.ViewModels
{
    public class BuildingViewModel : BaseViewModel
    {
        private readonly BuildingService _buildingService;
        private readonly DialogService _dialogService;

        private List<BuildingDto> _allBuildings = new();

        private ObservableCollection<BuildingDto> _buildings = new();
        public ObservableCollection<BuildingDto> Buildings
        {
            get => _buildings;
            set => SetProperty(ref _buildings, value);
        }

        private BuildingDto? _selectedBuilding;
        public BuildingDto? SelectedBuilding
        {
            get => _selectedBuilding;
            set => SetProperty(ref _selectedBuilding, value);
        }

        // View Mode: Card Grid View vs Table View
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

        // Summary Metric Cards
        private int _totalBuildingsCount;
        public int TotalBuildingsCount
        {
            get => _totalBuildingsCount;
            set => SetProperty(ref _totalBuildingsCount, value);
        }

        private int _totalRoomsCount;
        public int TotalRoomsCount
        {
            get => _totalRoomsCount;
            set => SetProperty(ref _totalRoomsCount, value);
        }

        private int _totalAvailableBedsCount;
        public int TotalAvailableBedsCount
        {
            get => _totalAvailableBedsCount;
            set => SetProperty(ref _totalAvailableBedsCount, value);
        }

        // Filter 1: Search Keyword
        private string _searchKeyword = string.Empty;
        public string SearchKeyword
        {
            get => _searchKeyword;
            set
            {
                if (SetProperty(ref _searchKeyword, value))
                {
                    ApplyFilters();
                }
            }
        }

        // Filter 2: Status
        public ObservableCollection<string> StatusFilterOptions { get; } = new()
        {
            "Tất cả trạng thái",
            "Active",
            "Inactive",
            "Maintenance"
        };

        private string _selectedStatusFilter = "Tất cả trạng thái";
        public string SelectedStatusFilter
        {
            get => _selectedStatusFilter;
            set
            {
                if (SetProperty(ref _selectedStatusFilter, value))
                {
                    ApplyFilters();
                }
            }
        }

        // Filter 3: Dynamic Floors
        private ObservableCollection<string> _floorFilterOptions = new() { "Tất cả số tầng" };
        public ObservableCollection<string> FloorFilterOptions
        {
            get => _floorFilterOptions;
            set => SetProperty(ref _floorFilterOptions, value);
        }

        private string _selectedFloorFilter = "Tất cả số tầng";
        public string SelectedFloorFilter
        {
            get => _selectedFloorFilter;
            set
            {
                if (SetProperty(ref _selectedFloorFilter, value))
                {
                    ApplyFilters();
                }
            }
        }

        // Filter 4: Occupancy Rate
        public ObservableCollection<string> OccupancyFilterOptions { get; } = new()
        {
            "Tất cả mức lấp đầy",
            "Dưới 50%",
            "Từ 50% đến 99%",
            "Đã đầy 100%"
        };

        private string _selectedOccupancyFilter = "Tất cả mức lấp đầy";
        public string SelectedOccupancyFilter
        {
            get => _selectedOccupancyFilter;
            set
            {
                if (SetProperty(ref _selectedOccupancyFilter, value))
                {
                    ApplyFilters();
                }
            }
        }

        public ICommand RefreshCommand { get; }
        public ICommand AddBuildingCommand { get; }
        public ICommand EditBuildingCommand { get; }
        public ICommand ViewDetailCommand { get; }
        public ICommand DeleteBuildingCommand { get; }
        public ICommand ToggleViewModeCommand { get; }

        public BuildingViewModel(BuildingService buildingService, DialogService dialogService)
        {
            Title = "Quản lý tòa nhà";
            _buildingService = buildingService;
            _dialogService = dialogService;

            RefreshCommand = new AsyncRelayCommand(LoadBuildingsAsync);
            AddBuildingCommand = new AsyncRelayCommand(ExecuteAddBuildingAsync);
            EditBuildingCommand = new AsyncRelayCommand(ExecuteEditBuildingAsync);
            ViewDetailCommand = new AsyncRelayCommand(ExecuteViewDetailAsync);
            DeleteBuildingCommand = new AsyncRelayCommand(ExecuteDeleteBuildingAsync);
            ToggleViewModeCommand = new RelayCommand(_ => IsCardView = !IsCardView);

            _ = LoadBuildingsAsync();
        }

        private bool _isInitializing = false;

        public async Task LoadBuildingsAsync()
        {
            if (_isInitializing) return;
            try
            {
                _isInitializing = true;
                IsBusy = true;
                var dtos = await _buildingService.GetAllBuildingsAsync();
                _allBuildings = dtos.ToList();

                // Populate Dynamic Floor Options using LINQ
                var floorOptions = _allBuildings
                    .Select(b => b.NumberOfFloors)
                    .Distinct()
                    .OrderBy(f => f)
                    .Select(f => $"{f} tầng")
                    .ToList();

                floorOptions.Insert(0, "Tất cả số tầng");
                FloorFilterOptions = new ObservableCollection<string>(floorOptions);

                if (!FloorFilterOptions.Contains(SelectedFloorFilter))
                {
                    _selectedFloorFilter = "Tất cả số tầng";
                    OnPropertyChanged(nameof(SelectedFloorFilter));
                }

                ApplyFilters();
            }
            catch (Exception ex)
            {
                _dialogService.ShowError($"Không thể tải dữ liệu tòa nhà từ Database:\n{ex.Message}\n\nVui lòng kiểm tra lại dịch vụ SQL Server và CSDL 'DormCareDB'.", "Lỗi Kết Nối Cơ Sở Dữ Liệu");
            }
            finally
            {
                IsBusy = false;
                _isInitializing = false;
            }
        }

        private void ApplyFilters()
        {
            var query = _allBuildings.AsEnumerable();

            // 1. Search Keyword (BuildingCode, BuildingName, Address)
            if (!string.IsNullOrWhiteSpace(SearchKeyword))
            {
                string kw = SearchKeyword.Trim();
                query = query.Where(b => b.BuildingCode.Contains(kw, StringComparison.OrdinalIgnoreCase) ||
                                         b.BuildingName.Contains(kw, StringComparison.OrdinalIgnoreCase) ||
                                         b.Address.Contains(kw, StringComparison.OrdinalIgnoreCase));
            }

            // 2. Status Filter
            if (!string.IsNullOrWhiteSpace(SelectedStatusFilter) &&
                SelectedStatusFilter != "Tất cả trạng thái" &&
                SelectedStatusFilter != "All")
            {
                query = query.Where(b => b.Status.Equals(SelectedStatusFilter, StringComparison.OrdinalIgnoreCase));
            }

            // 3. Dynamic Floor Filter
            if (!string.IsNullOrWhiteSpace(SelectedFloorFilter) &&
                SelectedFloorFilter != "Tất cả số tầng")
            {
                string floorStr = SelectedFloorFilter.Replace("tầng", "").Trim();
                if (int.TryParse(floorStr, out int floors))
                {
                    query = query.Where(b => b.NumberOfFloors == floors);
                }
            }

            // 4. Occupancy Rate Filter
            if (!string.IsNullOrWhiteSpace(SelectedOccupancyFilter) &&
                SelectedOccupancyFilter != "Tất cả mức lấp đầy")
            {
                if (SelectedOccupancyFilter == "Còn nhiều chỗ trống (< 50%)" || SelectedOccupancyFilter == "Dưới 50%")
                {
                    query = query.Where(b => b.OccupancyRate < 50.0);
                }
                else if (SelectedOccupancyFilter == "Đang sử dụng (50% - 99%)" || SelectedOccupancyFilter == "Từ 50% đến 99%")
                {
                    query = query.Where(b => b.OccupancyRate >= 50.0 && b.OccupancyRate < 100.0);
                }
                else if (SelectedOccupancyFilter == "Đã đầy (100%)" || SelectedOccupancyFilter == "Đã đầy 100%")
                {
                    query = query.Where(b => b.OccupancyRate >= 100.0);
                }
            }

            var filteredList = query.ToList();
            Buildings = new ObservableCollection<BuildingDto>(filteredList);

            // Update Summary Metric Cards
            TotalBuildingsCount = filteredList.Count;
            TotalRoomsCount = filteredList.Sum(b => b.TotalRooms);
            TotalAvailableBedsCount = filteredList.Sum(b => b.AvailableBeds);
        }

        private async Task ExecuteAddBuildingAsync()
        {
            var formVm = new BuildingFormViewModel(_buildingService);
            var window = new BuildingWindow
            {
                DataContext = formVm,
                Owner = Application.Current.MainWindow
            };

            formVm.CloseAction = result =>
            {
                window.DialogResult = result;
                window.Close();
            };

            if (window.ShowDialog() == true)
            {
                _dialogService.ShowInformation("✓ Tòa nhà đã được thêm thành công vào cơ sở dữ liệu SQL Server.", "Thành công");
                await LoadBuildingsAsync();
            }
        }

        private async Task ExecuteEditBuildingAsync(object? parameter)
        {
            var target = parameter as BuildingDto ?? SelectedBuilding;
            if (target == null)
            {
                _dialogService.ShowInformation("Vui lòng chọn một tòa nhà để chỉnh sửa.", "Thông báo");
                return;
            }

            var buildingEntity = new Building
            {
                BuildingId = target.BuildingId,
                BuildingCode = target.BuildingCode,
                BuildingName = target.BuildingName,
                Address = target.Address,
                NumberOfFloors = target.NumberOfFloors,
                Description = target.Description,
                Status = target.Status
            };

            var formVm = new BuildingFormViewModel(_buildingService, buildingEntity);
            var window = new BuildingWindow
            {
                DataContext = formVm,
                Owner = Application.Current.MainWindow
            };

            formVm.CloseAction = result =>
            {
                window.DialogResult = result;
                window.Close();
            };

            if (window.ShowDialog() == true)
            {
                _dialogService.ShowInformation("✓ Cập nhật tòa nhà trong SQL Server thành công.", "Thành công");
                await LoadBuildingsAsync();
            }
        }

        private async Task ExecuteViewDetailAsync(object? parameter)
        {
            var target = parameter as BuildingDto ?? SelectedBuilding;
            if (target == null)
            {
                _dialogService.ShowInformation("Vui lòng chọn một tòa nhà để xem chi tiết.", "Thông báo");
                return;
            }

            IsBusy = true;
            var detailDto = await _buildingService.GetBuildingDetailAsync(target.BuildingId);
            IsBusy = false;

            if (detailDto == null)
            {
                _dialogService.ShowError("Không thể tải thông tin chi tiết tòa nhà.", "Lỗi");
                return;
            }

            var detailWindow = new BuildingDetailWindow
            {
                DataContext = new { Building = detailDto },
                Owner = Application.Current.MainWindow
            };

            detailWindow.ShowDialog();
        }

        private async Task ExecuteDeleteBuildingAsync(object? parameter)
        {
            var target = parameter as BuildingDto ?? SelectedBuilding;
            if (target == null)
            {
                _dialogService.ShowInformation("Vui lòng chọn một tòa nhà để xóa.", "Thông báo");
                return;
            }

            bool confirm = _dialogService.ShowConfirmation($"Bạn có chắc chắn muốn xóa tòa nhà '{target.BuildingName}' không?", "Xác nhận xóa");
            if (!confirm) return;

            IsBusy = true;
            var result = await _buildingService.DeleteBuildingAsync(target.BuildingId);
            IsBusy = false;

            if (!result.IsSuccess)
            {
                _dialogService.ShowError(result.Message, "Lỗi");
                return;
            }

            var deleteInfo = result.Data;
            if (deleteInfo != null && !deleteInfo.CanDelete)
            {
                // Open safe deletion block window
                var blockedWindow = new BuildingDeleteBlockedWindow
                {
                    DataContext = new
                    {
                        BuildingName = target.BuildingName.Replace(" (Đã cập nhật)", "").Trim(),
                        DeleteResult = deleteInfo
                    },
                    Owner = Application.Current.MainWindow
                };

                if (blockedWindow.ShowDialog() == true)
                {
                    // User chose to deactivate building
                    IsBusy = true;
                    var deactivateResult = await _buildingService.DeactivateBuildingAsync(target.BuildingId);
                    IsBusy = false;

                    if (deactivateResult.IsSuccess)
                    {
                        _dialogService.ShowInformation(deactivateResult.Message, "Thành công");
                        await LoadBuildingsAsync();
                    }
                    else
                    {
                        _dialogService.ShowError(deactivateResult.Message, "Thất bại");
                    }
                }
            }
            else
            {
                _dialogService.ShowInformation(result.Message, "Thành công");
                await LoadBuildingsAsync();
            }
        }
    }
}
