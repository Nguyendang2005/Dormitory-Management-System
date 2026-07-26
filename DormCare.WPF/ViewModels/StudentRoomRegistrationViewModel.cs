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

        private ObservableCollection<RoomAvailabilityDto> _allRooms = new();

        public ObservableCollection<RoomAvailabilityDto> AvailableRooms { get; private set; } = new();
        public ObservableCollection<BedDto> AvailableBeds { get; private set; } = new();
        public ObservableCollection<RoomApplication> MyApplications { get; private set; } = new();

        private RoomAvailabilityDto? _selectedRoom;
        public RoomAvailabilityDto? SelectedRoom
        {
            get => _selectedRoom;
            set
            {
                if (SetProperty(ref _selectedRoom, value))
                {
                    SelectedBed = null;
                    _ = LoadAvailableBedsAsync();
                }
            }
        }

        private BedDto? _selectedBed;
        public BedDto? SelectedBed
        {
            get => _selectedBed;
            set => SetProperty(ref _selectedBed, value);
        }

        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    ApplyFilters();
                }
            }
        }

        private string _selectedRoomType = "All";
        public string SelectedRoomType
        {
            get => _selectedRoomType;
            set
            {
                if (SetProperty(ref _selectedRoomType, value))
                {
                    ApplyFilters();
                }
            }
        }

        private string _selectedGenderType = "All";
        public string SelectedGenderType
        {
            get => _selectedGenderType;
            set
            {
                if (SetProperty(ref _selectedGenderType, value))
                {
                    ApplyFilters();
                }
            }
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
        public ICommand ResetFiltersCommand { get; }
        public ICommand SubmitCommand { get; }

        public StudentRoomRegistrationViewModel(
            RoomService roomService,
            ApplicationService applicationService,
            DialogService dialogService,
            Student? student)
        {
            Title = "Dang ky phong";
            _roomService = roomService;
            _applicationService = applicationService;
            _dialogService = dialogService;
            _student = student;

            RefreshCommand = new AsyncRelayCommand(LoadAsync);
            ResetFiltersCommand = new RelayCommand(ResetFilters);
            SubmitCommand = new AsyncRelayCommand(SubmitAsync, CanSubmit);

            _ = LoadAsync();
        }

        public async Task LoadAsync()
        {
            IsBusy = true;
            try
            {
                var rooms = await _roomService.GetAvailableRoomsAsync();
                _allRooms = new ObservableCollection<RoomAvailabilityDto>(rooms.Select(r => new RoomAvailabilityDto
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
                    AvailableBeds = r.AvailableBeds,
                    ReservedBeds = r.ReservedBeds,
                    MaintenanceBeds = r.MaintenanceBeds,
                    TotalBedsCreated = r.TotalBedsCreated
                }));

                ApplyFilters();
                await LoadMyApplicationsAsync();
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task LoadAvailableBedsAsync()
        {
            AvailableBeds = SelectedRoom == null
                ? new ObservableCollection<BedDto>()
                : new ObservableCollection<BedDto>(await _applicationService.GetAvailableBedsByRoomAsync(SelectedRoom.RoomId));

            OnPropertyChanged(nameof(AvailableBeds));
        }

        private async Task LoadMyApplicationsAsync()
        {
            MyApplications = _student == null
                ? new ObservableCollection<RoomApplication>()
                : new ObservableCollection<RoomApplication>(await _applicationService.GetStudentApplicationsAsync(_student.StudentId));

            OnPropertyChanged(nameof(MyApplications));
        }

        private void ApplyFilters()
        {
            var query = _allRooms.Where(r => r.Status == "Available" && r.AvailableBeds > 0);

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                query = query.Where(r =>
                    r.RoomNumber.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    r.BuildingName.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
            }

            if (SelectedRoomType != "All")
            {
                query = query.Where(r => r.RoomType.Equals(SelectedRoomType, StringComparison.OrdinalIgnoreCase));
            }

            if (SelectedGenderType != "All")
            {
                query = query.Where(r => r.GenderType.Equals(SelectedGenderType, StringComparison.OrdinalIgnoreCase));
            }

            AvailableRooms = new ObservableCollection<RoomAvailabilityDto>(query);
            OnPropertyChanged(nameof(AvailableRooms));
        }

        private void ResetFilters()
        {
            SearchText = string.Empty;
            SelectedRoomType = "All";
            SelectedGenderType = "All";
            ApplyFilters();
        }

        private bool CanSubmit()
        {
            return !IsBusy && _student != null && SelectedRoom != null && SelectedBed != null && !string.IsNullOrWhiteSpace(Reason);
        }

        private async Task SubmitAsync()
        {
            if (!CanSubmit())
            {
                _dialogService.ShowError("Vui long chon phong, chon giuong va nhap ly do dang ky.");
                return;
            }

            var confirm = _dialogService.ShowConfirmation($"Gui yeu cau dang ky phong {SelectedRoom!.RoomNumber}, giuong {SelectedBed!.BedCode}?");
            if (!confirm)
            {
                return;
            }

            IsBusy = true;
            try
            {
                var result = await _applicationService.SubmitApplicationAsync(_student!.StudentId, SelectedRoom.RoomId, SelectedBed.BedId, Reason);
                Message = result.Message;

                if (result.IsSuccess)
                {
                    _dialogService.ShowInformation(result.Message);
                    Reason = string.Empty;
                    await LoadAsync();
                }
                else
                {
                    _dialogService.ShowError(result.Message);
                    await LoadAvailableBedsAsync();
                }
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
