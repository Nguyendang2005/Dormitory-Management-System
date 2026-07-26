using System.Threading.Tasks;
using System.Windows.Input;
using DormCare.Business.Services;
using DormCare.Domain.Entities;
using DormCare.WPF.Commands;
using DormCare.WPF.Services;

namespace DormCare.WPF.ViewModels
{
    public class StudentDashboardViewModel : BaseViewModel
    {
        private readonly StudentService _studentService;
        private readonly RoomService _roomService;
        private readonly ApplicationService _applicationService;
        private readonly DialogService _dialogService;
        private readonly User _currentUser;

        private Student? _student;
        public Student? Student
        {
            get => _student;
            set => SetProperty(ref _student, value);
        }

        private StudentRoomRegistrationViewModel? _roomRegistrationViewModel;
        public StudentRoomRegistrationViewModel? RoomRegistrationViewModel
        {
            get => _roomRegistrationViewModel;
            set => SetProperty(ref _roomRegistrationViewModel, value);
        }

        private bool _isProfileVisible = true;
        public bool IsProfileVisible
        {
            get => _isProfileVisible;
            set => SetProperty(ref _isProfileVisible, value);
        }

        private bool _isRoomRegistrationVisible;
        public bool IsRoomRegistrationVisible
        {
            get => _isRoomRegistrationVisible;
            set => SetProperty(ref _isRoomRegistrationVisible, value);
        }

        private string _activeTabName = "Profile";
        public string ActiveTabName
        {
            get => _activeTabName;
            set => SetProperty(ref _activeTabName, value);
        }

        public ICommand NavigateProfileCommand { get; }
        public ICommand NavigateRoomRegistrationCommand { get; }
        public ICommand RefreshCommand { get; }

        public StudentDashboardViewModel(
            StudentService studentService,
            RoomService roomService,
            ApplicationService applicationService,
            DialogService dialogService,
            User currentUser)
        {
            Title = "Trang chu Sinh vien";
            _studentService = studentService;
            _roomService = roomService;
            _applicationService = applicationService;
            _dialogService = dialogService;
            _currentUser = currentUser;

            NavigateProfileCommand = new RelayCommand(() =>
            {
                ActiveTabName = "Profile";
                IsProfileVisible = true;
                IsRoomRegistrationVisible = false;
            });

            NavigateRoomRegistrationCommand = new RelayCommand(() =>
            {
                ActiveTabName = "RoomRegistration";
                RoomRegistrationViewModel = new StudentRoomRegistrationViewModel(_roomService, _applicationService, _dialogService, Student);
                IsProfileVisible = false;
                IsRoomRegistrationVisible = true;
            });

            RefreshCommand = new AsyncRelayCommand(LoadStudentDataAsync);

            _ = LoadStudentDataAsync();
        }

        public async Task LoadStudentDataAsync()
        {
            IsBusy = true;
            try
            {
                Student = await _studentService.GetStudentByUserIdAsync(_currentUser.UserId);
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
