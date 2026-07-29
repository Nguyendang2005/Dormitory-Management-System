using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using DormCare.Business.Services;
using DormCare.Domain.Entities;
using DormCare.WPF.Commands;
using DormCare.WPF.Services;

namespace DormCare.WPF.ViewModels
{
    public class MaintenanceViewModel : BaseViewModel
    {
        private readonly MaintenanceService _maintenanceService;
        private readonly RoomService _roomService;
        private readonly StudentService _studentService;
        private readonly DialogService _dialogService;
        private readonly User? _currentUser;

        private ObservableCollection<MaintenanceRequest> _allRequests = new();
        private ObservableCollection<DormCare.Business.DTOs.RoomDto> _rooms = new();
        public ObservableCollection<DormCare.Business.DTOs.RoomDto> Rooms
        {
            get => _rooms;
            set => SetProperty(ref _rooms, value);
        }

        private int? _selectedNewRoomId;
        public int? SelectedNewRoomId
        {
            get => _selectedNewRoomId;
            set => SetProperty(ref _selectedNewRoomId, value);
        }

        private ObservableCollection<MaintenanceRequest> _requests = new();
        public ObservableCollection<MaintenanceRequest> Requests
        {
            get => _requests;
            set => SetProperty(ref _requests, value);
        }

        private MaintenanceRequest? _selectedRequest;
        public MaintenanceRequest? SelectedRequest
        {
            get => _selectedRequest;
            set
            {
                if (SetProperty(ref _selectedRequest, value))
                {
                    OnPropertyChanged(nameof(IsRequestSelected));
                }
            }
        }

        public bool IsRequestSelected => SelectedRequest != null;

        private string _newTitle = "Điện (Đèn, Ổ cắm, Quạt)";
        public string NewTitle
        {
            get => _newTitle;
            set
            {
                if (SetProperty(ref _newTitle, value))
                {
                    ValidateInput();
                }
            }
        }

        private string _newPriority = "Medium";
        public string NewPriority
        {
            get => _newPriority;
            set => SetProperty(ref _newPriority, value);
        }

        private string _newDescription = string.Empty;
        public string NewDescription
        {
            get => _newDescription;
            set
            {
                if (SetProperty(ref _newDescription, value))
                {
                    ValidateInput();
                }
            }
        }

        private string _validationMessage = string.Empty;
        public string ValidationMessage
        {
            get => _validationMessage;
            set => SetProperty(ref _validationMessage, value);
        }

        private bool _hasValidationError;
        public bool HasValidationError
        {
            get => _hasValidationError;
            set => SetProperty(ref _hasValidationError, value);
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


        private string _selectedStatusFilter = "All";
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

        private string _selectedPriorityFilter = "All";
        public string SelectedPriorityFilter
        {
            get => _selectedPriorityFilter;
            set
            {
                if (SetProperty(ref _selectedPriorityFilter, value))
                {
                    ApplyFilters();
                }
            }
        }

        private string _selectedUpdateStatus = "InProgress";
        public string SelectedUpdateStatus
        {
            get => _selectedUpdateStatus;
            set => SetProperty(ref _selectedUpdateStatus, value);
        }

        private string _updateMessage = string.Empty;
        public string UpdateMessage
        {
            get => _updateMessage;
            set => SetProperty(ref _updateMessage, value);
        }

        public ICommand RefreshCommand { get; }
        public ICommand ClearFiltersCommand { get; }
        public ICommand CreateRequestCommand { get; }
        public ICommand CompleteRequestCommand { get; }
        public ICommand UpdateSelectedStatusCommand { get; }

        public ICommand CloseRequestCommand { get; }
        public ICommand MarkInProgressCommand { get; }

        public MaintenanceViewModel(MaintenanceService maintenanceService, RoomService roomService, StudentService studentService, DialogService dialogService, User? currentUser = null)
        {
            Title = "Quản lý Báo Cáo & Sửa Chữa";
            _maintenanceService = maintenanceService;
            _roomService = roomService;
            _studentService = studentService;
            _dialogService = dialogService;
            _currentUser = currentUser;

            RefreshCommand = new AsyncRelayCommand(LoadRequestsAsync);
            ClearFiltersCommand = new RelayCommand(_ => ExecuteClearFilters());
            CreateRequestCommand = new AsyncRelayCommand(ExecuteCreateRequestAsync);
            CompleteRequestCommand = new AsyncRelayCommand(ExecuteCompleteRequestAsync, () => SelectedRequest != null);
            UpdateSelectedStatusCommand = new AsyncRelayCommand(ExecuteUpdateSelectedStatusAsync, () => SelectedRequest != null);

            CloseRequestCommand = new AsyncRelayCommand(ExecuteCloseRequestAsync, () => SelectedRequest != null);
            MarkInProgressCommand = new AsyncRelayCommand(ExecuteMarkInProgressAsync, () => SelectedRequest != null);

            _ = LoadRequestsAsync();
            _ = LoadRoomsAsync();
        }

        public async Task LoadRoomsAsync()
        {
            var rooms = await _roomService.GetAllRoomsAsync();
            Rooms = new ObservableCollection<DormCare.Business.DTOs.RoomDto>(rooms.OrderBy(r => r.RoomNumber));
            if (Rooms.Any())
            {
                SelectedNewRoomId = Rooms.First().RoomId;
            }
        }

        private void ExecuteClearFilters()
        {
            SearchText = string.Empty;
            SelectedStatusFilter = "All";
            SelectedPriorityFilter = "All";
        }

        private bool _hasNoActiveRoom;
        public bool HasNoActiveRoom
        {
            get => _hasNoActiveRoom;
            set => SetProperty(ref _hasNoActiveRoom, value);
        }

        public async Task LoadRequestsAsync()
        {
            IsBusy = true;
            if (_currentUser != null && _currentUser.Role == "Student")
            {
                var student = await _studentService.GetStudentByUserIdAsync(_currentUser.UserId);
                if (student != null)
                {
                    var reqs = await _maintenanceService.GetRequestsByStudentIdAsync(student.StudentId);
                    _allRequests = new ObservableCollection<MaintenanceRequest>(reqs);

                    var activeAssignment = student.RoomAssignments.FirstOrDefault(ra => ra.Status == "Active");
                    if (activeAssignment == null || activeAssignment.Room == null)
                    {
                        HasNoActiveRoom = true;
                        ValidationMessage = "⚠️ Bạn chưa nhận phòng trong ký túc xá nên không thể gửi báo cáo sự cố.";
                        HasValidationError = true;
                    }
                    else
                    {
                        HasNoActiveRoom = false;
                    }
                }
            }
            else
            {
                var reqs = await _maintenanceService.GetAllRequestsAsync();
                _allRequests = new ObservableCollection<MaintenanceRequest>(reqs);
            }
            ApplyFilters();
            IsBusy = false;
        }

        private void ApplyFilters()
        {
            var query = _allRequests.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                query = query.Where(m => m.Title.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                                         m.RequestCode.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                                         (m.Student != null && m.Student.FullName.Contains(SearchText, StringComparison.OrdinalIgnoreCase)) ||
                                         (m.Room != null && m.Room.RoomNumber.Contains(SearchText, StringComparison.OrdinalIgnoreCase)));
            }


            if (!string.IsNullOrWhiteSpace(SelectedStatusFilter) && SelectedStatusFilter != "All")
            {
                query = query.Where(m => m.Status.Equals(SelectedStatusFilter, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(SelectedPriorityFilter) && SelectedPriorityFilter != "All")
            {
                query = query.Where(m => m.Priority.Equals(SelectedPriorityFilter, StringComparison.OrdinalIgnoreCase));
            }

            Requests = new ObservableCollection<MaintenanceRequest>(query);
        }

        private bool ValidateInput()
        {
            if (HasNoActiveRoom)
            {
                ValidationMessage = "⚠️ Bạn chưa nhận phòng trong ký túc xá nên không thể gửi báo cáo sự cố.";
                HasValidationError = true;
                return false;
            }

            if (string.IsNullOrWhiteSpace(NewTitle))
            {
                ValidationMessage = "⚠️ Tiêu đề sự cố không được để trống.";
                HasValidationError = true;
                return false;
            }

            if (string.IsNullOrWhiteSpace(NewDescription) || NewDescription.Length < 5)
            {
                ValidationMessage = "⚠️ Mô tả chi tiết phải có ít nhất 5 ký tự.";
                HasValidationError = true;
                return false;
            }

            ValidationMessage = string.Empty;
            HasValidationError = false;
            return true;
        }

        private async Task ExecuteCreateRequestAsync()
        {
            if (_currentUser != null && _currentUser.Role == "Student")
            {
                var student = await _studentService.GetStudentByUserIdAsync(_currentUser.UserId);
                var activeAssignment = student?.RoomAssignments.FirstOrDefault(ra => ra.Status == "Active");
                if (student == null || activeAssignment == null || activeAssignment.Room == null)
                {
                    HasNoActiveRoom = true;
                    ValidationMessage = "⚠️ Bạn chưa nhận phòng trong ký túc xá nên không thể gửi báo cáo sự cố.";
                    HasValidationError = true;
                    _dialogService.ShowError("Bạn chưa nhận phòng trong ký túc xá nên không thể gửi báo cáo sự cố.", "Chưa Có Phòng Ở");
                    return;
                }
            }

            if (!ValidateInput())
            {
                _dialogService.ShowError(ValidationMessage, "Lỗi Nhập Liệu");
                return;
            }

            int studentId = 1; // Default fallback
            int roomId = SelectedNewRoomId ?? 1;

            if (_currentUser != null && _currentUser.Role == "Student")
            {
                var student = await _studentService.GetStudentByUserIdAsync(_currentUser.UserId);
                var activeAssignment = student!.RoomAssignments.First(ra => ra.Status == "Active");
                studentId = student.StudentId;
                roomId = activeAssignment.RoomId;
            }
            else
            {
                // Manager creating a request for a room
                if (SelectedNewRoomId.HasValue)
                {
                    roomId = SelectedNewRoomId.Value;
                    // Try to get a student in that room
                    var roomDetail = await _roomService.GetRoomDetailAsync(roomId);
                    var activeBed = roomDetail?.Beds?.FirstOrDefault(b => b.Status == "Occupied");
                    if (activeBed != null)
                    {
                        var students = await _studentService.GetAllStudentsAsync();
                        var student = students.FirstOrDefault(s => s.StudentCode == activeBed.StudentCode);
                        if (student != null)
                        {
                            studentId = student.Id;
                        }
                        else
                        {
                            _dialogService.ShowError("Không thể xác định sinh viên trong phòng này.");
                            return;
                        }
                    }
                    else
                    {
                        _dialogService.ShowError("Phòng này hiện không có sinh viên nào đang ở. Hệ thống yêu cầu phải có sinh viên để tạo yêu cầu.");
                        return;
                    }
                }
            }

            IsBusy = true;
            var result = await _maintenanceService.CreateRequestAsync(studentId, roomId, NewTitle, NewDescription, NewPriority);
            IsBusy = false;

            if (result.IsSuccess)
            {
                _dialogService.ShowInformation(result.Message);
                NewTitle = string.Empty;
                NewDescription = string.Empty;
                NewPriority = "Medium";
                HasValidationError = false;
                await LoadRequestsAsync();
            }
            else
            {
                _dialogService.ShowError(result.Message);
            }
        }

        private async Task ExecuteCompleteRequestAsync()
        {
            if (SelectedRequest == null) return;

            IsBusy = true;
            var result = await _maintenanceService.UpdateStatusAsync(SelectedRequest.RequestId, "Resolved", "Đã xử lý sửa chữa hoàn tất.");
            IsBusy = false;

            if (result.IsSuccess)
            {
                _dialogService.ShowInformation(result.Message);
                await LoadRequestsAsync();
            }
            else
            {
                _dialogService.ShowError(result.Message);
            }
        }


        private async Task ExecuteCloseRequestAsync()
        {
            if (SelectedRequest == null) return;

            IsBusy = true;
            var result = await _maintenanceService.CloseRequestAsync(SelectedRequest.RequestId, "Quản lý đã đóng yêu cầu này.");
            IsBusy = false;

            if (result.IsSuccess)
            {
                _dialogService.ShowInformation(result.Message);
                await LoadRequestsAsync(); //Fix here  
            }
            else
            {
                _dialogService.ShowError(result.Message);
            }
        }

        private async Task ExecuteMarkInProgressAsync()
        {
            if (SelectedRequest == null) return;

            IsBusy = true;
            var result = await _maintenanceService.UpdateStatusAsync(SelectedRequest.RequestId, "InProgress", "Sự cố đang được tiến hành xử lý.");
            IsBusy = false;

            if (result.IsSuccess)
            {
                _dialogService.ShowInformation(result.Message);
                await LoadRequestsAsync();
            }
            else
            {
                _dialogService.ShowError(result.Message);
            }
        }

        private async Task ExecuteUpdateSelectedStatusAsync()
        {
            if (SelectedRequest == null) return;

            string note = string.IsNullOrWhiteSpace(UpdateMessage) ? $"Quản lý đã cập nhật trạng thái thành: {SelectedUpdateStatus}" : UpdateMessage;

            IsBusy = true;
            var result = await _maintenanceService.UpdateStatusAsync(SelectedRequest.RequestId, SelectedUpdateStatus, note);
            IsBusy = false;

            if (result.IsSuccess)
            {
                _dialogService.ShowInformation("Cập nhật trạng thái và gửi thông báo thành công!");
                UpdateMessage = string.Empty;
                await LoadRequestsAsync();
            }
            else
            {
                _dialogService.ShowError(result.Message);
            }
        }
    }
}
