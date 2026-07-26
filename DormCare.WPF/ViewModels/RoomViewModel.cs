using System;
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
        private readonly ApplicationService? _applicationService;
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

        public RoomViewModel(RoomService roomService, DialogService dialogService, ApplicationService? applicationService = null, User? currentUser = null)
        {
            Title = "Quản Lý Phòng & Thống Kê Giường";
            _roomService = roomService;
            _dialogService = dialogService;
            _applicationService = applicationService;
            _currentUser = currentUser;

            RefreshCommand = new AsyncRelayCommand(LoadRoomsAsync);
            SearchCommand = new AsyncRelayCommand(FilterRoomsAsync);
            ApplyRoomCommand = new AsyncRelayCommand(ExecuteApplyRoomAsync, () => SelectedRoom != null);

            _ = LoadRoomsAsync();
        }

        public async Task LoadRoomsAsync()
        {
            IsBusy = true;
            var dtos = await _roomService.GetAllRoomsAsync();
            Rooms = new ObservableCollection<RoomDto>(dtos);

            OccupancyStats = await _roomService.GetOccupancyStatsAsync();
            IsBusy = false;
        }

        public async Task FilterRoomsAsync()
        {
            IsBusy = true;
            var filtered = await _roomService.SearchAndFilterRoomsAsync(null, SelectedGenderFilter, SelectedRoomTypeFilter, SearchText);
            Rooms = new ObservableCollection<RoomDto>(filtered);
            IsBusy = false;
        }

        private async Task ExecuteApplyRoomAsync()
        {
            if (SelectedRoom == null) return;
            if (_currentUser == null || _currentUser.Role != "Student" || _currentUser.StudentProfile == null)
            {
                _dialogService.ShowInformation("Tính năng Đăng ký ở phòng này dành cho sinh viên.", "Thông báo");
                return;
            }

            if (_applicationService == null) return;

            IsBusy = true;
            var result = await _applicationService.SubmitApplicationAsync(_currentUser.StudentProfile.StudentId, SelectedRoom.RoomId, null, "Đăng ký ở từ giao diện danh sách phòng");
            IsBusy = false;

            if (result.IsSuccess)
            {
                _dialogService.ShowInformation(result.Message, "Thành công");
            }
            else
            {
                _dialogService.ShowError(result.Message, "Không thể đăng ký");
            }
        }
    }
}
