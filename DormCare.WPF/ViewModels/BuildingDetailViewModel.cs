using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using DormCare.Business.DTOs;
using DormCare.Business.Services;
using DormCare.WPF.Commands;

namespace DormCare.WPF.ViewModels
{
    /// <summary>
    /// ViewModel cho BuildingDetailWindow — hỗ trợ 3 tab: Tổng quan, Theo tầng, Danh sách cư trú.
    /// </summary>
    public class BuildingDetailViewModel : BaseViewModel
    {
        private readonly BuildingService _buildingService;
        private readonly RoomService _roomService;

        // ───────────────────────────────────────────────
        //  Core data
        // ───────────────────────────────────────────────
        private BuildingDetailDto? _building;
        public BuildingDetailDto? Building
        {
            get => _building;
            set
            {
                if (SetProperty(ref _building, value))
                {
                    RebuildFloorList();
                    ApplyFloorFilter();
                    ApplyResidentFilter();
                    OnPropertyChanged(nameof(HasResidents));
                }
            }
        }

        // ───────────────────────────────────────────────
        //  Tab navigation (0=Tổng quan, 1=Theo tầng, 2=Cư trú)
        // ───────────────────────────────────────────────
        private int _activeTab;
        public int ActiveTab
        {
            get => _activeTab;
            set => SetProperty(ref _activeTab, value);
        }

        // ───────────────────────────────────────────────
        //  Floor tab buttons
        // ───────────────────────────────────────────────
        private ObservableCollection<int> _floors = new();
        public ObservableCollection<int> Floors
        {
            get => _floors;
            set => SetProperty(ref _floors, value);
        }

        private int _selectedFloor; // 0 = All
        public int SelectedFloor
        {
            get => _selectedFloor;
            set
            {
                if (SetProperty(ref _selectedFloor, value))
                {
                    ApplyFloorFilter();
                    OnPropertyChanged(nameof(SelectedFloorLabel));
                }
            }
        }

        public string SelectedFloorLabel => SelectedFloor == 0 ? "Tất cả tầng" : $"Tầng {SelectedFloor}";

        private ObservableCollection<BuildingRoomSummaryDto> _roomsOnFloor = new();
        public ObservableCollection<BuildingRoomSummaryDto> RoomsOnFloor
        {
            get => _roomsOnFloor;
            set => SetProperty(ref _roomsOnFloor, value);
        }

        // Số sinh viên trên tầng đang chọn
        public int FloorResidentCount => RoomsOnFloor.Sum(r => r.OccupiedBeds);
        public int FloorAvailableCount => RoomsOnFloor.Sum(r => r.AvailableBeds);

        // ───────────────────────────────────────────────
        //  Resident list (Tab 3)
        // ───────────────────────────────────────────────
        private string _residentSearch = string.Empty;
        public string ResidentSearch
        {
            get => _residentSearch;
            set
            {
                if (SetProperty(ref _residentSearch, value))
                    ApplyResidentFilter();
            }
        }

        private string _selectedResidentFloor = "Tất cả tầng";
        public string SelectedResidentFloor
        {
            get => _selectedResidentFloor;
            set
            {
                if (SetProperty(ref _selectedResidentFloor, value))
                    ApplyResidentFilter();
            }
        }

        public ObservableCollection<string> ResidentFloorOptions { get; } = new();

        private ObservableCollection<BuildingResidentDto> _filteredResidents = new();
        public ObservableCollection<BuildingResidentDto> FilteredResidents
        {
            get => _filteredResidents;
            set => SetProperty(ref _filteredResidents, value);
        }

        public bool HasResidents => Building?.TotalResidents > 0;

        // ───────────────────────────────────────────────
        //  Commands
        // ───────────────────────────────────────────────
        public ICommand SelectFloorCommand { get; }
        public ICommand ClearFloorCommand { get; }
        public ICommand SelectTabCommand { get; }
        public ICommand CloseCommand { get; }

        public Action? CloseAction { get; set; }

        // ───────────────────────────────────────────────
        //  Constructor
        // ───────────────────────────────────────────────
        public BuildingDetailViewModel(BuildingService buildingService, RoomService roomService)
        {
            _buildingService = buildingService;
            _roomService = roomService;

            SelectFloorCommand = new RelayCommand(param =>
            {
                if (param != null && int.TryParse(param.ToString(), out int floor))
                    SelectedFloor = (SelectedFloor == floor) ? 0 : floor;
            });

            ClearFloorCommand = new RelayCommand(_ => SelectedFloor = 0);

            SelectTabCommand = new RelayCommand(param =>
            {
                if (param != null && int.TryParse(param.ToString(), out int tab))
                {
                    ActiveTab = tab;
                }
            });

            CloseCommand = new RelayCommand(_ => CloseAction?.Invoke());
        }

        // ───────────────────────────────────────────────
        //  Load
        // ───────────────────────────────────────────────
        public async Task LoadAsync(int buildingId)
        {
            IsBusy = true;
            try
            {
                var detail = await _buildingService.GetBuildingDetailAsync(buildingId);
                if (detail != null)
                {
                    Title = $"Chi Tiết Tòa Nhà {detail.BuildingCode}";
                    Building = detail;
                }
            }
            finally
            {
                IsBusy = false;
            }
        }

        // ───────────────────────────────────────────────
        //  Private helpers
        // ───────────────────────────────────────────────
        private void RebuildFloorList()
        {
            if (Building == null) return;

            var allFloors = Building.Rooms
                .Select(r => r.FloorNumber)
                .Distinct()
                .OrderBy(f => f)
                .ToList();

            Floors = new ObservableCollection<int>(allFloors);

            // Rebuild resident floor filter options
            ResidentFloorOptions.Clear();
            ResidentFloorOptions.Add("Tất cả tầng");
            foreach (var f in allFloors)
                ResidentFloorOptions.Add($"Tầng {f}");
        }

        private void ApplyFloorFilter()
        {
            if (Building == null) return;

            var rooms = SelectedFloor == 0
                ? Building.Rooms
                : Building.Rooms.Where(r => r.FloorNumber == SelectedFloor).ToList();

            RoomsOnFloor = new ObservableCollection<BuildingRoomSummaryDto>(rooms);
            OnPropertyChanged(nameof(FloorResidentCount));
            OnPropertyChanged(nameof(FloorAvailableCount));
        }

        private void ApplyResidentFilter()
        {
            if (Building == null) return;

            var query = Building.AllResidents.AsEnumerable();

            // Floor filter
            if (SelectedResidentFloor != "Tất cả tầng")
            {
                if (int.TryParse(SelectedResidentFloor.Replace("Tầng", "").Trim(), out int fl))
                    query = query.Where(r => r.FloorNumber == fl);
            }

            // Search filter
            if (!string.IsNullOrWhiteSpace(ResidentSearch))
            {
                string kw = ResidentSearch.Trim();
                query = query.Where(r =>
                    r.FullName.Contains(kw, StringComparison.OrdinalIgnoreCase) ||
                    r.StudentCode.Contains(kw, StringComparison.OrdinalIgnoreCase) ||
                    r.RoomNumber.Contains(kw, StringComparison.OrdinalIgnoreCase) ||
                    r.BedCode.Contains(kw, StringComparison.OrdinalIgnoreCase));
            }

            FilteredResidents = new ObservableCollection<BuildingResidentDto>(query.ToList());
        }
    }
}
