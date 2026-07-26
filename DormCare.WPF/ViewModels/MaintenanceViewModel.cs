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
        private readonly DialogService _dialogService;
        private readonly User? _currentUser;

        private ObservableCollection<MaintenanceRequest> _allRequests = new();

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
            set => SetProperty(ref _selectedRequest, value);
        }

        private string _newTitle = string.Empty;
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

        public ICommand RefreshCommand { get; }
        public ICommand CreateRequestCommand { get; }
        public ICommand CompleteRequestCommand { get; }

        public MaintenanceViewModel(MaintenanceService maintenanceService, DialogService dialogService, User? currentUser = null)
        {
            Title = "Quản lý Báo Cáo & Sửa Chữa";
            _maintenanceService = maintenanceService;
            _dialogService = dialogService;
            _currentUser = currentUser;

            RefreshCommand = new AsyncRelayCommand(LoadRequestsAsync);
            CreateRequestCommand = new AsyncRelayCommand(ExecuteCreateRequestAsync);
            CompleteRequestCommand = new AsyncRelayCommand(ExecuteCompleteRequestAsync, () => SelectedRequest != null);

            _ = LoadRequestsAsync();
        }

        public async Task LoadRequestsAsync()
        {
            IsBusy = true;
            if (_currentUser != null && _currentUser.Role == "Student" && _currentUser.StudentProfile != null)
            {
                var reqs = await _maintenanceService.GetRequestsByStudentIdAsync(_currentUser.StudentProfile.StudentId);
                _allRequests = new ObservableCollection<MaintenanceRequest>(reqs);
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

            if (!string.IsNullOrWhiteSpace(SelectedPriorityFilter) && SelectedPriorityFilter != "All")
            {
                query = query.Where(m => m.Priority.Equals(SelectedPriorityFilter, StringComparison.OrdinalIgnoreCase));
            }

            Requests = new ObservableCollection<MaintenanceRequest>(query);
        }

        private bool ValidateInput()
        {
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
            if (!ValidateInput())
            {
                _dialogService.ShowError(ValidationMessage, "Lỗi Nhập Liệu");
                return;
            }

            if (_currentUser == null || _currentUser.StudentProfile == null)
            {
                _dialogService.ShowError("Chỉ sinh viên đang ở trong ký túc xá mới có thể gửi yêu cầu sửa chữa.");
                return;
            }

            IsBusy = true;
            var roomId = 1;
            var result = await _maintenanceService.CreateRequestAsync(_currentUser.StudentProfile.StudentId, roomId, NewTitle, NewDescription);
            IsBusy = false;

            if (result.IsSuccess)
            {
                _dialogService.ShowInformation(result.Message);
                NewTitle = string.Empty;
                NewDescription = string.Empty;
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
    }
}
