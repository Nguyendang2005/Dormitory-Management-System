using System.Threading.Tasks;
using System.Linq;
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
        private readonly BuildingService _buildingService;
        private readonly ApplicationService _applicationService;
        private readonly InvoiceService _invoiceService;
        private readonly PaymentService _paymentService;
        private readonly DialogService _dialogService;
        private readonly NotificationService _notificationService;
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

        private AvailableRoomViewModel? _availableRoomsViewModel;
        public AvailableRoomViewModel? AvailableRoomsViewModel
        {
            get => _availableRoomsViewModel;
            set => SetProperty(ref _availableRoomsViewModel, value);
        }

        private InvoiceViewModel? _invoiceViewModel;
        public InvoiceViewModel? InvoiceViewModel
        {
            get => _invoiceViewModel;
            set => SetProperty(ref _invoiceViewModel, value);
        }

        private bool _isProfileVisible = true;
        public bool IsProfileVisible
        {
            get => _isProfileVisible;
            set => SetProperty(ref _isProfileVisible, value);
        }

        private bool _isAvailableRoomsVisible;
        public bool IsAvailableRoomsVisible
        {
            get => _isAvailableRoomsVisible;
            set => SetProperty(ref _isAvailableRoomsVisible, value);
        }

        private bool _isRoomRegistrationVisible;
        public bool IsRoomRegistrationVisible
        {
            get => _isRoomRegistrationVisible;
            set => SetProperty(ref _isRoomRegistrationVisible, value);
        }

        private bool _isInvoiceVisible;
        public bool IsInvoiceVisible
        {
            get => _isInvoiceVisible;
            set => SetProperty(ref _isInvoiceVisible, value);
        }

        private bool _isMaintenanceVisible;
        public bool IsMaintenanceVisible
        {
            get => _isMaintenanceVisible;
            set => SetProperty(ref _isMaintenanceVisible, value);
        }

        private bool _isNotificationsVisible;
        public bool IsNotificationsVisible
        {
            get => _isNotificationsVisible;
            set => SetProperty(ref _isNotificationsVisible, value);
        }

        private MaintenanceViewModel? _maintenanceViewModel;
        public MaintenanceViewModel? MaintenanceViewModel
        {
            get => _maintenanceViewModel;
            set => SetProperty(ref _maintenanceViewModel, value);
        }

        private NotificationsViewModel? _notificationsViewModel;
        public NotificationsViewModel? NotificationsViewModel
        {
            get => _notificationsViewModel;
            set
            {
                if (SetProperty(ref _notificationsViewModel, value))
                {
                    if (_notificationsViewModel != null)
                    {
                        _notificationsViewModel.PropertyChanged += (s, e) =>
                        {
                            if (e.PropertyName == nameof(NotificationsViewModel.UnreadCount))
                            {
                                UnreadNotificationsCount = _notificationsViewModel.UnreadCount;
                            }
                        };
                    }
                }
            }
        }

        private int _unreadNotificationsCount;
        public int UnreadNotificationsCount
        {
            get => _unreadNotificationsCount;
            set
            {
                if (SetProperty(ref _unreadNotificationsCount, value))
                {
                    OnPropertyChanged(nameof(HasUnreadNotifications));
                }
            }
        }

        public bool HasUnreadNotifications => UnreadNotificationsCount > 0;

        private string _activeTabName = "Profile";
        public string ActiveTabName
        {
            get => _activeTabName;
            set => SetProperty(ref _activeTabName, value);
        }

        public ICommand NavigateProfileCommand { get; }
        public ICommand NavigateAvailableRoomsCommand { get; }
        public ICommand NavigateRoomRegistrationCommand { get; }
        public ICommand NavigateInvoiceCommand { get; }
        public ICommand NavigateMaintenanceCommand { get; }
        public ICommand NavigateNotificationsCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand LogoutCommand { get; }

        public event System.Action? RequestLogout;

        public StudentDashboardViewModel(
            StudentService studentService,
            RoomService roomService,
            BuildingService buildingService,
            ApplicationService applicationService,
            InvoiceService invoiceService,
            PaymentService paymentService,
            DialogService dialogService,
            MaintenanceService maintenanceService,
            NotificationService notificationService,
            User currentUser)
        {
            Title = "Trang chủ Sinh viên";
            _studentService = studentService;
            _roomService = roomService;
            _buildingService = buildingService;
            _applicationService = applicationService;
            _invoiceService = invoiceService;
            _paymentService = paymentService;
            _dialogService = dialogService;
            _notificationService = notificationService;
            _currentUser = currentUser;

            LogoutCommand = new RelayCommand(() => RequestLogout?.Invoke());

            NavigateProfileCommand = new RelayCommand(() =>
            {
                ActiveTabName = "Profile";
                IsProfileVisible = true;
                IsAvailableRoomsVisible = false;
                IsRoomRegistrationVisible = false;
                IsInvoiceVisible = false;
            });

            NavigateAvailableRoomsCommand = new AsyncRelayCommand(ShowAvailableRoomsAsync);

            NavigateRoomRegistrationCommand = new AsyncRelayCommand(() => ShowRoomRegistrationAsync(null));

            NavigateInvoiceCommand = new AsyncRelayCommand(async () =>
            {
                ActiveTabName = "Invoice";
                if (Student == null)
                {
                    await LoadStudentDataAsync();
                }
                if (Student != null && InvoiceViewModel == null)
                {
                    InvoiceViewModel = new InvoiceViewModel(_invoiceService, _paymentService, _studentService, _roomService, _dialogService, Student.StudentId);
                }
                IsProfileVisible = false;
                IsAvailableRoomsVisible = false;
                IsRoomRegistrationVisible = false;
                IsInvoiceVisible = true;
                IsMaintenanceVisible = false;
                IsNotificationsVisible = false;
            });

            NavigateMaintenanceCommand = new RelayCommand(() =>
            {
                ActiveTabName = "Maintenance";
                if (MaintenanceViewModel == null)
                {
                    MaintenanceViewModel = new MaintenanceViewModel(maintenanceService, _roomService, _studentService, _dialogService, _currentUser);
                }
                IsProfileVisible = false;
                IsRoomRegistrationVisible = false;
                IsInvoiceVisible = false;
                IsMaintenanceVisible = true;
                IsNotificationsVisible = false;
            });

            NavigateNotificationsCommand = new RelayCommand(() =>
            {
                ActiveTabName = "Notifications";
                if (NotificationsViewModel == null)
                {
                    NotificationsViewModel = new NotificationsViewModel(_notificationService, _currentUser.UserId);
                }
                else
                {
                    _ = NotificationsViewModel.LoadNotificationsAsync();
                }
                IsProfileVisible = false;
                IsRoomRegistrationVisible = false;
                IsInvoiceVisible = false;
                IsMaintenanceVisible = false;
                IsNotificationsVisible = true;
            });

            RefreshCommand = new AsyncRelayCommand(LoadStudentDataAsync);

            _ = LoadStudentDataAsync();
            _ = LoadNotificationsCountAsync();
        }

        private async Task ShowAvailableRoomsAsync()
        {
            ActiveTabName = "AvailableRooms";
            AvailableRoomsViewModel ??= new AvailableRoomViewModel(_roomService, _buildingService, roomId => ShowRoomRegistrationAsync(roomId));
            IsProfileVisible = false;
            IsAvailableRoomsVisible = true;
            IsRoomRegistrationVisible = false;
            IsInvoiceVisible = false;
            IsMaintenanceVisible = false;
            IsNotificationsVisible = false;

            await Task.CompletedTask;
        }

        private async Task ShowRoomRegistrationAsync(int? selectedRoomId)
        {
            if (Student == null)
            {
                await LoadStudentDataAsync();
            }

            if (StudentHasActiveAssignment())
            {
                _dialogService.ShowError(BuildActiveAssignmentMessage());
                return;
            }

            ActiveTabName = "RoomRegistration";
            RoomRegistrationViewModel = new StudentRoomRegistrationViewModel(
                _roomService,
                _applicationService,
                _dialogService,
                Student,
                selectedRoomId,
                ShowAvailableRoomsAsync);
            IsProfileVisible = false;
            IsAvailableRoomsVisible = false;
            IsRoomRegistrationVisible = true;
            IsInvoiceVisible = false;
            IsMaintenanceVisible = false;
            IsNotificationsVisible = false;
        }

        private bool StudentHasActiveAssignment()
        {
            return Student?.RoomAssignments.Any(a => a.Status == "Active") == true;
        }

        private string BuildActiveAssignmentMessage()
        {
            var activeAssignment = Student?.RoomAssignments.FirstOrDefault(a => a.Status == "Active");
            if (activeAssignment?.Room != null && activeAssignment.Bed != null)
            {
                return $"Ban hien dang o phong {activeAssignment.Room.RoomNumber}, giuong {activeAssignment.Bed.BedCode}. Vui long check-out phong hien tai truoc khi dang ky phong moi.";
            }

            return "Ban dang co phong trong ky tuc xa. Vui long hoan thanh thu tuc tra phong truoc khi dang ky phong moi.";
        }

        private async Task LoadNotificationsCountAsync()
        {
            if (NotificationsViewModel == null)
            {
                NotificationsViewModel = new NotificationsViewModel(_notificationService, _currentUser.UserId);
            }
            await NotificationsViewModel.LoadNotificationsAsync();
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
