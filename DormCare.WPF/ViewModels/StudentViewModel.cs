using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using DormCare.Business.DTOs;
using DormCare.Business.Services;
using DormCare.DataAccess.Data;
using DormCare.Domain.Entities;
using DormCare.WPF.Commands;
using DormCare.WPF.Services;
using DormCare.WPF.Views.Manager;

namespace DormCare.WPF.ViewModels
{
    public class StudentViewModel : BaseViewModel
    {
        private readonly StudentService _studentService;
        private readonly DormCareDbContext _context;
        private readonly DialogService _dialogService;
        private readonly User _currentUser;

        private ObservableCollection<StudentDto> _allStudents = new();

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
            set => SetProperty(ref _selectedStudent, value);
        }

        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    ApplyFilters();
                }
            }
        }

        private ObservableCollection<RoomDto> _rooms = new();
        public ObservableCollection<RoomDto> Rooms
        {
            get => _rooms;
            set => SetProperty(ref _rooms, value);
        }

        private RoomDto? _selectedRoomFilter;
        public RoomDto? SelectedRoomFilter
        {
            get => _selectedRoomFilter;
            set
            {
                if (SetProperty(ref _selectedRoomFilter, value))
                {
                    ApplyFilters();
                }
            }
        }

        public ICommand RefreshCommand { get; }
        public ICommand AddStudentCommand { get; }
        public ICommand EditStudentCommand { get; }
        public ICommand DeleteStudentCommand { get; }
        public ICommand CheckInCommand { get; }
        public ICommand CheckOutCommand { get; }

        public StudentViewModel(
            StudentService studentService,
            DormCareDbContext context,
            DialogService dialogService,
            User currentUser)
        {
            Title = "Quản lý Sinh viên";
            _studentService = studentService;
            _context = context;
            _dialogService = dialogService;
            _currentUser = currentUser;

            RefreshCommand = new AsyncRelayCommand(LoadStudentsAsync);
            AddStudentCommand = new RelayCommand(ExecuteAddStudent);
            EditStudentCommand = new RelayCommand(ExecuteEditStudent);
            DeleteStudentCommand = new AsyncRelayCommand(ExecuteDeleteStudentAsync);
            CheckInCommand = new RelayCommand(ExecuteCheckIn);
            CheckOutCommand = new AsyncRelayCommand(ExecuteCheckOutAsync);

            _ = LoadStudentsAsync();
        }

        public async Task LoadStudentsAsync()
        {
            IsBusy = true;
            try
            {
                var dtos = await _studentService.GetAllStudentsAsync();
                _allStudents = new ObservableCollection<StudentDto>(dtos);

                // Load rooms for filter combobox
                var roomService = App.ServiceProvider.GetService(typeof(RoomService)) as RoomService;
                if (roomService != null)
                {
                    var roomList = await roomService.GetAllRoomsAsync();
                    var list = new List<RoomDto> { new RoomDto { RoomNumber = "Tất cả" } };
                    list.AddRange(roomList.OrderBy(r => r.RoomNumber));
                    Rooms = new ObservableCollection<RoomDto>(list);
                    SelectedRoomFilter = Rooms.FirstOrDefault();
                }

                ApplyFilters();
            }
            catch (Exception ex)
            {
                _dialogService.ShowError($"Lỗi khi tải danh sách sinh viên: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void ApplyFilters()
        {
            var query = _allStudents.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                query = query.Where(s => s.FullName.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                                         s.StudentCode.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                                         s.Major.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                                         s.ClassName.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
            }

            if (SelectedRoomFilter != null && SelectedRoomFilter.RoomNumber != "Tất cả")
            {
                query = query.Where(s => s.RoomNumber == SelectedRoomFilter.RoomNumber);
            }

            Students = new ObservableCollection<StudentDto>(query);
        }

        private void ExecuteAddStudent()
        {
            var detailVm = new StudentDetailViewModel(_studentService);
            var dialog = new StudentDetailWindow
            {
                DataContext = detailVm,
                Owner = System.Windows.Application.Current?.MainWindow
            };

            detailVm.RequestClose += async (success) =>
            {
                dialog.Close();
                if (success)
                {
                    _dialogService.ShowInformation("Thêm sinh viên mới thành công!");
                    await LoadStudentsAsync();
                }
            };

            dialog.ShowDialog();
        }

        private void ExecuteEditStudent()
        {
            if (SelectedStudent == null)
            {
                _dialogService.ShowInformation("Vui lòng chọn sinh viên cần chỉnh sửa.", "Thông báo");
                return;
            }

            var detailVm = new StudentDetailViewModel(_studentService, SelectedStudent);
            var dialog = new StudentDetailWindow
            {
                DataContext = detailVm,
                Owner = System.Windows.Application.Current?.MainWindow
            };

            detailVm.RequestClose += async (success) =>
            {
                dialog.Close();
                if (success)
                {
                    _dialogService.ShowInformation("Cập nhật thông tin sinh viên thành công!");
                    await LoadStudentsAsync();
                }
            };

            dialog.ShowDialog();
        }

        private async Task ExecuteDeleteStudentAsync()
        {
            if (SelectedStudent == null)
            {
                _dialogService.ShowInformation("Vui lòng chọn sinh viên cần xóa.", "Thông báo");
                return;
            }

            var confirmed = _dialogService.ShowConfirmation($"Bạn có chắc chắn muốn xóa sinh viên {SelectedStudent.FullName} (Mã số: {SelectedStudent.StudentCode})?", "Xác nhận xóa");
            if (!confirmed) return;

            IsBusy = true;
            try
            {
                var result = await _studentService.DeleteStudentAsync(SelectedStudent.Id);
                IsBusy = false;

                if (result.IsSuccess)
                {
                    _dialogService.ShowInformation("Xóa sinh viên thành công!");
                    await LoadStudentsAsync();
                }
                else
                {
                    _dialogService.ShowError(result.Message, "Lỗi");
                }
            }
            catch (Exception ex)
            {
                IsBusy = false;
                _dialogService.ShowError($"Lỗi khi xóa sinh viên: {ex.Message}", "Lỗi");
            }
        }

        private void ExecuteCheckIn()
        {
            if (SelectedStudent == null)
            {
                _dialogService.ShowInformation("Vui lòng chọn sinh viên cần Check-in.", "Thông báo");
                return;
            }

            if (SelectedStudent.RoomNumber != "Chưa nhận phòng")
            {
                _dialogService.ShowInformation("Sinh viên này đã có phòng ở. Vui lòng Check-out trước.", "Thông báo");
                return;
            }

            var checkInVm = new CheckInViewModel(_studentService, _context, _dialogService, SelectedStudent, _currentUser);
            var dialog = new CheckInWindow
            {
                DataContext = checkInVm,
                Owner = System.Windows.Application.Current?.MainWindow
            };

            checkInVm.RequestClose += async (success) =>
            {
                dialog.Close();
                if (success)
                {
                    _dialogService.ShowInformation("Nhận phòng (Check-in) thành công!");
                    await LoadStudentsAsync();
                }
            };

            dialog.ShowDialog();
        }

        private async Task ExecuteCheckOutAsync()
        {
            if (SelectedStudent == null)
            {
                _dialogService.ShowInformation("Vui lòng chọn sinh viên cần Check-out.", "Thông báo");
                return;
            }

            if (SelectedStudent.RoomNumber == "Chưa nhận phòng")
            {
                _dialogService.ShowInformation("Sinh viên này hiện tại chưa nhận phòng.", "Thông báo");
                return;
            }

            var confirmed = _dialogService.ShowConfirmation($"Bạn có chắc chắn muốn Check-out (trả phòng) cho sinh viên {SelectedStudent.FullName} ra khỏi phòng {SelectedStudent.RoomNumber}?", "Xác nhận Check-out");
            if (!confirmed) return;

            IsBusy = true;
            try
            {
                var result = await _studentService.CheckOutStudentAsync(SelectedStudent.Id);
                IsBusy = false;

                if (result.IsSuccess)
                {
                    _dialogService.ShowInformation("Trả phòng (Check-out) thành công!");
                    await LoadStudentsAsync();
                }
                else
                {
                    _dialogService.ShowError(result.Message, "Lỗi");
                }
            }
            catch (Exception ex)
            {
                IsBusy = false;
                _dialogService.ShowError($"Lỗi khi Check-out: {ex.Message}", "Lỗi");
            }
        }
    }
}
