using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using DormCare.Business.DTOs;
using DormCare.Business.Services;
using DormCare.WPF.Commands;
using DormCare.WPF.Services;

namespace DormCare.WPF.ViewModels
{
    public class BedViewModel : BaseViewModel
    {
        private readonly BedService _bedService;
        private readonly DialogService _dialogService;
        private readonly int _roomId;

        private ObservableCollection<BedDto> _allBeds = new();

        private ObservableCollection<BedDto> _beds = new();
        public ObservableCollection<BedDto> Beds
        {
            get => _beds;
            set => SetProperty(ref _beds, value);
        }

        private BedDto? _selectedBed;
        public BedDto? SelectedBed
        {
            get => _selectedBed;
            set => SetProperty(ref _selectedBed, value);
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

        public ICommand RefreshCommand { get; }
        public ICommand SetAvailableCommand { get; }
        public ICommand SetMaintenanceCommand { get; }

        public BedViewModel(BedService bedService, DialogService dialogService, int roomId = 1)
        {
            Title = "🔵 Dạng — Quản Lý Danh Sách Giường";
            _bedService = bedService;
            _dialogService = dialogService;
            _roomId = roomId;

            RefreshCommand = new AsyncRelayCommand(LoadBedsAsync);
            SetAvailableCommand = new AsyncRelayCommand(() => ExecuteChangeStatusAsync("Available"), () => SelectedBed != null);
            SetMaintenanceCommand = new AsyncRelayCommand(() => ExecuteChangeStatusAsync("Maintenance"), () => SelectedBed != null);

            _ = LoadBedsAsync();
        }

        public async Task LoadBedsAsync()
        {
            IsBusy = true;
            var dtos = await _bedService.GetBedsByRoomIdAsync(_roomId);
            _allBeds = new ObservableCollection<BedDto>(dtos);
            ApplyFilters();
            IsBusy = false;
        }

        private void ApplyFilters()
        {
            var query = _allBeds.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                query = query.Where(b => b.BedCode.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                                         b.BedNumber.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                                         b.RoomNumber.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(SelectedStatusFilter) && SelectedStatusFilter != "All")
            {
                query = query.Where(b => b.Status.Equals(SelectedStatusFilter, StringComparison.OrdinalIgnoreCase));
            }

            Beds = new ObservableCollection<BedDto>(query);
        }

        private async Task ExecuteChangeStatusAsync(string newStatus)
        {
            if (SelectedBed == null) return;

            IsBusy = true;
            var result = await _bedService.UpdateBedStatusAsync(SelectedBed.BedId, newStatus);
            IsBusy = false;

            if (result.IsSuccess)
            {
                _dialogService.ShowInformation(result.Message);
                await LoadBedsAsync();
            }
            else
            {
                _dialogService.ShowError(result.Message);
            }
        }
    }
}
