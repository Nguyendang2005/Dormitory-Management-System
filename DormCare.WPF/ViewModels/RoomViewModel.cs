using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using DormCare.Business.DTOs;
using DormCare.Business.Services;
using DormCare.Domain.Entities;
using DormCare.WPF.Commands;
using DormCare.WPF.Services;

namespace DormCare.WPF.ViewModels
{
    public class RoomViewModel : BaseViewModel
    {
        private readonly RoomService _roomService;
        private readonly ApplicationService _applicationService;
        private readonly DialogService _dialogService;
        private readonly User? _currentUser;

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

        private RoomOccupancyDto _occupancyStats = new();
        public RoomOccupancyDto OccupancyStats
        {
            get => _occupancyStats;
            set => SetProperty(ref _occupancyStats, value);
        }

        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    _ = FilterRoomsAsync();
                }
            }
        }

        private string _selectedGenderFilter = "All";
        public string SelectedGenderFilter
        {
            get => _selectedGenderFilter;
            set
            {
                if (SetProperty(ref _selectedGenderFilter, value))
                {
                    _ = FilterRoomsAsync();
                }
            }
        }

        private string _selectedRoomTypeFilter = "All";
        public string SelectedRoomTypeFilter
        {
            get => _selectedRoomTypeFilter;
            set
            {
                if (SetProperty(ref _selectedRoomTypeFilter, value))
                {
                    _ = FilterRoomsAsync();
                }
            }
        }

        public ICommand RefreshCommand { get; }
        public ICommand SearchCommand { get; }
        public ICommand ApplyRoomCommand { get; }

        public RoomViewModel(RoomService roomService, ApplicationService applicationService, DialogService dialogService, User? currentUser = null)
        {
            Title = "🔵 Dạng — Quản Lý Phòng & Thống Kê Giường";
            _roomService = roomService;
            _applicationService = applicationService;
            _dialogService = dialogService;
            _currentUser = currentUser;

            RefreshCommand = new AsyncRelayCommand(LoadDataAsync);
            SearchCommand = new AsyncRelayCommand(FilterRoomsAsync);
            ApplyRoomCommand = new AsyncRelayCommand(ExecuteApplyRoomAsync, () => SelectedRoom != null);

            _ = LoadDataAsync();
        }

        public async Task LoadDataAsync()
        {
            IsBusy = true;
            OccupancyStats = await _roomService.GetOccupancyStatsAsync();
            await FilterRoomsAsync();
            IsBusy = false;
        }

        public async Task FilterRoomsAsync()
        {
            var roomDtos = await _roomService.SearchAndFilterRoomsAsync(null, SelectedGenderFilter, SelectedRoomTypeFilter, SearchText);
            Rooms = new ObservableCollection<RoomDto>(roomDtos);
        }

        private async Task ExecuteApplyRoomAsync()
        {
            if (SelectedRoom == null || _currentUser == null || _currentUser.StudentProfile == null)
            {
                _dialogService.ShowError("Vui lòng chọn phòng trống muốn đăng ký.");
                return;
            }

            if (!_dialogService.ShowConfirmation($"Bạn có chắc chắn muốn gửi đơn đăng ký phòng {SelectedRoom.RoomNumber} ({SelectedRoom.BuildingName})?"))
                return;

            IsBusy = true;
            var result = await _applicationService.CreateApplicationAsync(_currentUser.StudentProfile.StudentId, SelectedRoom.RoomId, "Đăng ký từ ứng dụng DormCare");
            IsBusy = false;

            if (result.IsSuccess)
            {
                _dialogService.ShowInformation(result.Message);
            }
            else
            {
                _dialogService.ShowError(result.Message);
            }
        }
    }
}
