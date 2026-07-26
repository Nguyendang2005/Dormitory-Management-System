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

        private string _username = "admin";
        public string Username
        {
            get => _username;
            set => SetProperty(ref _username, value);
        }

        private string _password = "admin123";
        public string Password
        {
            get => _password;
            set => SetProperty(ref _password, value);
        }

        private string _errorMessage = string.Empty;
        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        public ICommand LoginCommand { get; }
        public ICommand SelectAdminCommand { get; }
        public ICommand SelectStudentCommand { get; }

        public event System.Action<User>? LoginSuccess;

        public LoginViewModel(AuthService authService, DialogService dialogService)
        {
            Title = "Đăng nhập — DormCare";
            _authService = authService;
            _dialogService = dialogService;

            LoginCommand = new AsyncRelayCommand(ExecuteLoginAsync);
            SelectAdminCommand = new RelayCommand(() =>
            {
                Username = "admin";
                Password = "admin123";
            });
            SelectStudentCommand = new RelayCommand(() =>
            {
                Username = "student1";
                Password = "student123";
            });
        }

        private async Task ExecuteLoginAsync()
        {
            ErrorMessage = string.Empty;
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
                _dialogService.ShowError(result.Message, "Đăng nhập thất bại");
            }
        }
    }
}
