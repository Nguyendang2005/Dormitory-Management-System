using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using DormCare.Business.DTOs;
using DormCare.Business.Services;
using DormCare.WPF.Commands;

namespace DormCare.WPF.ViewModels
{
    public class BedOption
    {
        public BedDto Bed { get; }
        public string Display => $"{Bed.BuildingName} — Phòng {Bed.RoomNumber} — Giường {Bed.BedNumber} ({Bed.BedCode})";

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

        private ObservableCollection<BedOption> _availableBeds = new();
        public ObservableCollection<BedOption> AvailableBeds
        {
            get => _availableBeds;
            set => SetProperty(ref _availableBeds, value);
        }

        private BedOption? _selectedBed;
        public BedOption? SelectedBed
        {
            get => _selectedBed;
            set => SetProperty(ref _selectedBed, value);
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
            var beds = await _studentService.GetAvailableBedsAsync();
            AvailableBeds = new ObservableCollection<BedOption>(beds.Select(b => new BedOption(b)));
            IsBusy = false;

            if (AvailableBeds.Count == 0)
            {
                ErrorMessage = "Hiện không còn giường trống nào trong ký túc xá.";
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
