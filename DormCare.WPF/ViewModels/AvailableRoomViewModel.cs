using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using DormCare.Business.DTOs;
using DormCare.Business.Services;
using DormCare.WPF.Commands;

namespace DormCare.WPF.ViewModels
{
    public class AvailableRoomViewModel : BaseViewModel
    {
        private readonly RoomService _roomService;
        private readonly BuildingService _buildingService;
        private readonly Func<int, Task>? _registerRoomAsync;

        private bool _isInitializing = false;

        // View Mode Toggle (Card View vs DataGrid Table View)
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

        private ObservableCollection<RoomAvailabilityDto> _availableRooms = new();
        public ObservableCollection<RoomAvailabilityDto> AvailableRooms
        {
            get => _availableRooms;
            set => SetProperty(ref _availableRooms, value);
        }

        // Real Calculated Filter Summary Stats
        private int _matchedRoomsCount;
        public int MatchedRoomsCount
        {
            get => _matchedRoomsCount;
            set => SetProperty(ref _matchedRoomsCount, value);
        }

        private int _totalAvailableBedsCount;
        public int TotalAvailableBedsCount
        {
            get => _totalAvailableBedsCount;
            set => SetProperty(ref _totalAvailableBedsCount, value);
        }

        private decimal _averageRent;
        public decimal AverageRent
        {
            get => _averageRent;
            set => SetProperty(ref _averageRent, value);
        }

        // Dynamic Filter Dropdowns
        public ObservableCollection<string> BuildingFilterOptions { get; } = new() { "Tất cả tòa" };
        public List<string> RoomTypeOptions { get; } = new() { "Tất cả loại", "Standard", "Premium", "VIP" };
        public List<string> GenderTypeOptions { get; } = new() { "Tất cả giới tính", "Male", "Female", "Mixed" };

        private string _searchKeyword = string.Empty;
        public string SearchKeyword
        {
            get => _searchKeyword;
            set
            {
                if (SetProperty(ref _searchKeyword, value))
                {
                    if (!_isInitializing) ApplyFilters();
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
                    if (!_isInitializing) ApplyFilters();
                }
            }
        }

        private string _selectedRoomType = "Tất cả loại";
        public string SelectedRoomType
        {
            get => _selectedRoomType;
            set
            {
                if (SetProperty(ref _selectedRoomType, value))
                {
                    if (!_isInitializing) ApplyFilters();
                }
            }
        }

        private string _selectedGenderType = "Tất cả giới tính";
        public string SelectedGenderType
        {
            get => _selectedGenderType;
            set
            {
                if (SetProperty(ref _selectedGenderType, value))
                {
                    if (!_isInitializing) ApplyFilters();
                }
            }
        }

        private string _minAvailableBedsText = "1";
        public string MinAvailableBedsText
        {
            get => _minAvailableBedsText;
            set
            {
                if (SetProperty(ref _minAvailableBedsText, value))
                {
                    if (!_isInitializing) ApplyFilters();
                }
            }
        }

        private string _maxMonthlyRentText = "5000000";
        public string MaxMonthlyRentText
        {
            get => _maxMonthlyRentText;
            set
            {
                if (SetProperty(ref _maxMonthlyRentText, value))
                {
                    if (!_isInitializing) ApplyFilters();
                }
            }
        }

        private string _loadingStateMessage = "Đang tải dữ liệu...";
        public string LoadingStateMessage
        {
            get => _loadingStateMessage;
            set => SetProperty(ref _loadingStateMessage, value);
        }

        private bool _isEmptyState;
        public bool IsEmptyState
        {
            get => _isEmptyState;
            set => SetProperty(ref _isEmptyState, value);
        }

        public bool CanRegisterRooms => _registerRoomAsync != null;

        public ICommand ResetFiltersCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand ToggleViewModeCommand { get; }
        public ICommand RegisterRoomCommand { get; }

        private List<RoomAvailabilityDto> _allRoomsCache = new();

        public AvailableRoomViewModel(RoomService roomService, BuildingService buildingService, Func<int, Task>? registerRoomAsync = null)
        {
            Title = "Tìm Phòng Còn Giường Trống";
            _roomService = roomService;
            _buildingService = buildingService;
            _registerRoomAsync = registerRoomAsync;

            ResetFiltersCommand = new RelayCommand(_ => ResetFilters());
            RefreshCommand = new AsyncRelayCommand(LoadAvailableRoomsAsync);
            ToggleViewModeCommand = new RelayCommand(_ => IsCardView = !IsCardView);
            RegisterRoomCommand = new AsyncRelayCommand(RegisterRoomAsync, CanRegisterRoom);

            _ = InitializeAsync();
        }

        private bool CanRegisterRoom(object? parameter)
        {
            return !IsBusy && _registerRoomAsync != null && TryGetRoomId(parameter, out _);
        }

        private async Task RegisterRoomAsync(object? parameter)
        {
            if (_registerRoomAsync == null || !TryGetRoomId(parameter, out var roomId))
            {
                return;
            }

            await _registerRoomAsync(roomId);
        }

        private static bool TryGetRoomId(object? parameter, out int roomId)
        {
            switch (parameter)
            {
                case int id:
                    roomId = id;
                    return id > 0;
                case RoomAvailabilityDto room:
                    roomId = room.RoomId;
                    return room.RoomId > 0;
                default:
                    roomId = 0;
                    return false;
            }
        }

        private async Task InitializeAsync()
        {
            try
            {
                _isInitializing = true;
                await LoadBuildingFilterOptionsAsync();
                await LoadAvailableRoomsAsync();
            }
            finally
            {
                _isInitializing = false;
            }
        }

        private async Task LoadBuildingFilterOptionsAsync()
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
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading building filters: {ex.Message}");
            }
        }

        public async Task LoadAvailableRoomsAsync()
        {
            try
            {
                IsBusy = true;
                LoadingStateMessage = "Đang tìm kiếm phòng còn chỗ từ Database...";
                IsEmptyState = false;

                var dtos = await _roomService.GetAvailableRoomsAsync();
                _allRoomsCache = dtos.Select(r => new RoomAvailabilityDto
                {
                    RoomId = r.RoomId,
                    BuildingId = r.BuildingId,
                    BuildingName = r.BuildingName,
                    RoomNumber = r.RoomNumber,
                    FloorNumber = r.FloorNumber,
                    RoomType = r.RoomType,
                    Capacity = r.Capacity,
                    MonthlyRent = r.MonthlyRent,
                    GenderType = r.GenderType,
                    Status = r.Status,
                    OccupiedBeds = r.OccupiedBeds,
                    AvailableBeds = r.AvailableBeds
                }).ToList();

                ApplyFilters();
            }
            catch (Exception ex)
            {
                LoadingStateMessage = $"Lỗi kết nối cơ sở dữ liệu: {ex.Message}";
                IsEmptyState = true;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void ApplyFilters()
        {
            var query = _allRoomsCache.AsEnumerable();

            // Search Keyword Filter
            if (!string.IsNullOrWhiteSpace(SearchKeyword))
            {
                string kw = SearchKeyword.Trim();
                query = query.Where(r =>
                    r.RoomNumber.Contains(kw, StringComparison.OrdinalIgnoreCase) ||
                    r.BuildingName.Contains(kw, StringComparison.OrdinalIgnoreCase));
            }

            // Min Available Beds Validation & Filter
            if (int.TryParse(MinAvailableBedsText?.Trim(), out int minBeds) && minBeds > 0)
            {
                query = query.Where(r => r.AvailableBeds >= minBeds);
            }

            // Building Filter
            if (!string.IsNullOrWhiteSpace(SelectedBuildingFilter) && SelectedBuildingFilter != "Tất cả tòa" && SelectedBuildingFilter != "All")
            {
                query = query.Where(r => r.BuildingName.Equals(SelectedBuildingFilter, StringComparison.OrdinalIgnoreCase));
            }

            // Room Type Filter
            if (!string.IsNullOrWhiteSpace(SelectedRoomType) && SelectedRoomType != "Tất cả loại" && SelectedRoomType != "All")
            {
                query = query.Where(r => r.RoomType.Equals(SelectedRoomType, StringComparison.OrdinalIgnoreCase));
            }

            // Gender Filter
            if (!string.IsNullOrWhiteSpace(SelectedGenderType) && SelectedGenderType != "Tất cả giới tính" && SelectedGenderType != "All")
            {
                query = query.Where(r => r.GenderType.Equals(SelectedGenderType, StringComparison.OrdinalIgnoreCase));
            }

            // Max Monthly Rent Validation & Filter
            if (decimal.TryParse(MaxMonthlyRentText?.Trim(), out decimal maxRent) && maxRent > 0)
            {
                query = query.Where(r => r.MonthlyRent <= maxRent);
            }

            var result = query.ToList();
            AvailableRooms = new ObservableCollection<RoomAvailabilityDto>(result);

            // Compute Summary Stats
            MatchedRoomsCount = result.Count;
            TotalAvailableBedsCount = result.Sum(r => r.AvailableBeds);
            AverageRent = result.Count > 0 ? result.Average(r => r.MonthlyRent) : 0;

            IsEmptyState = AvailableRooms.Count == 0;
            LoadingStateMessage = IsEmptyState ? "🔍 Không tìm thấy phòng nào phù hợp với bộ lọc tìm kiếm." : string.Empty;
        }

        private void ResetFilters()
        {
            _searchKeyword = string.Empty;
            _selectedBuildingFilter = "Tất cả tòa";
            _selectedRoomType = "Tất cả loại";
            _selectedGenderType = "Tất cả giới tính";
            _minAvailableBedsText = "1";
            _maxMonthlyRentText = "5000000";

            OnPropertyChanged(nameof(SearchKeyword));
            OnPropertyChanged(nameof(SelectedBuildingFilter));
            OnPropertyChanged(nameof(SelectedRoomType));
            OnPropertyChanged(nameof(SelectedGenderType));
            OnPropertyChanged(nameof(MinAvailableBedsText));
            OnPropertyChanged(nameof(MaxMonthlyRentText));

            ApplyFilters();
        }
    }
}
