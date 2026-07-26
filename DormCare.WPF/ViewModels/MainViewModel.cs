using System.Windows.Input;
using DormCare.Business.Services;
using DormCare.Domain.Entities;
using DormCare.WPF.Commands;
using DormCare.WPF.Services;

namespace DormCare.WPF.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        private readonly NavigationService _navigationService;
        private readonly StudentService _studentService;
        private readonly RoomService _roomService;
        private readonly BuildingService _buildingService;
        private readonly BedService _bedService;
        private readonly ApplicationService _applicationService;
        private readonly InvoiceService _invoiceService;
        private readonly MaintenanceService _maintenanceService;
        private readonly DialogService _dialogService;

        private User _currentUser;
        public User CurrentUser
        {
            get => _currentUser;
            set => SetProperty(ref _currentUser, value);
        }

        private BaseViewModel _currentChildViewModel = null!;
        public BaseViewModel CurrentChildViewModel
        {
            get => _currentChildViewModel;
            set => SetProperty(ref _currentChildViewModel, value);
        }

        public bool IsManager => CurrentUser.Role == "Manager";
        public bool IsStudent => CurrentUser.Role == "Student";

        public ICommand NavigateDashboardCommand { get; }
        public ICommand NavigateBuildingsCommand { get; }
        public ICommand NavigateRoomsCommand { get; }
        public ICommand NavigateBedsCommand { get; }
        public ICommand NavigateStudentsCommand { get; }
        public ICommand NavigateApplicationsCommand { get; }
        public ICommand NavigateInvoicesCommand { get; }
        public ICommand NavigateMaintenanceCommand { get; }
        public ICommand LogoutCommand { get; }

        public event System.Action? RequestLogout;

        public MainViewModel(
            User user,
            NavigationService navigationService,
            StudentService studentService,
            RoomService roomService,
            BuildingService buildingService,
            BedService bedService,
            ApplicationService applicationService,
            InvoiceService invoiceService,
            MaintenanceService maintenanceService,
            DialogService dialogService)
        {
            _currentUser = user;
            _navigationService = navigationService;
            _studentService = studentService;
            _roomService = roomService;
            _buildingService = buildingService;
            _bedService = bedService;
            _applicationService = applicationService;
            _invoiceService = invoiceService;
            _maintenanceService = maintenanceService;
            _dialogService = dialogService;

            Title = $"DormCare — Ký Túc Xá ({user.Username} - {user.Role})";

            NavigateDashboardCommand = new RelayCommand(ExecuteNavigateDashboard);
            NavigateBuildingsCommand = new RelayCommand(() => CurrentChildViewModel = new BuildingViewModel(_buildingService, _dialogService));
            NavigateRoomsCommand = new RelayCommand(() => CurrentChildViewModel = new RoomViewModel(_roomService, _applicationService, _dialogService, CurrentUser));
            NavigateBedsCommand = new RelayCommand(() => CurrentChildViewModel = new BedViewModel(_bedService, _dialogService));
            NavigateStudentsCommand = new RelayCommand(() => CurrentChildViewModel = new StudentViewModel(_studentService));
            NavigateApplicationsCommand = new RelayCommand(() => CurrentChildViewModel = new ApplicationViewModel(_applicationService, _dialogService, CurrentUser));
            NavigateInvoicesCommand = new RelayCommand(() => CurrentChildViewModel = new InvoiceViewModel(_invoiceService, _dialogService, IsStudent ? CurrentUser.StudentProfile?.StudentId : null));
            NavigateMaintenanceCommand = new RelayCommand(() => CurrentChildViewModel = new MaintenanceViewModel(_maintenanceService, _dialogService, CurrentUser));
            LogoutCommand = new RelayCommand(ExecuteLogout);

            ExecuteNavigateDashboard();
        }

        private void ExecuteNavigateDashboard()
        {
            if (IsStudent)
            {
                CurrentChildViewModel = new StudentDashboardViewModel(_studentService, CurrentUser);
            }
            else
            {
                CurrentChildViewModel = new RoomViewModel(_roomService, _applicationService, _dialogService, CurrentUser);
            }
        }

        private void ExecuteLogout()
        {
            if (_dialogService.ShowConfirmation("Bạn có muốn đăng xuất khỏi hệ thống?"))
            {
                RequestLogout?.Invoke();
            }
        }
    }
}
