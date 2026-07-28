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

        // Active Selected Role Toggle: true = Admin/Manager, false = Student
        private bool _isAdminSelected = true;
        public bool IsAdminSelected
        {
            get => _isAdminSelected;
            set
            {
                if (SetProperty(ref _isAdminSelected, value))
                {
                    OnPropertyChanged(nameof(IsStudentSelected));
                }
            }
        }

        public bool IsStudentSelected => !IsAdminSelected;

        // View Mode: Login vs Register
        private bool _isRegisterMode;
        public bool IsRegisterMode
        {
            get => _isRegisterMode;
            set
            {
                SetProperty(ref _isRegisterMode, value);
                OnPropertyChanged(nameof(IsLoginMode));
                ErrorMessage = string.Empty;
                SuccessMessage = string.Empty;
            }
        }

        public bool IsLoginMode => !IsRegisterMode;

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

        private string _regEmergencyContactName = string.Empty;
        public string RegEmergencyContactName
        {
            get => _regEmergencyContactName;
            set => SetProperty(ref _regEmergencyContactName, value);
        }

        private string _regEmergencyContactPhone = string.Empty;
        public string RegEmergencyContactPhone
        {
            get => _regEmergencyContactPhone;
            set => SetProperty(ref _regEmergencyContactPhone, value);
        }

        public System.Collections.ObjectModel.ObservableCollection<string> RegGenderOptions { get; } = new() { "Nam", "Nữ" };

        private string _regGender = "Nam";
        public string RegGender
        {
            get => _regGender;
            set => SetProperty(ref _regGender, value);
        }

        private string _regAddress = string.Empty;
        public string RegAddress
        {
            get => _regAddress;
            set => SetProperty(ref _regAddress, value);
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
                IsAdminSelected = true;
                IsRegisterMode = false;
                Username = "manager01";
                Password = "HASH_MANAGER_01";
            });

            SelectStudentCommand = new RelayCommand(() =>
            {
                IsAdminSelected = false;
                IsRegisterMode = false;
                Username = "student1";
                Password = "HASH_STUDENT_1";
            });
        }

        private async Task ExecuteLoginAsync()
        {
            ErrorMessage = string.Empty;
            SuccessMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(Username))
            {
                ErrorMessage = "Vui lòng nhập tên đăng nhập.";
                _dialogService.ShowError(ErrorMessage, "Đăng nhập thất bại");
                return;
            }

            if (string.IsNullOrWhiteSpace(Password))
            {
                ErrorMessage = "Vui lòng nhập mật khẩu.";
                _dialogService.ShowError(ErrorMessage, "Đăng nhập thất bại");
                return;
            }

            IsBusy = true;

            try
            {
                var result = await _authService.LoginAsync(Username, Password);
                IsBusy = false;

                if (result.IsSuccess && result.Data != null)
                {
                    LoginSuccess?.Invoke(result.Data);
                }
                else
                {
                    ErrorMessage = result.Message;
                    _dialogService.ShowError(ErrorMessage, "Đăng nhập thất bại");
                }
            }
            catch (System.Exception ex)
            {
                IsBusy = false;
                ErrorMessage = $"Lỗi kết nối cơ sở dữ liệu: {ex.Message}";
                _dialogService.ShowError(ErrorMessage, "Lỗi kết nối DB");
            }
        }

        private async Task ExecuteRegisterAsync()
        {
            ErrorMessage = string.Empty;
            SuccessMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(RegUsername))
            {
                ErrorMessage = "Vui lòng nhập tên đăng nhập.";
                _dialogService.ShowError(ErrorMessage, "Lỗi đăng ký");
                return;
            }

            if (RegUsername.Trim().Length < 3)
            {
                ErrorMessage = "Tên đăng nhập phải có ít nhất 3 ký tự.";
                _dialogService.ShowError(ErrorMessage, "Lỗi đăng ký");
                return;
            }

            if (string.IsNullOrWhiteSpace(RegStudentCode))
            {
                ErrorMessage = "Vui lòng nhập mã sinh viên.";
                _dialogService.ShowError(ErrorMessage, "Lỗi đăng ký");
                return;
            }

            if (string.IsNullOrWhiteSpace(RegFullName))
            {
                ErrorMessage = "Vui lòng nhập họ và tên sinh viên.";
                _dialogService.ShowError(ErrorMessage, "Lỗi đăng ký");
                return;
            }

            if (string.IsNullOrWhiteSpace(RegEmail))
            {
                ErrorMessage = "Vui lòng nhập địa chỉ Email.";
                _dialogService.ShowError(ErrorMessage, "Lỗi đăng ký");
                return;
            }

            if (!RegEmail.Contains("@") || !RegEmail.Contains("."))
            {
                ErrorMessage = "Địa chỉ Email không đúng định dạng (VD: student@fpt.edu.vn).";
                _dialogService.ShowError(ErrorMessage, "Lỗi đăng ký");
                return;
            }

            if (string.IsNullOrWhiteSpace(RegPassword))
            {
                ErrorMessage = "Vui lòng nhập mật khẩu.";
                _dialogService.ShowError(ErrorMessage, "Lỗi đăng ký");
                return;
            }

            if (RegPassword.Length < 6)
            {
                ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự.";
                _dialogService.ShowError(ErrorMessage, "Lỗi đăng ký");
                return;
            }

            if (RegPassword != RegConfirmPassword)
            {
                ErrorMessage = "Mật khẩu xác nhận không trùng khớp với mật khẩu đã nhập.";
                _dialogService.ShowError(ErrorMessage, "Lỗi đăng ký");
                return;
            }

            if (!string.IsNullOrWhiteSpace(RegPhone) && !System.Text.RegularExpressions.Regex.IsMatch(RegPhone.Trim(), @"^\d{9,11}$"))
            {
                ErrorMessage = "Số điện thoại phải chứa từ 9 đến 11 chữ số.";
                _dialogService.ShowError(ErrorMessage, "Lỗi đăng ký");
                return;
            }

            if (!string.IsNullOrWhiteSpace(RegEmergencyContactPhone) && !System.Text.RegularExpressions.Regex.IsMatch(RegEmergencyContactPhone.Trim(), @"^\d{9,11}$"))
            {
                ErrorMessage = "SĐT liên hệ khẩn cấp phải chứa từ 9 đến 11 chữ số.";
                _dialogService.ShowError(ErrorMessage, "Lỗi đăng ký");
                return;
            }

            if (!string.IsNullOrWhiteSpace(RegPhone) && !string.IsNullOrWhiteSpace(RegEmergencyContactPhone) &&
                string.Equals(RegPhone.Trim(), RegEmergencyContactPhone.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                ErrorMessage = "Số điện thoại cá nhân không được trùng với số điện thoại liên hệ khẩn cấp.";
                _dialogService.ShowError(ErrorMessage, "Lỗi đăng ký");
                return;
            }

            if (string.IsNullOrWhiteSpace(RegAddress))
            {
                ErrorMessage = "Vui lòng nhập địa chỉ thường trú.";
                _dialogService.ShowError(ErrorMessage, "Lỗi đăng ký");
                return;
            }

            string dbGender = string.Equals(RegGender, "Nữ", StringComparison.OrdinalIgnoreCase) ? "Female" : "Male";

            IsBusy = true;
            var result = await _authService.RegisterStudentAsync(
                RegUsername, RegPassword, RegEmail, RegPhone, 
                RegFullName, RegStudentCode, RegMajor, RegClassName,
                RegEmergencyContactName, RegEmergencyContactPhone,
                dbGender, RegAddress);
            IsBusy = false;

            if (result.IsSuccess)
            {
                SuccessMessage = result.Message;
                _dialogService.ShowInfo(result.Message, "Thành công");
                
                // Pre-fill login with new account and switch to Login tab
                Username = RegUsername;
                Password = RegPassword;
                IsAdminSelected = false;
                IsRegisterMode = false;
            }
            else
            {
                ErrorMessage = result.Message;
                _dialogService.ShowError(ErrorMessage, "Lỗi đăng ký");
            }
        }
    }
}
