using System;
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
    public class BuildingViewModel : BaseViewModel
    {
        private readonly BuildingService _buildingService;
        private readonly DialogService _dialogService;

        private ObservableCollection<BuildingDto> _allBuildings = new();

        private ObservableCollection<BuildingDto> _buildings = new();
        public ObservableCollection<BuildingDto> Buildings
        {
            get => _buildings;
            set => SetProperty(ref _buildings, value);
        }

        private BuildingDto? _selectedBuilding;
        public BuildingDto? SelectedBuilding
        {
            get => _selectedBuilding;
            set => SetProperty(ref _selectedBuilding, value);
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

        public BuildingViewModel(BuildingService buildingService, DialogService dialogService)
        {
            Title = "🔵 Dạng — Quản Lý Tòa Nhà Ký Túc Xá";
            _buildingService = buildingService;
            _dialogService = dialogService;

            RefreshCommand = new AsyncRelayCommand(LoadBuildingsAsync);
            _ = LoadBuildingsAsync();
        }

        public async Task LoadBuildingsAsync()
        {
            IsBusy = true;
            var dtos = await _buildingService.GetAllBuildingsAsync();
            _allBuildings = new ObservableCollection<BuildingDto>(dtos);
            ApplyFilters();
            IsBusy = false;
        }

        private void ApplyFilters()
        {
            var query = _allBuildings.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                query = query.Where(b => b.BuildingCode.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                                         b.BuildingName.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(SelectedStatusFilter) && SelectedStatusFilter != "All")
            {
                query = query.Where(b => b.Status.Equals(SelectedStatusFilter, StringComparison.OrdinalIgnoreCase));
            }

            Buildings = new ObservableCollection<BuildingDto>(query);
        }
    }
}
