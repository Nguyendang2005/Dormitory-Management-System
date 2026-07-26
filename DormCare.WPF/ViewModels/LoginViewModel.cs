using System.Threading.Tasks;
using System.Windows.Input;
using DormCare.Business.Services;
using DormCare.Domain.Entities;
using DormCare.WPF.Commands;
using DormCare.WPF.Services;

namespace DormCare.WPF.ViewModels
{
    public class LoginViewModel : BaseViewModel
    {
        private readonly AuthService _authService;
        private readonly DialogService _dialogService;

        // View Mode: Login vs Register
        private bool _isRegisterMode;
        public bool IsRegisterMode
        {
            get => _isRegisterMode;
            set
            {
                SetProperty(ref _isRegisterMode, value);
                ErrorMessage = string.Empty;
                SuccessMessage = string.Empty;
            }
        }

        // --- LOGIN FIELDS ---
        private string _username = "manager01";
        public string Username
        {
            get => _username;
            set => SetProperty(ref _username, value);
        }

        private string _password = "HASH_MANAGER_01";
        public string Password
        {
            get => _password;
            set => SetProperty(ref _password, value);
        }

        // --- REGISTER FIELDS ---
        private string _regUsername = string.Empty;
        public string RegUsername
        {
            get => _regUsername;
            set => SetProperty(ref _regUsername, value);
        }

        private string _regPassword = string.Empty;
        public string RegPassword
        {
            get => _regPassword;
            set => SetProperty(ref _regPassword, value);
        }

        private string _regConfirmPassword = string.Empty;
        public string RegConfirmPassword
        {
            get => _regConfirmPassword;
            set => SetProperty(ref _regConfirmPassword, value);
        }

        private string _regEmail = string.Empty;
        public string RegEmail
        {
            get => _regEmail;
            set => SetProperty(ref _regEmail, value);
        }

        private string _regPhone = string.Empty;
        public string RegPhone
        {
            get => _regPhone;
            set => SetProperty(ref _regPhone, value);
        }

        private string _regFullName = string.Empty;
        public string RegFullName
        {
            get => _regFullName;
            set => SetProperty(ref _regFullName, value);
        }

        private string _regStudentCode = string.Empty;
        public string RegStudentCode
        {
            get => _regStudentCode;
            set => SetProperty(ref _regStudentCode, value);
        }

        private string _regMajor = "Software Engineering";
        public string RegMajor
        {
            get => _regMajor;
            set => SetProperty(ref _regMajor, value);
        }

        private string _regClassName = "SE1801";
        public string RegClassName
        {
            get => _regClassName;
            set => SetProperty(ref _regClassName, value);
        }

        // --- MESSAGES ---
        private string _errorMessage = string.Empty;
        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        private string _successMessage = string.Empty;
        public string SuccessMessage
        {
            get => _successMessage;
            set => SetProperty(ref _successMessage, value);
        }

        // --- COMMANDS ---
        public ICommand LoginCommand { get; }
        public ICommand RegisterCommand { get; }
        public ICommand ToggleModeCommand { get; }
        public ICommand SelectAdminCommand { get; }
        public ICommand SelectStudentCommand { get; }

        public event System.Action<User>? LoginSuccess;

        public LoginViewModel(AuthService authService, DialogService dialogService)
        {
            Title = "Đăng nhập — DormCare";
            _authService = authService;
            _dialogService = dialogService;

            LoginCommand = new AsyncRelayCommand(ExecuteLoginAsync);
            RegisterCommand = new AsyncRelayCommand(ExecuteRegisterAsync);

            ToggleModeCommand = new RelayCommand(() =>
            {
                IsRegisterMode = !IsRegisterMode;
            });

            SelectAdminCommand = new RelayCommand(() =>
            {
                IsRegisterMode = false;
                Username = "manager01";
                Password = "HASH_MANAGER_01";
            });

            SelectStudentCommand = new RelayCommand(() =>
            {
                IsRegisterMode = false;
                Username = "student1";
                Password = "HASH_STUDENT_1";
            });
        }

        private async Task ExecuteLoginAsync()
        {
            ErrorMessage = string.Empty;
            SuccessMessage = string.Empty;
            IsBusy = true;

            var result = await _authService.LoginAsync(Username, Password);
            IsBusy = false;

            if (result.IsSuccess && result.Data != null)
            {
                LoginSuccess?.Invoke(result.Data);
            }
            else
            {
                ErrorMessage = result.Message;
            }
        }

        private async Task ExecuteRegisterAsync()
        {
            ErrorMessage = string.Empty;
            SuccessMessage = string.Empty;

            if (RegPassword != RegConfirmPassword)
            {
                ErrorMessage = "Mật khẩu xác nhận không trùng khớp.";
                return;
            }

            IsBusy = true;
            var result = await _authService.RegisterStudentAsync(
                RegUsername, RegPassword, RegEmail, RegPhone, 
                RegFullName, RegStudentCode, RegMajor, RegClassName);
            IsBusy = false;

            if (result.IsSuccess)
            {
                SuccessMessage = result.Message;
                _dialogService.ShowInfo(result.Message, "Thành công");
                
                // Pre-fill login with new account and switch to Login tab
                Username = RegUsername;
                Password = RegPassword;
                IsRegisterMode = false;
            }
            else
            {
                ErrorMessage = result.Message;
            }
        }
    }
}
