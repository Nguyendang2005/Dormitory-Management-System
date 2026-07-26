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
    public class CreateInvoiceViewModel : BaseViewModel
    {
        private readonly InvoiceService _invoiceService;
        private readonly StudentService _studentService;
        private readonly RoomService _roomService;

        public event Action<bool>? RequestClose;

        private ObservableCollection<StudentDto> _students = new();
        public ObservableCollection<StudentDto> Students
        {
            get => _students;
            set => SetProperty(ref _students, value);
        }

        private StudentDto? _selectedStudent;
        public StudentDto? SelectedStudent
        {
            get => _selectedStudent;
            set
            {
                if (SetProperty(ref _selectedStudent, value))
                {
                    OnStudentSelected();
                }
            }
        }

        private ObservableCollection<RoomDto> _rooms = new();
        public ObservableCollection<RoomDto> Rooms
        {
            get => _rooms;
            set => SetProperty(ref _rooms, value);
        }

        private RoomDto? _selectedRoom;
        public RoomDto? SelectedRoom
        {
            get => _selectedRoom;
            set
            {
                if (SetProperty(ref _selectedRoom, value))
                {
                    if (_selectedRoom != null)
                    {
                        RoomFee = _selectedRoom.MonthlyRent;
                    }
                }
            }
        }

        private int _month = DateTime.Today.Month;
        public int Month
        {
            get => _month;
            set => SetProperty(ref _month, value);
        }

        private int _year = DateTime.Today.Year;
        public int Year
        {
            get => _year;
            set => SetProperty(ref _year, value);
        }

        private decimal _roomFee;
        public decimal RoomFee
        {
            get => _roomFee;
            set
            {
                if (SetProperty(ref _roomFee, value))
                {
                    OnPropertyChanged(nameof(TotalAmount));
                }
            }
        }

        private decimal _electricityFee;
        public decimal ElectricityFee
        {
            get => _electricityFee;
            set
            {
                if (SetProperty(ref _electricityFee, value))
                {
                    OnPropertyChanged(nameof(TotalAmount));
                }
            }
        }

        private decimal _waterFee;
        public decimal WaterFee
        {
            get => _waterFee;
            set
            {
                if (SetProperty(ref _waterFee, value))
                {
                    OnPropertyChanged(nameof(TotalAmount));
                }
            }
        }

        private decimal _otherFee;
        public decimal OtherFee
        {
            get => _otherFee;
            set
            {
                if (SetProperty(ref _otherFee, value))
                {
                    OnPropertyChanged(nameof(TotalAmount));
                }
            }
        }

        private decimal _discountAmount;
        public decimal DiscountAmount
        {
            get => _discountAmount;
            set
            {
                if (SetProperty(ref _discountAmount, value))
                {
                    OnPropertyChanged(nameof(TotalAmount));
                }
            }
        }

        public decimal TotalAmount => _invoiceService.CalculateTotalFee(RoomFee, ElectricityFee, WaterFee + OtherFee, DiscountAmount);

        private DateTime _dueDate = DateTime.Today.AddDays(10);
        public DateTime DueDate
        {
            get => _dueDate;
            set => SetProperty(ref _dueDate, value);
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
            set => SetProperty(ref _errorMessage, value);
        }

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        public CreateInvoiceViewModel(
            InvoiceService invoiceService,
            StudentService studentService,
            RoomService roomService)
        {
            Title = "Tạo Hóa Đơn Mới";
            _invoiceService = invoiceService;
            _studentService = studentService;
            _roomService = roomService;

            SaveCommand = new AsyncRelayCommand(ExecuteSaveAsync);
            CancelCommand = new RelayCommand(() => RequestClose?.Invoke(false));

            _ = LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            IsBusy = true;
            try
            {
                var students = await _studentService.GetAllStudentsAsync();
                Students = new ObservableCollection<StudentDto>(students);

                var rooms = await _roomService.GetAllRoomsAsync();
                Rooms = new ObservableCollection<RoomDto>(rooms);
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Lỗi tải dữ liệu: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void OnStudentSelected()
        {
            if (SelectedStudent != null && !string.IsNullOrEmpty(SelectedStudent.RoomNumber) && SelectedStudent.RoomNumber != "Chưa nhận phòng")
            {
                SelectedRoom = Rooms.FirstOrDefault(r => r.RoomNumber.Equals(SelectedStudent.RoomNumber, StringComparison.OrdinalIgnoreCase));
            }
        }

        private async Task ExecuteSaveAsync()
        {
            if (SelectedStudent == null)
            {
                ErrorMessage = "Vui lòng chọn sinh viên.";
                return;
            }

            if (SelectedRoom == null)
            {
                ErrorMessage = "Sinh viên được chọn chưa được phân phòng ký túc xá.";
                return;
            }

            if (Month < 1 || Month > 12)
            {
                ErrorMessage = "Tháng không hợp lệ (1 - 12).";
                return;
            }

            ErrorMessage = string.Empty;
            IsBusy = true;

            var dto = new CreateInvoiceDto
            {
                StudentId = SelectedStudent.Id,
                RoomId = SelectedRoom.RoomId,
                BillingMonth = new DateTime(Year, Month, 1),
                RoomFee = RoomFee,
                ElectricityFee = ElectricityFee,
                WaterFee = WaterFee,
                OtherFee = OtherFee,
                DiscountAmount = DiscountAmount,
                DueDate = DueDate,
                Note = Note
            };

            var result = await _invoiceService.CreateInvoiceAsync(dto);
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
}
