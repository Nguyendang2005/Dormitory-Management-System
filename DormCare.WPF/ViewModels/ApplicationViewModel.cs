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
    public class ApplicationViewModel : BaseViewModel
    {
        private readonly ApplicationService _applicationService;
        private readonly DialogService _dialogService;
        private readonly User? _currentUser;

        private ObservableCollection<RoomApplication> _allApplications = new();

        private ObservableCollection<RoomApplication> _applications = new();
        public ObservableCollection<RoomApplication> Applications
        {
            get => _applications;
            set => SetProperty(ref _applications, value);
        }

        private RoomApplication? _selectedApplication;
        public RoomApplication? SelectedApplication
        {
            get => _selectedApplication;
            set => SetProperty(ref _selectedApplication, value);
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

        public ICommand ApproveCommand { get; }
        public ICommand RejectCommand { get; }
        public ICommand RefreshCommand { get; }

        public ApplicationViewModel(ApplicationService applicationService, DialogService dialogService, User? currentUser = null)
        {
            Title = "Quản lý Đơn Đăng Ký";
            _applicationService = applicationService;
            _dialogService = dialogService;
            _currentUser = currentUser;

            RefreshCommand = new AsyncRelayCommand(LoadApplicationsAsync);
            ApproveCommand = new AsyncRelayCommand(ExecuteApproveAsync, () => SelectedApplication != null);
            RejectCommand = new AsyncRelayCommand(ExecuteRejectAsync, () => SelectedApplication != null);

            _ = LoadApplicationsAsync();
        }

        public async Task LoadApplicationsAsync()
        {
            IsBusy = true;
            var apps = await _applicationService.GetAllApplicationsAsync();
            _allApplications = new ObservableCollection<RoomApplication>(apps);
            ApplyFilters();
            IsBusy = false;
        }

        private void ApplyFilters()
        {
            var query = _allApplications.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                query = query.Where(a => a.ApplicationCode.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                                         (a.Student != null && a.Student.FullName.Contains(SearchText, StringComparison.OrdinalIgnoreCase)) ||
                                         (a.Student != null && a.Student.StudentCode.Contains(SearchText, StringComparison.OrdinalIgnoreCase)) ||
                                         (a.Room != null && a.Room.RoomNumber.Contains(SearchText, StringComparison.OrdinalIgnoreCase)));
            }

            if (!string.IsNullOrWhiteSpace(SelectedStatusFilter) && SelectedStatusFilter != "All")
            {
                query = query.Where(a => a.Status.Equals(SelectedStatusFilter, StringComparison.OrdinalIgnoreCase));
            }

            Applications = new ObservableCollection<RoomApplication>(query);
        }

        private async Task ExecuteApproveAsync()
        {
            if (SelectedApplication == null) return;
            if (!_dialogService.ShowConfirmation($"Chấp nhận đơn đăng ký của sinh viên {SelectedApplication.Student?.FullName}?")) return;

            IsBusy = true;
            var reviewerId = _currentUser?.UserId ?? 1;
            var result = await _applicationService.ApproveApplicationAsync(SelectedApplication.ApplicationId, reviewerId, "Đã duyệt bởi Quản lý");
            IsBusy = false;

            if (result.IsSuccess)
            {
                _dialogService.ShowInformation(result.Message);
                await LoadApplicationsAsync();
            }
            else
            {
                _dialogService.ShowError(result.Message);
            }
        }

        private async Task ExecuteRejectAsync()
        {
            if (SelectedApplication == null) return;
            if (!_dialogService.ShowConfirmation($"Từ chối đơn đăng ký của sinh viên {SelectedApplication.Student?.FullName}?")) return;

            IsBusy = true;
            var reviewerId = _currentUser?.UserId ?? 1;
            var result = await _applicationService.RejectApplicationAsync(SelectedApplication.ApplicationId, reviewerId, "Không đạt tiêu chuẩn / Hết chỗ");
            IsBusy = false;

            if (result.IsSuccess)
            {
                _dialogService.ShowInformation(result.Message);
                await LoadApplicationsAsync();
            }
            else
            {
                _dialogService.ShowError(result.Message);
            }
        }
    }
}
