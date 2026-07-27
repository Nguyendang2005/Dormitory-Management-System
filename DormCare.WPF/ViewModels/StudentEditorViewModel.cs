using System;
using System.Threading.Tasks;
using System.Windows.Input;
using DormCare.Business.DTOs;
using DormCare.Business.Services;
using DormCare.WPF.Commands;

namespace DormCare.WPF.ViewModels
{
    public class StudentEditorViewModel : BaseViewModel
    {
        private readonly StudentService _studentService;

        public StudentDto Draft { get; }
        public bool IsEditMode { get; }

        public string HeaderText => IsEditMode ? "✏️ Sửa Thông Tin Sinh Viên" : "➕ Thêm Sinh Viên Mới";
        public string SubHeaderText => IsEditMode
            ? $"Cập nhật hồ sơ của {Draft.FullName} ({Draft.StudentCode})"
            : "Hệ thống sẽ tự tạo tài khoản đăng nhập (mật khẩu mặc định: 123456)";
        public string SaveButtonText => IsEditMode ? "💾 Lưu Thay Đổi" : "➕ Thêm Sinh Viên";

        public string[] GenderOptions { get; } = { "Male", "Female", "Other" };
        public string[] StatusOptions { get; } = { "Active", "Inactive", "Graduated", "Suspended" };

        private string _errorMessage = string.Empty;
        public string ErrorMessage
        {
            get => _errorMessage;
            set
            {
                if (SetProperty(ref _errorMessage, value))
                {
                    OnPropertyChanged(nameof(HasError));
                }
            }
        }
        public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        /// <summary>Raised when the dialog should close. Argument: true if saved successfully.</summary>
        public event Action<bool>? RequestClose;

        public string ResultMessage { get; private set; } = string.Empty;

        public StudentEditorViewModel(StudentService studentService, StudentDto? existing = null)
        {
            _studentService = studentService;
            IsEditMode = existing != null;
            Title = IsEditMode ? "Sửa Thông Tin Sinh Viên" : "Thêm Sinh Viên";

            Draft = existing == null ? new StudentDto() : new StudentDto
            {
                Id = existing.Id,
                UserId = existing.UserId,
                StudentCode = existing.StudentCode,
                FullName = existing.FullName,
                DateOfBirth = existing.DateOfBirth,
                Major = existing.Major,
                ClassName = existing.ClassName,
                Gender = existing.Gender,
                Email = existing.Email,
                PhoneNumber = existing.PhoneNumber,
                Campus = existing.Campus,
                Address = existing.Address,
                EmergencyContactName = existing.EmergencyContactName,
                EmergencyContactPhone = existing.EmergencyContactPhone,
                Status = existing.Status
            };

            SaveCommand = new AsyncRelayCommand(SaveAsync);
            CancelCommand = new RelayCommand(() => RequestClose?.Invoke(false));
        }

        private async Task SaveAsync()
        {
            IsBusy = true;
            ErrorMessage = string.Empty;

            var result = IsEditMode
                ? await _studentService.UpdateStudentAsync(Draft)
                : await _studentService.CreateStudentAsync(Draft);

            IsBusy = false;

            if (result.IsSuccess)
            {
                ResultMessage = result.Message;
                RequestClose?.Invoke(true);
            }
            else
            {
                ErrorMessage = result.Message;
            }
        }
    }
}
