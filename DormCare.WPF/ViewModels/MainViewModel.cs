using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using DormCare.DataAccess.Data;
using DormCare.Business.Services;
using DormCare.Domain.Entities;
using DormCare.WPF.Commands;
using DormCare.WPF.Services;

namespace DormCare.WPF.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        private readonly AuthService _authService;
        private readonly DialogService _dialogService;
        private readonly BuildingService _buildingService;
        private readonly RoomService _roomService;
        private readonly BedService _bedService;
        private readonly StudentService _studentService;
        private readonly ApplicationService _applicationService;
        private readonly InvoiceService _invoiceService;
        private readonly PaymentService _paymentService;
        private readonly MaintenanceService _maintenanceService;
        private readonly OccupancyService _occupancyService;

        public User CurrentUser { get; }

        public bool IsManager => CurrentUser.Role == "Manager";

        private BaseViewModel _currentView;
        public BaseViewModel CurrentView
        {
            get => _currentView;
            set => SetProperty(ref _currentView, value);
        }

        private string _activeTabName = "Buildings";
        public string ActiveTabName
        {
            get => _activeTabName;
            set => SetProperty(ref _activeTabName, value);
        }

        public ICommand NavigateDashboardCommand { get; }
        public ICommand NavigateBuildingsCommand { get; }
        public ICommand NavigateRoomsCommand { get; }
        public ICommand NavigateBedsCommand { get; }
        public ICommand NavigateAvailableRoomsCommand { get; }
        public ICommand NavigateOccupancyCommand { get; }
        public ICommand NavigateStudentsCommand { get; }
        public ICommand NavigateApplicationsCommand { get; }
        public ICommand NavigateInvoicesCommand { get; }
        public ICommand NavigateMaintenanceCommand { get; }
        public ICommand LogoutCommand { get; }

        public event System.Action? RequestLogout;

        public MainViewModel(
            User currentUser,
            AuthService authService,
            DialogService dialogService,
            BuildingService buildingService,
            RoomService roomService,
            BedService bedService,
            StudentService studentService,
            ApplicationService applicationService,
            InvoiceService invoiceService,
            PaymentService paymentService,
            MaintenanceService maintenanceService,
            OccupancyService occupancyService)
        {
            Title = "DormCare — Quản Lý Ký Túc Xá Sinh Viên";
            CurrentUser = currentUser;
            _authService = authService;
            _dialogService = dialogService;
            _buildingService = buildingService;
            _roomService = roomService;
            _bedService = bedService;
            _studentService = studentService;
            _applicationService = applicationService;
            _invoiceService = invoiceService;
            _paymentService = paymentService;
            _maintenanceService = maintenanceService;
            _occupancyService = occupancyService;

            NavigateDashboardCommand = new RelayCommand(() =>
            {
                ActiveTabName = "Dashboard";
                CurrentView = new OccupancyStatisticsViewModel(_occupancyService);
            });

            NavigateBuildingsCommand = new RelayCommand(() =>
            {
                ActiveTabName = "Buildings";
                CurrentView = new BuildingViewModel(_buildingService, _dialogService);
            });

            NavigateRoomsCommand = new RelayCommand(() =>
            {
                ActiveTabName = "Rooms";
                CurrentView = new RoomViewModel(_roomService, _buildingService, _dialogService, _applicationService, CurrentUser);
            });

            NavigateBedsCommand = new RelayCommand(() =>
            {
                ActiveTabName = "Beds";
                CurrentView = new BedViewModel(_bedService, _dialogService);
            });

            NavigateAvailableRoomsCommand = new RelayCommand(() =>
            {
                ActiveTabName = "AvailableRooms";
                CurrentView = new AvailableRoomViewModel(_roomService, _buildingService);
            });

            NavigateOccupancyCommand = new RelayCommand(() =>
            {
                ActiveTabName = "Occupancy";
                CurrentView = new OccupancyStatisticsViewModel(_occupancyService);
            });

            NavigateStudentsCommand = new RelayCommand(() =>
            {
                ActiveTabName = "Students";
                CurrentView = new StudentViewModel(_studentService, _dialogService, CurrentUser);
            });

            NavigateApplicationsCommand = new RelayCommand(() =>
            {
                ActiveTabName = "Applications";
                CurrentView = new ApplicationViewModel(_applicationService, _dialogService, CurrentUser);
            });

            NavigateInvoicesCommand = new RelayCommand(() =>
            {
                ActiveTabName = "Invoices";
                CurrentView = new InvoiceViewModel(_invoiceService, _paymentService, _studentService, _roomService, _dialogService);
            });

            NavigateMaintenanceCommand = new RelayCommand(() =>
            {
                ActiveTabName = "Maintenance";
                CurrentView = new MaintenanceViewModel(_maintenanceService, _roomService, _studentService, _dialogService, CurrentUser);
            });

            LogoutCommand = new RelayCommand(() => RequestLogout?.Invoke());

            // Default startup view: Buildings & Rooms (as specified)
            _currentView = new BuildingViewModel(_buildingService, _dialogService);
        }
    }
}
