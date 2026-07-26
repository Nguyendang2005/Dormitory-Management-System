using System;
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

        private ObservableCollection<RoomAvailabilityDto> _availableRooms = new();
        public ObservableCollection<RoomAvailabilityDto> AvailableRooms
        {
            get => _availableRooms;
            set => SetProperty(ref _availableRooms, value);
        }

        private string _selectedBuilding = "All";
        public string SelectedBuilding
        {
            get => _selectedBuilding;
            set
            {
                if (SetProperty(ref _selectedBuilding, value)) ApplyFilters();
            }
        }

        private string _selectedRoomType = "All";
        public string SelectedRoomType
        {
            get => _selectedRoomType;
            set
            {
                if (SetProperty(ref _selectedRoomType, value)) ApplyFilters();
            }
        }

        private string _selectedGenderType = "All";
        public string SelectedGenderType
        {
            get => _selectedGenderType;
            set
            {
                if (SetProperty(ref _selectedGenderType, value)) ApplyFilters();
            }
        }

        private int _minAvailableBeds = 1;
        public int MinAvailableBeds
        {
            get => _minAvailableBeds;
            set
            {
                if (SetProperty(ref _minAvailableBeds, value)) ApplyFilters();
            }
        }

        private decimal _maxMonthlyRent = 5000000;
        public decimal MaxMonthlyRent
        {
            get => _maxMonthlyRent;
            set
            {
                if (SetProperty(ref _maxMonthlyRent, value)) ApplyFilters();
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

        public ICommand ResetFiltersCommand { get; }
        public ICommand RefreshCommand { get; }

        private ObservableCollection<RoomAvailabilityDto> _allRoomsCache = new();

        public AvailableRoomViewModel(RoomService roomService)
        {
            Title = "Tìm phòng còn chỗ";
            _roomService = roomService;

            ResetFiltersCommand = new RelayCommand(ResetFilters);
            RefreshCommand = new AsyncRelayCommand(LoadAvailableRoomsAsync);

            _ = LoadAvailableRoomsAsync();
        }

        public async Task LoadAvailableRoomsAsync()
        {
            IsBusy = true;
            LoadingStateMessage = "Đang tìm kiếm phòng còn chỗ...";
            IsEmptyState = false;

            var dtos = await _roomService.GetAvailableRoomsAsync();
            var roomAvailabilities = dtos.Select(r => new RoomAvailabilityDto
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

            _allRoomsCache = new ObservableCollection<RoomAvailabilityDto>(roomAvailabilities);
            ApplyFilters();
            IsBusy = false;
        }

        private void ApplyFilters()
        {
            // Only rooms where Available Beds > 0 and Status is NOT Full/Inactive/Maintenance
            var query = _allRoomsCache.Where(r => r.AvailableBeds >= MinAvailableBeds && r.Status == "Available");

            if (SelectedBuilding != "All" && !string.IsNullOrWhiteSpace(SelectedBuilding))
            {
                query = query.Where(r => r.BuildingName.Equals(SelectedBuilding, StringComparison.OrdinalIgnoreCase) ||
                                         r.BuildingId.ToString() == SelectedBuilding);
            }

            if (SelectedRoomType != "All" && !string.IsNullOrWhiteSpace(SelectedRoomType))
            {
                query = query.Where(r => r.RoomType.Equals(SelectedRoomType, StringComparison.OrdinalIgnoreCase));
            }

            if (SelectedGenderType != "All" && !string.IsNullOrWhiteSpace(SelectedGenderType))
            {
                query = query.Where(r => r.GenderType.Equals(SelectedGenderType, StringComparison.OrdinalIgnoreCase));
            }

            if (MaxMonthlyRent > 0)
            {
                query = query.Where(r => r.MonthlyRent <= MaxMonthlyRent);
            }

            var result = query.ToList();
            AvailableRooms = new ObservableCollection<RoomAvailabilityDto>(result);
            IsEmptyState = AvailableRooms.Count == 0;
            LoadingStateMessage = IsEmptyState ? "Không tìm thấy phòng nào phù hợp với bộ lọc." : string.Empty;
        }

        private void ResetFilters()
        {
            _selectedBuilding = "All";
            _selectedRoomType = "All";
            _selectedGenderType = "All";
            _minAvailableBeds = 1;
            _maxMonthlyRent = 5000000;

            OnPropertyChanged(nameof(SelectedBuilding));
            OnPropertyChanged(nameof(SelectedRoomType));
            OnPropertyChanged(nameof(SelectedGenderType));
            OnPropertyChanged(nameof(MinAvailableBeds));
            OnPropertyChanged(nameof(MaxMonthlyRent));

            ApplyFilters();
        }
    }
}
