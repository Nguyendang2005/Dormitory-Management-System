using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Input;
using DormCare.Business.DTOs;
using DormCare.Business.Services;
using DormCare.Domain.Entities;
using DormCare.WPF.Commands;

namespace DormCare.WPF.ViewModels
{
    public class StudentDetailViewModel : BaseViewModel
    {
        private readonly StudentService _studentService;
        private readonly StudentDto? _originalStudentDto;

        public bool IsEditMode => _originalStudentDto != null;
        public bool IsAddMode => !IsEditMode;

        private string _studentCode = string.Empty;
        public string StudentCode
        {
            get => _studentCode;
            set => SetProperty(ref _studentCode, value);
        }

        private string _fullName = string.Empty;
        public string FullName
        {
            get => _fullName;
            set => SetProperty(ref _fullName, value);
        }

        private DateTime _dateOfBirth = DateTime.Today.AddYears(-18);
        public DateTime DateOfBirth
        {
            get => _dateOfBirth;
            set => SetProperty(ref _dateOfBirth, value);
        }

        private string _selectedGender = "Male";
        public string SelectedGender
        {
            get => _selectedGender;
            set => SetProperty(ref _selectedGender, value);
        }

        private string _email = string.Empty;
        public string Email
        {
            get => _email;
            set => SetProperty(ref _email, value);
        }

        private string _phone = string.Empty;
        public string Phone
        {
            get => _phone;
            set => SetProperty(ref _phone, value);
        }

        private string _major = "Software Engineering";
        public string Major
        {
            get => _major;
            set => SetProperty(ref _major, value);
        }

        private string _className = "SE1801";
        public string ClassName
        {
            get => _className;
            set => SetProperty(ref _className, value);
        }

        private string _campus = "FPT University Da Nang";
        public string Campus
        {
            get => _campus;
            set => SetProperty(ref _campus, value);
        }

        private string? _emergencyContactName;
        public string? EmergencyContactName
        {
            get => _emergencyContactName;
            set => SetProperty(ref _emergencyContactName, value);
        }

        private string? _emergencyContactPhone;
        public string? EmergencyContactPhone
        {
            get => _emergencyContactPhone;
            set => SetProperty(ref _emergencyContactPhone, value);
        }

        private string? _address;
        public string? Address
        {
            get => _address;
            set => SetProperty(ref _address, value);
        }

        private string _selectedStatus = "Active";
        public string SelectedStatus
        {
            get => _selectedStatus;
            set => SetProperty(ref _selectedStatus, value);
        }

        private string _errorMessage = string.Empty;
        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        public List<string> Genders { get; } = new() { "Male", "Female", "Other" };
        public List<string> Statuses { get; } = new() { "Active", "Inactive", "Graduated", "Suspended" };

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        public event Action<bool>? RequestClose;

        public StudentDetailViewModel(StudentService studentService, StudentDto? studentDto = null)
        {
            _studentService = studentService;
            _originalStudentDto = studentDto;

            if (IsEditMode && studentDto != null)
            {
                Title = "✏️ Chỉnh Sửa Thông Tin Sinh Viên";
                StudentCode = studentDto.StudentCode;
                FullName = studentDto.FullName;
                SelectedGender = studentDto.Gender;
                Email = studentDto.Email;
                Phone = studentDto.PhoneNumber;
                Major = studentDto.Major;
                ClassName = studentDto.ClassName;
                _ = InitializeEditFieldsAsync(studentDto.Id);
            }
            else
            {
                Title = "➕ Thêm Sinh Viên Mới";
            }

            SaveCommand = new AsyncRelayCommand(ExecuteSaveAsync);
            CancelCommand = new RelayCommand(() => RequestClose?.Invoke(false));
        }

        private async Task InitializeEditFieldsAsync(int studentId)
        {
            try
            {
                var dbStudent = await _studentService.GetStudentByUserIdAsync(_originalStudentDto!.UserId);
                if (dbStudent != null)
                {
                    DateOfBirth = dbStudent.DateOfBirth;
                    Campus = dbStudent.Campus;
                    EmergencyContactName = dbStudent.EmergencyContactName;
                    EmergencyContactPhone = dbStudent.EmergencyContactPhone;
                    Address = dbStudent.Address;
                    SelectedStatus = dbStudent.Status;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Không thể tải thông tin chi tiết: {ex.Message}";
            }
        }

        private async Task ExecuteSaveAsync()
        {
            ErrorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(StudentCode))
            {
                ErrorMessage = "Vui lòng nhập Mã sinh viên.";
                return;
            }
            if (string.IsNullOrWhiteSpace(FullName))
            {
                ErrorMessage = "Vui lòng nhập Họ và tên sinh viên.";
                return;
            }
            if (string.IsNullOrWhiteSpace(Email))
            {
                ErrorMessage = "Vui lòng nhập Email.";
                return;
            }
            if (string.IsNullOrWhiteSpace(Phone))
            {
                ErrorMessage = "Vui lòng nhập Số điện thoại.";
                return;
            }

            IsBusy = true;

            try
            {
                if (IsEditMode)
                {
                    var dto = new StudentDto
                    {
                        Id = _originalStudentDto!.Id,
                        StudentCode = StudentCode.Trim(),
                        FullName = FullName.Trim(),
                        DateOfBirth = DateOfBirth,
                        Gender = SelectedGender,
                        Email = Email.Trim(),
                        PhoneNumber = Phone.Trim(),
                        Major = Major.Trim(),
                        ClassName = ClassName.Trim(),
                        Campus = Campus.Trim(),
                        EmergencyContactName = EmergencyContactName?.Trim(),
                        EmergencyContactPhone = EmergencyContactPhone?.Trim(),
                        Address = Address?.Trim(),
                        Status = SelectedStatus
                    };

                    var result = await _studentService.UpdateStudentAsync(dto);
                    IsBusy = false;

                    if (result.IsSuccess)
                    {
                        RequestClose?.Invoke(true);
                    }
                    else
                    {
                        ErrorMessage = result.Message;
                    }
                }
                else
                {
                    var dto = new StudentDto
                    {
                        StudentCode = StudentCode.Trim(),
                        FullName = FullName.Trim(),
                        DateOfBirth = DateOfBirth,
                        Gender = SelectedGender,
                        Email = Email.Trim(),
                        PhoneNumber = Phone.Trim(),
                        Major = Major.Trim(),
                        ClassName = ClassName.Trim(),
                        Campus = Campus.Trim(),
                        EmergencyContactName = EmergencyContactName?.Trim(),
                        EmergencyContactPhone = EmergencyContactPhone?.Trim(),
                        Address = Address?.Trim(),
                        Status = "Active"
                    };

                    var result = await _studentService.CreateStudentAsync(dto);
                    IsBusy = false;

                    if (result.IsSuccess)
                    {
                        RequestClose?.Invoke(true);
                    }
                    else
                    {
                        ErrorMessage = result.Message;
                    }
                }
            }
            catch (Exception ex)
            {
                IsBusy = false;
                ErrorMessage = $"Lỗi hệ thống: {ex.Message}";
            }
        }
    }
}
