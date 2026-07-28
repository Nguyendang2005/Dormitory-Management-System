using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using DormCare.Business.DTOs;
using DormCare.Business.Services;
using DormCare.Domain.Entities;
using DormCare.WPF.Commands;

namespace DormCare.WPF.ViewModels
{
    public class BedOption
    {
        public BedDto Bed { get; }
        public string Display => $"Giường {Bed.BedNumber} ({Bed.BedCode})";

        public BedOption(BedDto bed)
        {
            Bed = bed;
        }
    }

    public class CheckInViewModel : BaseViewModel
    {
        private readonly StudentService _studentService;
        private readonly int _managerId;

        public StudentDto Student { get; }
        public string HeaderText => $"✅ Check-in: {Student.FullName}";
        public string SubHeaderText => $"Mã SV: {Student.StudentCode} · Giới tính: {Student.Gender} · Chọn giường trống để xếp phòng";

        private string _buildingName = "N/A";
        public string BuildingName
        {
            get => _buildingName;
            set => SetProperty(ref _buildingName, value);
        }

        private string _roomNumber = "Chưa đăng ký";
        public string RoomNumber
        {
            get => _roomNumber;
            set => SetProperty(ref _roomNumber, value);
        }

        private string _bedCode = "Chưa gán";
        public string BedCode
        {
            get => _bedCode;
            set => SetProperty(ref _bedCode, value);
        }

        private BedOption? _selectedBed;
        public BedOption? SelectedBed
        {
            get => _selectedBed;
            set
            {
                if (SetProperty(ref _selectedBed, value))
                {
                    System.Windows.Input.CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        private string _note = string.Empty;
        public string Note
        {
            get => _note;
            set => SetProperty(ref _note, value);
        }

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

        public ICommand ConfirmCommand { get; }
        public ICommand CancelCommand { get; }

        /// <summary>Raised when the dialog should close. Argument: true if checked in successfully.</summary>
        public event Action<bool>? RequestClose;

        public string ResultMessage { get; private set; } = string.Empty;

        public CheckInViewModel(StudentService studentService, StudentDto student, int managerId)
        {
            Title = "Check-in Sinh Viên";
            _studentService = studentService;
            _managerId = managerId;
            Student = student;

            ConfirmCommand = new AsyncRelayCommand(ConfirmAsync, () => SelectedBed != null);
            CancelCommand = new RelayCommand(() => RequestClose?.Invoke(false));

            _ = LoadBedsAsync();
        }

        private async Task LoadBedsAsync()
        {
            IsBusy = true;
            ErrorMessage = string.Empty;

            try
            {
                var app = await _studentService.GetLatestApplicationByStudentIdAsync(Student.Id);
                if (app == null)
                {
                    ErrorMessage = "Sinh viên chưa có đơn đăng ký phòng hoặc đơn đã bị hủy/từ chối.";
                    SelectedBed = null;
                    return;
                }

                if (app.PreferredBed == null)
                {
                    ErrorMessage = "Đơn đăng ký phòng chưa được xếp giường.";
                    SelectedBed = null;
                    return;
                }

                BuildingName = app.Room.Building.BuildingName;
                RoomNumber = app.Room.RoomNumber;
                BedCode = $"Giường {app.PreferredBed.BedNumber} ({app.PreferredBed.BedCode})";

                var bedDto = new BedDto
                {
                    BedId = app.PreferredBed.BedId,
                    RoomId = app.RoomId,
                    RoomNumber = app.Room.RoomNumber,
                    BuildingName = app.Room.Building.BuildingName,
                    BedNumber = app.PreferredBed.BedNumber,
                    BedCode = app.PreferredBed.BedCode,
                    Status = app.PreferredBed.Status,
                    Description = app.PreferredBed.Description ?? string.Empty
                };

                SelectedBed = new BedOption(bedDto);
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Lỗi tải dữ liệu check-in: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task ConfirmAsync()
        {
            if (SelectedBed == null) return;

            IsBusy = true;
            ErrorMessage = string.Empty;

            var result = await _studentService.CheckInAsync(Student.Id, SelectedBed.Bed.BedId, _managerId, Note);
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
