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
    public class RoomViewModel : BaseViewModel
    {
        private readonly RoomService _roomService;
        private readonly BuildingService _buildingService;
        private readonly ApplicationService? _applicationService;
        private readonly DialogService _dialogService;
        private readonly User? _currentUser;

        private bool _isInitializing = false;

        private ObservableCollection<RoomDto> _rooms = new();
        public ObservableCollection<RoomDto> Rooms
        {
            get => _rooms;
            set => SetProperty(ref _rooms, value);
        }

        private RoomDto? _selectedRoom;
        public RoomDto? SelectedRoom
        {
            get => _selectedRoom;
            set => SetProperty(ref _selectedRoom, value);
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
        public bool IsTableView => !IsCardView;

        // Real Database Metric Cards Properties
        private int _totalRoomsCount;
        public int TotalRoomsCount
        {
            get => _totalRoomsCount;
            set => SetProperty(ref _totalRoomsCount, value);
        }

        private int _totalBedsCount;
        public int TotalBedsCount
        {
            get => _totalBedsCount;
            set => SetProperty(ref _totalBedsCount, value);
        }

        private int _availableRoomsCount;
        public int AvailableRoomsCount
        {
            get => _availableRoomsCount;
            set => SetProperty(ref _availableRoomsCount, value);
        }

        // Dynamic Filters
        public ObservableCollection<string> BuildingFilterOptions { get; } = new() { "Tất cả tòa" };
        public ObservableCollection<string> TypeFilterOptions { get; } = new() { "Tất cả loại", "Standard", "Premium", "Accessible" };

        private string _searchKeyword = string.Empty;
        public string SearchKeyword
        {
            get => _searchKeyword;
            set
            {
                if (SetProperty(ref _searchKeyword, value))
                {
                    if (!_isInitializing) _ = LoadFilteredRoomsAsync();
                }
            }
        }

        private string _selectedBuildingFilter = "Tất cả tòa";
        public string SelectedBuildingFilter
        {
            get => _selectedBuildingFilter;
            set
            {
                if (SetProperty(ref _selectedBuildingFilter, value))
                {
                    if (!_isInitializing) _ = LoadFilteredRoomsAsync();
                }
            }
        }

        private string _selectedTypeFilter = "Tất cả loại";
        public string SelectedTypeFilter
        {
            get => _selectedTypeFilter;
            set
            {
                if (SetProperty(ref _selectedTypeFilter, value))
                {
                    if (!_isInitializing) _ = LoadFilteredRoomsAsync();
                }
            }
        }

        public ICommand RefreshCommand { get; }
        public ICommand ToggleViewModeCommand { get; }
        public ICommand AddRoomCommand { get; }
        public ICommand EditRoomCommand { get; }
        public ICommand DeleteRoomCommand { get; }
        public ICommand ViewDetailCommand { get; }
        public ICommand ApplyRoomCommand { get; }

        public RoomViewModel(RoomService roomService, BuildingService buildingService, DialogService dialogService, ApplicationService? applicationService = null, User? currentUser = null)
        {
            Title = "Quản Lý Phòng Ở KTX";
            _roomService = roomService;
            _buildingService = buildingService;
            _dialogService = dialogService;
            _applicationService = applicationService;
            _currentUser = currentUser;

            RefreshCommand = new AsyncRelayCommand(LoadRoomsAsync);
            ToggleViewModeCommand = new RelayCommand(_ => IsCardView = !IsCardView);
            AddRoomCommand = new AsyncRelayCommand(ExecuteAddRoomAsync);
            EditRoomCommand = new AsyncRelayCommand(ExecuteEditRoomAsync);
            DeleteRoomCommand = new AsyncRelayCommand(ExecuteDeleteRoomAsync);
            ViewDetailCommand = new AsyncRelayCommand(ExecuteViewDetailAsync);
            ApplyRoomCommand = new AsyncRelayCommand(ExecuteApplyRoomAsync, () => SelectedRoom != null);

            _roomService.RoomUpdated += (s, e) =>
            {
                System.Windows.Application.Current?.Dispatcher.InvokeAsync(async () => await LoadRoomsAsync());
            };

            _ = InitializeDataAsync();
        }

        private async Task InitializeDataAsync()
        {
            try
            {
                _isInitializing = true;
                await LoadBuildingFilterOptionsAsync();
                await LoadRoomsAsync();
            }
            finally
            {
                _isInitializing = false;
            }
        }

        public async Task LoadBuildingFilterOptionsAsync()
        {
            try
            {
                var buildings = await _buildingService.GetAllBuildingsAsync();
                BuildingFilterOptions.Clear();
                BuildingFilterOptions.Add("Tất cả tòa");

                foreach (var b in buildings)
                {
                    if (!string.IsNullOrWhiteSpace(b.BuildingName))
                    {
                        BuildingFilterOptions.Add(b.BuildingName);
                    }
                }
                SelectedBuildingFilter = "Tất cả tòa";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading building filter options: {ex.Message}");
            }
        }

        private readonly System.Threading.SemaphoreSlim _semaphore = new(1, 1);

        public async Task LoadRoomsAsync()
        {
            await _semaphore.WaitAsync();
            try
            {
                IsBusy = true;

                // Load real DB metric stats
                var stats = await _roomService.GetOccupancyStatsAsync();
                TotalRoomsCount = stats.TotalRoomsCount;
                TotalBedsCount = stats.TotalBedsCount;
                AvailableRoomsCount = stats.AvailableRoomsCount;

                int? buildingId = null;
                if (SelectedBuildingFilter != "Tất cả tòa")
                {
                    var buildings = await _buildingService.GetAllBuildingsAsync();
                    var found = buildings.FirstOrDefault(b => b.BuildingName.Equals(SelectedBuildingFilter, StringComparison.OrdinalIgnoreCase));
                    if (found != null)
                    {
                        buildingId = found.BuildingId;
                    }
                }

                var filtered = await _roomService.SearchAndFilterRoomsAsync(
                    buildingId: buildingId,
                    genderType: null,
                    roomType: SelectedTypeFilter,
                    searchText: SearchKeyword);

                Rooms = new ObservableCollection<RoomDto>(filtered);
            }
            catch (Exception ex)
            {
                _dialogService.ShowError($"Không thể tải dữ liệu phòng từ Database:\n{ex.Message}", "Lỗi Cơ Sở Dữ Liệu");
            }
            finally
            {
                IsBusy = false;
                _semaphore.Release();
            }
        }

        private async Task LoadFilteredRoomsAsync()
        {
            await _semaphore.WaitAsync();
            try
            {
                int? buildingId = null;
                if (SelectedBuildingFilter != "Tất cả tòa")
                {
                    var buildings = await _buildingService.GetAllBuildingsAsync();
                    var found = buildings.FirstOrDefault(b => b.BuildingName.Equals(SelectedBuildingFilter, StringComparison.OrdinalIgnoreCase));
                    if (found != null)
                    {
                        buildingId = found.BuildingId;
                    }
                }

                var filtered = await _roomService.SearchAndFilterRoomsAsync(
                    buildingId: buildingId,
                    genderType: null,
                    roomType: SelectedTypeFilter,
                    searchText: SearchKeyword);

                Rooms = new ObservableCollection<RoomDto>(filtered);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error filtering rooms: {ex.Message}");
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private async Task ExecuteViewDetailAsync(object? parameter)
        {
            var target = parameter as RoomDto ?? SelectedRoom;
            if (target == null) return;

            IsBusy = true;
            var roomDetail = await _roomService.GetRoomDetailAsync(target.RoomId);
            IsBusy = false;

            if (roomDetail == null)
            {
                _dialogService.ShowError("Không thể tải chi tiết phòng.", "Lỗi");
                return;
            }

            var vm = new RoomDetailViewModel(_roomService, roomDetail);
            var win = new RoomDetailWindow
            {
                DataContext = vm,
                Owner = Application.Current.MainWindow
            };
            win.ShowDialog();
        }

        private async Task ExecuteAddRoomAsync()
        {
            var vm = new RoomFormViewModel(_roomService, _buildingService);
            var win = new RoomWindow
            {
                DataContext = vm,
                Owner = Application.Current.MainWindow
            };

            vm.CloseAction = (result) =>
            {
                win.DialogResult = result;
                win.Close();
            };

            if (win.ShowDialog() == true)
            {
                _dialogService.ShowInformation("Thêm phòng thành công!", "Thành Công");
                await LoadRoomsAsync();
            }
        }

        private async Task ExecuteEditRoomAsync(object? parameter)
        {
            var target = parameter as RoomDto ?? SelectedRoom;
            if (target == null)
            {
                _dialogService.ShowInformation("Vui lòng chọn phòng để chỉnh sửa.", "Thông Báo");
                return;
            }

            var vm = new RoomFormViewModel(_roomService, _buildingService, target);
            var win = new RoomWindow
            {
                DataContext = vm,
                Owner = Application.Current.MainWindow
            };

            vm.CloseAction = (result) =>
            {
                win.DialogResult = result;
                win.Close();
            };

            if (win.ShowDialog() == true)
            {
                _dialogService.ShowInformation($"Cập nhật thông tin phòng '{target.RoomNumber}' thành công!", "Thành Công");
                await LoadRoomsAsync();
            }
        }

        private async Task ExecuteDeleteRoomAsync(object? parameter)
        {
            var target = parameter as RoomDto ?? SelectedRoom;
            if (target == null)
            {
                _dialogService.ShowInformation("Vui lòng chọn phòng để xóa.", "Thông Báo");
                return;
            }

            IsBusy = true;
            var checkResult = await _roomService.CheckRoomDeleteDependencyAsync(target.RoomId);
            IsBusy = false;

            if (!checkResult.CanDelete)
            {
                // Dependency exists -> Show custom safe delete blocked dialog
                var vm = new RoomDeleteBlockedViewModel(target.RoomNumber, checkResult);
                var blockedWin = new RoomDeleteBlockedWindow
                {
                    DataContext = vm,
                    Owner = Application.Current.MainWindow
                };

                vm.CloseAction = (result) =>
                {
                    blockedWin.DialogResult = result;
                    blockedWin.Close();
                };

                if (blockedWin.ShowDialog() == true)
                {
                    // User selected: Deactivate room instead
                    IsBusy = true;
                    var deactResult = await _roomService.DeactivateRoomAsync(target.RoomId);
                    IsBusy = false;

                    if (deactResult.IsSuccess)
                    {
                        _dialogService.ShowInformation(deactResult.Message, "Thành Công");
                        await LoadRoomsAsync();
                    }
                    else
                    {
                        _dialogService.ShowError(deactResult.Message, "Thất Bại");
                    }
                }
            }
            else
            {
                // Clean delete
                bool confirm = _dialogService.ShowConfirmation($"Bạn có chắc chắn muốn xóa phòng '{target.RoomNumber}' khỏi hệ thống không?", "Xác Nhận Xóa Phòng");
                if (!confirm) return;

                IsBusy = true;
                var delResult = await _roomService.DeleteRoomAsync(target.RoomId);
                IsBusy = false;

                if (delResult.IsSuccess)
                {
                    _dialogService.ShowInformation(delResult.Message, "Thành Công");
                    await LoadRoomsAsync();
                }
                else
                {
                    _dialogService.ShowError(delResult.Message, "Thất Bại");
                }
            }
        }

        private async Task ExecuteApplyRoomAsync()
        {
            if (SelectedRoom == null) return;
            if (_currentUser == null || _currentUser.Role != "Student" || _currentUser.StudentProfile == null)
            {
                _dialogService.ShowInformation("Tính năng Đăng ký ở phòng này dành cho sinh viên.", "Thông Báo");
                return;
            }

            if (_applicationService == null) return;

            IsBusy = true;
            var result = await _applicationService.SubmitApplicationAsync(_currentUser.StudentProfile.StudentId, SelectedRoom.RoomId, null, "Đăng ký ở từ giao diện danh sách phòng");
            IsBusy = false;

            if (result.IsSuccess)
            {
                _dialogService.ShowInformation(result.Message, "Thành Công");
            }
            else
            {
                _dialogService.ShowError(result.Message, "Không Thể Đăng Ký");
            }
        }
    }
}
