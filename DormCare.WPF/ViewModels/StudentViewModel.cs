using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using DormCare.Business.DTOs;
using DormCare.Business.Services;
using DormCare.Domain.Entities;
using DormCare.WPF.Commands;
using DormCare.WPF.Services;
using DormCare.WPF.Views.Manager;

namespace DormCare.WPF.ViewModels
{
    public class StudentViewModel : BaseViewModel
    {
        private const string FilterAll = "Tất cả phòng";
        private const string FilterNoRoom = "Chưa nhận phòng";

        private readonly StudentService _studentService;
        private readonly DialogService _dialogService;
        private readonly User? _currentUser;

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

        private ObservableCollection<string> _roomFilterOptions = new();
        public ObservableCollection<string> RoomFilterOptions
        {
            get => _roomFilterOptions;
            set => SetProperty(ref _roomFilterOptions, value);
        }

        private string _selectedRoomFilter = FilterAll;
        public string SelectedRoomFilter
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

        public ObservableCollection<string> GenderFilterOptions { get; } = new() { "Tất cả giới tính", "Nam", "Nữ" };
        private string _selectedGenderFilter = "Tất cả giới tính";
        public string SelectedGenderFilter
        {
            get => _selectedGenderFilter;
            set
            {
                if (SetProperty(ref _selectedGenderFilter, value))
                {
                    ApplyFilters();
                }
            }
        }

        public ObservableCollection<string> StatusFilterOptions { get; } = new() { "Tất cả trạng thái", "Active", "Graduated", "Suspended", "CheckedOut" };
        private string _selectedStatusFilter = "Tất cả trạng thái";
        public string SelectedStatusFilter
        {
            get => _selectedStatusFilter;
            set
            {
                if (SetProperty(ref _selectedStatusFilter, value))
                {
                    ApplyFilters();
                }
            }
        }

        private string _summaryText = string.Empty;
        public string SummaryText
        {
            get => _summaryText;
            set => SetProperty(ref _summaryText, value);
        }

        public ICommand RefreshCommand { get; }
        public ICommand ClearFiltersCommand { get; }
        public ICommand AddCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand CheckInCommand { get; }
        public ICommand CheckOutCommand { get; }

        public StudentViewModel(StudentService studentService, DialogService dialogService, User? currentUser = null)
        {
            Title = "Quản lý Sinh viên";
            _studentService = studentService;
            _dialogService = dialogService;
            _currentUser = currentUser;

            RefreshCommand = new AsyncRelayCommand(LoadStudentsAsync);
            ClearFiltersCommand = new RelayCommand(_ => ExecuteClearFilters());
            AddCommand = new RelayCommand(ExecuteAdd);
            EditCommand = new RelayCommand(ExecuteEdit, () => SelectedStudent != null);
            DeleteCommand = new AsyncRelayCommand(ExecuteDeleteAsync, () => SelectedStudent != null);
            CheckInCommand = new RelayCommand(ExecuteCheckIn, () => SelectedStudent is { HasRoom: false });
            CheckOutCommand = new AsyncRelayCommand(ExecuteCheckOutAsync, () => SelectedStudent is { HasRoom: true });

            _ = LoadStudentsAsync();
        }

        private void ExecuteClearFilters()
        {
            SearchText = string.Empty;
            SelectedRoomFilter = FilterAll;
            SelectedGenderFilter = "Tất cả giới tính";
            SelectedStatusFilter = "Tất cả trạng thái";
        }

        public async Task LoadStudentsAsync()
        {
            IsBusy = true;
            try
            {
                var dtos = await _studentService.GetAllStudentsAsync();
                _allStudents = new ObservableCollection<StudentDto>(dtos);
                RebuildRoomFilterOptions();
                ApplyFilters();
            }
            catch (Exception ex)
            {
                _dialogService.ShowError($"Không thể tải danh sách sinh viên:\n{ex.Message}", "Lỗi tải dữ liệu");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void RebuildRoomFilterOptions()
        {
            var rooms = _allStudents
                .Where(s => s.HasRoom)
                .Select(s => s.RoomDisplay)
                .Distinct()
                .OrderBy(r => r)
                .ToList();

            var options = new ObservableCollection<string> { FilterAll, FilterNoRoom };
            foreach (var room in rooms)
            {
                options.Add(room);
            }

            var current = SelectedRoomFilter;
            RoomFilterOptions = options;
            _selectedRoomFilter = options.Contains(current) ? current : FilterAll;
            OnPropertyChanged(nameof(SelectedRoomFilter));
        }

        private void ApplyFilters()
        {
            var query = _allStudents.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                query = query.Where(s => s.FullName.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                                         s.StudentCode.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                                         s.Email.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                                         s.PhoneNumber.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                                         s.Major.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                                         s.ClassName.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
            }

            if (SelectedRoomFilter == FilterNoRoom)
            {
                query = query.Where(s => !s.HasRoom);
            }
            else if (SelectedRoomFilter != FilterAll)
            {
                query = query.Where(s => s.RoomDisplay == SelectedRoomFilter);
            }

            if (SelectedGenderFilter != "Tất cả giới tính")
            {
                string dbGender = string.Equals(SelectedGenderFilter, "Nam", StringComparison.OrdinalIgnoreCase) ? "Male" : "Female";
                query = query.Where(s => string.Equals(s.Gender, dbGender, StringComparison.OrdinalIgnoreCase));
            }

            if (SelectedStatusFilter != "Tất cả trạng thái")
            {
                query = query.Where(s => string.Equals(s.Status, SelectedStatusFilter, StringComparison.OrdinalIgnoreCase));
            }

            Students = new ObservableCollection<StudentDto>(query);
            SummaryText = $"Hiển thị {Students.Count}/{_allStudents.Count} sinh viên · " +
                          $"Đang ở KTX: {_allStudents.Count(s => s.HasRoom)} · " +
                          $"Chưa nhận phòng: {_allStudents.Count(s => !s.HasRoom)}";
        }

        /* =====================================================
           ADD / EDIT / DELETE
           ===================================================== */

        private void ExecuteAdd()
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
                    _dialogService.ShowInformation("Thêm sinh viên mới thành công!", "Thành công");
                    await LoadStudentsAsync();
                }
            };

            dialog.ShowDialog();
        }

        private void ExecuteEdit()
        {
            if (SelectedStudent == null) return;

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
                    _dialogService.ShowInformation("Cập nhật thông tin sinh viên thành công!", "Thành công");
                    await LoadStudentsAsync();
                }
            };

            dialog.ShowDialog();
        }

        private async Task ExecuteDeleteAsync()
        {
            if (SelectedStudent == null) return;

            var confirmed = _dialogService.ShowConfirmation(
                $"Bạn có chắc muốn xóa sinh viên '{SelectedStudent.FullName}' ({SelectedStudent.StudentCode})?\n" +
                "Tài khoản đăng nhập của sinh viên cũng sẽ bị xóa.",
                "Xác nhận xóa");

            if (!confirmed) return;

            IsBusy = true;
            var result = await _studentService.DeleteStudentAsync(SelectedStudent.Id);
            IsBusy = false;

            if (result.IsSuccess)
            {
                _dialogService.ShowInformation(result.Message, "Đã xóa");
                await LoadStudentsAsync();
            }
            else
            {
                _dialogService.ShowError(result.Message, "Không thể xóa");
            }
        }

        /* =====================================================
           CHECK-IN / CHECK-OUT
           ===================================================== */

        private void ExecuteCheckIn()
        {
            if (SelectedStudent == null || _currentUser == null) return;

            var checkInVm = new CheckInViewModel(_studentService, SelectedStudent, _currentUser.UserId);
            var window = new CheckInWindow(checkInVm);

            if (window.ShowDialog() == true)
            {
                _dialogService.ShowInformation(checkInVm.ResultMessage, "Check-in thành công");
                _ = LoadStudentsAsync();
            }
        }

        private async Task ExecuteCheckOutAsync()
        {
            if (SelectedStudent == null || _currentUser == null) return;

            var confirmed = _dialogService.ShowConfirmation(
                $"Check-out sinh viên '{SelectedStudent.FullName}' khỏi {SelectedStudent.RoomDisplay} (giường {SelectedStudent.BedNumber})?",
                "Xác nhận Check-out");

            if (!confirmed) return;

            IsBusy = true;
            var result = await _studentService.CheckOutAsync(SelectedStudent.Id, _currentUser.UserId);
            IsBusy = false;

            if (result.IsSuccess)
            {
                _dialogService.ShowInformation(result.Message, "Check-out thành công");
                await LoadStudentsAsync();
            }
            else
            {
                _dialogService.ShowError(result.Message, "Không thể check-out");
            }
        }
    }
}
