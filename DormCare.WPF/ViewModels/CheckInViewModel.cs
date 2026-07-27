using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using DormCare.Business.DTOs;
using DormCare.Business.Services;
using DormCare.DataAccess.Data;
using DormCare.Domain.Entities;
using DormCare.WPF.Commands;
using DormCare.WPF.Services;

namespace DormCare.WPF.ViewModels
{
    public class CheckInViewModel : BaseViewModel
    {
        private readonly StudentService _studentService;
        private readonly DormCareDbContext _context;
        private readonly DialogService _dialogService;
        private readonly StudentDto _studentDto;
        private readonly User _currentUser;

        public string StudentName => _studentDto.FullName;
        public string StudentCode => _studentDto.StudentCode;
        public string StudentGender => _studentDto.Gender;

        private ObservableCollection<Building> _buildings = new();
        public ObservableCollection<Building> Buildings
        {
            get => _buildings;
            set => SetProperty(ref _buildings, value);
        }

        private Building? _selectedBuilding;
        public Building? SelectedBuilding
        {
            get => _selectedBuilding;
            set
            {
                if (SetProperty(ref _selectedBuilding, value))
                {
                    OnPropertyChanged(nameof(IsRoomSelectionEnabled));
                    _ = LoadRoomsAsync();
                }
            }
        }

        private ObservableCollection<Room> _rooms = new();
        public ObservableCollection<Room> Rooms
        {
            get => _rooms;
            set => SetProperty(ref _rooms, value);
        }

        private Room? _selectedRoom;
        public Room? SelectedRoom
        {
            get => _selectedRoom;
            set
            {
                if (SetProperty(ref _selectedRoom, value))
                {
                    OnPropertyChanged(nameof(IsBedSelectionEnabled));
                    _ = LoadBedsAsync();
                }
            }
        }

        public bool IsRoomSelectionEnabled => SelectedBuilding != null;
        public bool IsBedSelectionEnabled => SelectedRoom != null;

        private ObservableCollection<Bed> _beds = new();
        public ObservableCollection<Bed> Beds
        {
            get => _beds;
            set => SetProperty(ref _beds, value);
        }

        private Bed? _selectedBed;
        public Bed? SelectedBed
        {
            get => _selectedBed;
            set => SetProperty(ref _selectedBed, value);
        }

        private string _errorMessage = string.Empty;
        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        public ICommand CheckInCommand { get; }
        public ICommand CancelCommand { get; }

        public event Action<bool>? RequestClose;

        public CheckInViewModel(
            StudentService studentService,
            DormCareDbContext context,
            DialogService dialogService,
            StudentDto studentDto,
            User currentUser)
        {
            Title = "🔑 Nhận Phòng (Check-in)";
            _studentService = studentService;
            _context = context;
            _dialogService = dialogService;
            _studentDto = studentDto;
            _currentUser = currentUser;

            CheckInCommand = new AsyncRelayCommand(ExecuteCheckInAsync);
            CancelCommand = new RelayCommand(() => RequestClose?.Invoke(false));

            _ = LoadBuildingsAsync();
        }

        private async Task LoadBuildingsAsync()
        {
            try
            {
                var list = await _context.Buildings.Where(b => b.Status == "Active").ToListAsync();
                Buildings = new ObservableCollection<Building>(list);
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Lỗi tải danh sách tòa nhà: {ex.Message}";
            }
        }

        private async Task LoadRoomsAsync()
        {
            Rooms.Clear();
            Beds.Clear();
            SelectedRoom = null;
            SelectedBed = null;

            if (SelectedBuilding == null) return;

            try
            {
                var list = await _context.Rooms
                    .Where(r => r.BuildingId == SelectedBuilding.BuildingId && 
                                (r.Status == "Available" || r.Status == "Full") &&
                                (r.GenderType == "Mixed" || r.GenderType == _studentDto.Gender))
                    .ToListAsync();
                Rooms = new ObservableCollection<Room>(list);
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Lỗi tải danh sách phòng: {ex.Message}";
            }
        }

        private async Task LoadBedsAsync()
        {
            Beds.Clear();
            SelectedBed = null;

            if (SelectedRoom == null) return;

            try
            {
                var list = await _context.Beds
                    .Where(b => b.RoomId == SelectedRoom.RoomId && b.Status == "Available")
                    .ToListAsync();
                Beds = new ObservableCollection<Bed>(list);
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Lỗi tải danh sách giường: {ex.Message}";
            }
        }

        private async Task ExecuteCheckInAsync()
        {
            ErrorMessage = string.Empty;

            if (SelectedBuilding == null)
            {
                ErrorMessage = "Vui lòng chọn Tòa nhà.";
                return;
            }
            if (SelectedRoom == null)
            {
                ErrorMessage = "Vui lòng chọn Phòng.";
                return;
            }
            if (SelectedBed == null)
            {
                ErrorMessage = "Vui lòng chọn Giường.";
                return;
            }

            IsBusy = true;
            try
            {
                var result = await _studentService.CheckInStudentAsync(_studentDto.Id, SelectedBed.BedId, _currentUser.UserId);
                IsBusy = false;

                if (result.IsSuccess)
                {
                    RequestClose?.Invoke(true);
                }
                else
                {
                    ErrorMessage = result.Message;
                }
            }
            catch (Exception ex)
            {
                IsBusy = false;
                ErrorMessage = $"Lỗi check-in: {ex.Message}";
            }
        }
    }
}
