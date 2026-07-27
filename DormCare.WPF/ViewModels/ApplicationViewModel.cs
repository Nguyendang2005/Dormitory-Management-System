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

namespace DormCare.WPF.ViewModels
{
    public class ApplicationViewModel : BaseViewModel
    {
        private readonly ApplicationService _applicationService;
        private readonly DialogService _dialogService;
        private readonly User? _currentUser;

        private ObservableCollection<RoomApplication> _allApplications = new();

        public ObservableCollection<RoomApplication> Applications { get; private set; } = new();
        public ObservableCollection<BedDto> AvailableBeds { get; private set; } = new();

        private RoomApplication? _selectedApplication;
        public RoomApplication? SelectedApplication
        {
            get => _selectedApplication;
            set
            {
                if (SetProperty(ref _selectedApplication, value))
                {
                    ReviewNote = value?.ReviewNote ?? string.Empty;
                    SelectedBed = null;
                    _ = LoadAvailableBedsAsync();
                    System.Windows.Input.CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        private BedDto? _selectedBed;
        public BedDto? SelectedBed
        {
            get => _selectedBed;
            set => SetProperty(ref _selectedBed, value);
        }

        private string _reviewNote = string.Empty;
        public string ReviewNote
        {
            get => _reviewNote;
            set => SetProperty(ref _reviewNote, value);
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
        public ICommand ResetFiltersCommand { get; }

        public ApplicationViewModel(ApplicationService applicationService, DialogService dialogService, User? currentUser = null)
        {
            Title = "Quan ly don dang ky phong";
            _applicationService = applicationService;
            _dialogService = dialogService;
            _currentUser = currentUser;

            RefreshCommand = new AsyncRelayCommand(LoadApplicationsAsync);
            ResetFiltersCommand = new RelayCommand(ResetFilters);
            ApproveCommand = new AsyncRelayCommand(ExecuteApproveAsync, CanReviewSelectedApplication);
            RejectCommand = new AsyncRelayCommand(ExecuteRejectAsync, CanReviewSelectedApplication);

            _ = LoadApplicationsAsync();
        }

        public async Task LoadApplicationsAsync()
        {
            IsBusy = true;
            try
            {
                var apps = await _applicationService.GetAllApplicationsAsync();
                _allApplications = new ObservableCollection<RoomApplication>(apps);
                ApplyFilters();
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task LoadAvailableBedsAsync()
        {
            AvailableBeds = SelectedApplication == null
                ? new ObservableCollection<BedDto>()
                : new ObservableCollection<BedDto>(await _applicationService.GetAvailableBedsByRoomAsync(SelectedApplication.RoomId));

            if (SelectedApplication?.PreferredBedId != null)
            {
                SelectedBed = AvailableBeds.FirstOrDefault(b => b.BedId == SelectedApplication.PreferredBedId.Value);
            }

            OnPropertyChanged(nameof(AvailableBeds));
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
            OnPropertyChanged(nameof(Applications));
        }

        private void ResetFilters()
        {
            SearchText = string.Empty;
            SelectedStatusFilter = "All";
            ApplyFilters();
        }

        private bool CanReviewSelectedApplication()
        {
            return !IsBusy && SelectedApplication?.Status == "Pending";
        }

        private async Task ExecuteApproveAsync()
        {
            if (SelectedApplication == null)
            {
                return;
            }

            if (SelectedBed == null)
            {
                _dialogService.ShowError("Vui long chon giuong con trong de duyet don.");
                return;
            }

            if (!_dialogService.ShowConfirmation($"Duyet don cua sinh vien {SelectedApplication.Student?.FullName} va giu giuong {SelectedBed.BedCode}?"))
            {
                return;
            }

            IsBusy = true;
            try
            {
                var reviewerId = _currentUser?.UserId ?? 0;
                var result = await _applicationService.ApproveApplicationAsync(
                    SelectedApplication.ApplicationId,
                    reviewerId,
                    ReviewNote,
                    SelectedBed.BedId);

                if (result.IsSuccess)
                {
                    _dialogService.ShowInformation(result.Message);
                    await LoadApplicationsAsync();
                }
                else
                {
                    _dialogService.ShowError(result.Message);
                    await LoadAvailableBedsAsync();
                }
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task ExecuteRejectAsync()
        {
            if (SelectedApplication == null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(ReviewNote))
            {
                _dialogService.ShowError("Vui long nhap ly do tu choi.");
                return;
            }

            if (!_dialogService.ShowConfirmation($"Tu choi don dang ky cua sinh vien {SelectedApplication.Student?.FullName}?"))
            {
                return;
            }

            IsBusy = true;
            try
            {
                var reviewerId = _currentUser?.UserId ?? 0;
                var result = await _applicationService.RejectApplicationAsync(SelectedApplication.ApplicationId, reviewerId, ReviewNote);

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
            finally
            {
                IsBusy = false;
            }
        }
    }
}
