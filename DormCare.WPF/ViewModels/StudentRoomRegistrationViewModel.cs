using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using DormCare.Business.DTOs;
using DormCare.Business.Services;
using DormCare.Domain.Entities;
using DormCare.WPF.Commands;
using DormCare.WPF.Services;

namespace DormCare.WPF.ViewModels
{
    public class StudentRoomRegistrationViewModel : BaseViewModel
    {
        private readonly RoomService _roomService;
        private readonly ApplicationService _applicationService;
        private readonly DialogService _dialogService;
        private readonly Student? _student;
        private readonly Func<Task>? _navigateBackToRoomSearchAsync;
        private readonly int? _initialRoomId;

        public ObservableCollection<BedDto> AvailableBeds { get; private set; } = new();
        public ObservableCollection<RoomApplication> MyApplications { get; private set; } = new();

        private RoomAvailabilityDto? _selectedRoom;
        public RoomAvailabilityDto? SelectedRoom
        {
            get => _selectedRoom;
            private set
            {
                if (SetProperty(ref _selectedRoom, value))
                {
                    OnPropertyChanged(nameof(HasSelectedRoom));
                    OnPropertyChanged(nameof(NoSelectedRoom));
                }
            }
        }

        public bool HasSelectedRoom => SelectedRoom != null;
        public bool NoSelectedRoom => SelectedRoom == null;

        private BedDto? _selectedBed;
        public BedDto? SelectedBed
        {
            get => _selectedBed;
            set => SetProperty(ref _selectedBed, value);
        }

        private string _reason = string.Empty;
        public string Reason
        {
            get => _reason;
            set => SetProperty(ref _reason, value);
        }

        private string _message = string.Empty;
        public string Message
        {
            get => _message;
            set => SetProperty(ref _message, value);
        }

        public ICommand RefreshCommand { get; }
        public ICommand SubmitCommand { get; }
        public ICommand BackToRoomSearchCommand { get; }

        public StudentRoomRegistrationViewModel(
            RoomService roomService,
            ApplicationService applicationService,
            DialogService dialogService,
            Student? student,
            int? selectedRoomId = null,
            Func<Task>? navigateBackToRoomSearchAsync = null)
        {
            Title = "Dang ky phong";
            _roomService = roomService;
            _applicationService = applicationService;
            _dialogService = dialogService;
            _student = student;
            _initialRoomId = selectedRoomId;
            _navigateBackToRoomSearchAsync = navigateBackToRoomSearchAsync;

            RefreshCommand = new AsyncRelayCommand(LoadAsync);
            SubmitCommand = new AsyncRelayCommand(SubmitAsync, CanSubmit);
            BackToRoomSearchCommand = new AsyncRelayCommand(BackToRoomSearchAsync);

            _ = LoadAsync();
        }

        public async Task LoadAsync()
        {
            IsBusy = true;
            ClearStatus();
            try
            {
                await LoadSelectedRoomAsync();
                await LoadMyApplicationsAsync();
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task LoadSelectedRoomAsync()
        {
            SelectedBed = null;
            AvailableBeds = new ObservableCollection<BedDto>();

            if (!_initialRoomId.HasValue)
            {
                SelectedRoom = null;
                Message = "Ban chua chon phong. Vui long tim va chon mot phong con cho truoc khi dang ky.";
                SetWarning(Message);
                OnPropertyChanged(nameof(AvailableBeds));
                return;
            }

            var rooms = await _roomService.GetAvailableRoomsAsync();
            var room = rooms.FirstOrDefault(r => r.RoomId == _initialRoomId.Value);
            SelectedRoom = room == null ? null : MapToAvailabilityDto(room);

            if (SelectedRoom == null)
            {
                Message = "Phong da chon khong con san sang dang ky. Vui long chon phong khac.";
                SetWarning(Message);
                OnPropertyChanged(nameof(AvailableBeds));
                return;
            }

            AvailableBeds = new ObservableCollection<BedDto>(
                await _applicationService.GetAvailableBedsByRoomAsync(SelectedRoom.RoomId));
            SelectedBed = AvailableBeds.FirstOrDefault();
            Message = string.Empty;
            OnPropertyChanged(nameof(AvailableBeds));
        }

        private async Task LoadMyApplicationsAsync()
        {
            MyApplications = _student == null
                ? new ObservableCollection<RoomApplication>()
                : new ObservableCollection<RoomApplication>(await _applicationService.GetStudentApplicationsAsync(_student.StudentId));

            OnPropertyChanged(nameof(MyApplications));
        }

        private static RoomAvailabilityDto MapToAvailabilityDto(RoomDto room)
        {
            return new RoomAvailabilityDto
            {
                RoomId = room.RoomId,
                BuildingId = room.BuildingId,
                BuildingName = room.BuildingName,
                RoomNumber = room.RoomNumber,
                FloorNumber = room.FloorNumber,
                RoomType = room.RoomType,
                Capacity = room.Capacity,
                MonthlyRent = room.MonthlyRent,
                GenderType = room.GenderType,
                Status = room.Status,
                OccupiedBeds = room.OccupiedBeds,
                AvailableBeds = room.AvailableBeds,
                ReservedBeds = room.ReservedBeds,
                MaintenanceBeds = room.MaintenanceBeds,
                TotalBedsCreated = room.TotalBedsCreated
            };
        }

        private bool CanSubmit()
        {
            return !IsBusy && _student != null && SelectedRoom != null && SelectedBed != null && !string.IsNullOrWhiteSpace(Reason);
        }

        private async Task SubmitAsync()
        {
            if (!CanSubmit())
            {
                SetError("Vui long chon giuong con trong va nhap ly do dang ky.");
                _dialogService.ShowError("Vui long chon giuong con trong va nhap ly do dang ky.");
                return;
            }

            var confirm = _dialogService.ShowConfirmation($"Gui yeu cau dang ky phong {SelectedRoom!.RoomNumber}, giuong {SelectedBed!.BedCode}?");
            if (!confirm)
            {
                return;
            }

            IsBusy = true;
            ClearStatus();
            try
            {
                var result = await _applicationService.SubmitApplicationAsync(_student!.StudentId, SelectedRoom.RoomId, SelectedBed.BedId, Reason);
                Message = result.Message;

                if (result.IsSuccess)
                {
                    SetSuccess(result.Message);
                    _dialogService.ShowInformation(result.Message);
                    Reason = string.Empty;
                    await LoadAsync();
                }
                else
                {
                    SetError(result.Message);
                    _dialogService.ShowError(result.Message);
                    await LoadSelectedRoomAsync();
                }
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task BackToRoomSearchAsync()
        {
            if (_navigateBackToRoomSearchAsync != null)
            {
                await _navigateBackToRoomSearchAsync();
            }
        }
    }
}
