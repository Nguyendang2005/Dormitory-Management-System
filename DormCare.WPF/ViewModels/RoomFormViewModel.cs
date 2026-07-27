using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using DormCare.Business.DTOs;
using DormCare.Business.Services;
using DormCare.Domain.Entities;
using DormCare.WPF.Commands;

namespace DormCare.WPF.ViewModels
{
    public class RoomFormViewModel : BaseViewModel
    {
        private readonly RoomService _roomService;
        private readonly BuildingService _buildingService;
        public Action<bool?>? CloseAction { get; set; }

        public bool IsEditMode { get; }
        public int RoomId { get; }

        public string WindowTitle => IsEditMode ? "SỬA PHÒNG Ở KÝ TÚC XÁ" : "THÊM PHÒNG Ở KÝ TÚC XÁ";
        public string WindowSubtitle => IsEditMode ? "Cập nhật thông tin phòng ở và sức chứa" : "Nhập đầy đủ thông tin để tạo phòng ở mới";
        public string HeaderIcon => IsEditMode ? "✏️" : "➕";
        public string SubmitButtonText => IsEditMode ? "Lưu Thay Đổi" : "Thêm Phòng";

        public ObservableCollection<BuildingDto> ActiveBuildings { get; } = new();
        public ObservableCollection<int> AvailableFloors { get; } = new();
        public List<string> RoomTypeOptions { get; } = new() { "Standard", "Premium", "Accessible" };
        public List<string> GenderTypeOptions { get; } = new() { "Male", "Female", "Mixed" };
        public List<string> StatusOptions { get; } = new() { "Available", "Full", "Maintenance", "Inactive" };

        private BuildingDto? _selectedBuilding;
        public BuildingDto? SelectedBuilding
        {
            get => _selectedBuilding;
            set
            {
                if (SetProperty(ref _selectedBuilding, value))
                {
                    UpdateAvailableFloors();
                }
            }
        }

        private string _roomNumber = string.Empty;
        public string RoomNumber
        {
            get => _roomNumber;
            set => SetProperty(ref _roomNumber, value);
        }

        private int _selectedFloor = 1;
        public int SelectedFloor
        {
            get => _selectedFloor;
            set => SetProperty(ref _selectedFloor, value);
        }

        private string _selectedRoomType = "Standard";
        public string SelectedRoomType
        {
            get => _selectedRoomType;
            set
            {
                if (SetProperty(ref _selectedRoomType, value))
                {
                    AutoSetMonthlyRentAndCapacity();
                }
            }
        }

        private string _capacityText = "6";
        public string CapacityText
        {
            get => _capacityText;
            set => SetProperty(ref _capacityText, value);
        }

        private string _monthlyRentText = "1500000";
        public string MonthlyRentText
        {
            get => _monthlyRentText;
            set => SetProperty(ref _monthlyRentText, value);
        }

        private string _selectedGenderType = "Male";
        public string SelectedGenderType
        {
            get => _selectedGenderType;
            set => SetProperty(ref _selectedGenderType, value);
        }

        private string _selectedStatus = "Available";
        public string SelectedStatus
        {
            get => _selectedStatus;
            set => SetProperty(ref _selectedStatus, value);
        }

        private string _description = string.Empty;
        public string Description
        {
            get => _description;
            set
            {
                if (SetProperty(ref _description, value))
                {
                    OnPropertyChanged(nameof(DescriptionCounterText));
                    OnPropertyChanged(nameof(DescriptionCounterColor));
                }
            }
        }

        public string DescriptionCounterText => $"{Description?.Length ?? 0} / 500";
        public string DescriptionCounterColor => (Description?.Length ?? 0) > 500 ? "#EF4444" : "#64748B";

        private string _errorMessage = string.Empty;
        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        // Add Constructor
        public RoomFormViewModel(RoomService roomService, BuildingService buildingService)
        {
            _roomService = roomService;
            _buildingService = buildingService;
            IsEditMode = false;

            SaveCommand = new AsyncRelayCommand(ExecuteSaveAsync);
            CancelCommand = new RelayCommand(_ => CloseAction?.Invoke(false));

            _ = LoadBuildingsAsync();
        }

        // Edit Constructor
        public RoomFormViewModel(RoomService roomService, BuildingService buildingService, RoomDto roomDto)
        {
            _roomService = roomService;
            _buildingService = buildingService;
            IsEditMode = true;

            RoomId = roomDto.RoomId;
            RoomNumber = roomDto.RoomNumber;
            SelectedFloor = roomDto.FloorNumber;
            SelectedRoomType = RoomTypeOptions.Contains(roomDto.RoomType) ? roomDto.RoomType : "Standard";
            CapacityText = roomDto.Capacity.ToString();
            MonthlyRentText = roomDto.MonthlyRent.ToString("F0");
            SelectedGenderType = roomDto.GenderType;
            SelectedStatus = roomDto.Status;
            Description = roomDto.Description;

            SaveCommand = new AsyncRelayCommand(ExecuteSaveAsync);
            CancelCommand = new RelayCommand(_ => CloseAction?.Invoke(false));

            _ = LoadBuildingsAsync(roomDto.BuildingId);
        }

        private async Task LoadBuildingsAsync(int? selectBuildingId = null)
        {
            try
            {
                var buildings = await _buildingService.GetAllBuildingsAsync();
                ActiveBuildings.Clear();

                foreach (var b in buildings.Where(b => b.Status == "Active" || (selectBuildingId.HasValue && b.BuildingId == selectBuildingId.Value)))
                {
                    // Clean up building name if it has suffix
                    b.BuildingName = b.BuildingName.Replace(" (Đã cập nhật)", "");
                    ActiveBuildings.Add(b);
                }

                if (selectBuildingId.HasValue)
                {
                    SelectedBuilding = ActiveBuildings.FirstOrDefault(b => b.BuildingId == selectBuildingId.Value);
                }
                else
                {
                    SelectedBuilding = ActiveBuildings.FirstOrDefault();
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Lỗi khi tải danh sách tòa nhà: {ex.Message}";
            }
        }

        private void UpdateAvailableFloors()
        {
            AvailableFloors.Clear();
            if (SelectedBuilding != null && SelectedBuilding.NumberOfFloors > 0)
            {
                for (int i = 1; i <= SelectedBuilding.NumberOfFloors; i++)
                {
                    AvailableFloors.Add(i);
                }
                if (!AvailableFloors.Contains(SelectedFloor))
                {
                    SelectedFloor = AvailableFloors.FirstOrDefault();
                }
            }
            else
            {
                AvailableFloors.Add(1);
                SelectedFloor = 1;
            }
        }

        private void AutoSetMonthlyRentAndCapacity()
        {
            if (IsEditMode) return; // Don't override user entries on edit

            switch (SelectedRoomType)
            {
                case "Standard":
                    MonthlyRentText = "1500000";
                    CapacityText = "6";
                    break;
                case "Premium":
                    MonthlyRentText = "2000000";
                    CapacityText = "6";
                    break;
                case "Accessible":
                    MonthlyRentText = "1800000";
                    CapacityText = "4";
                    break;
            }
        }

        private async Task ExecuteSaveAsync()
        {
            ErrorMessage = string.Empty;

            if (SelectedBuilding == null)
            {
                ErrorMessage = "Vui lòng chọn tòa nhà.";
                return;
            }

            if (!int.TryParse(CapacityText?.Trim(), out int capacity))
            {
                ErrorMessage = "Sức chứa phải là số nguyên (từ 1 đến 20 giường).";
                return;
            }

            if (!decimal.TryParse(MonthlyRentText?.Trim(), out decimal rent))
            {
                ErrorMessage = "Giá thuê phòng không hợp lệ.";
                return;
            }

            var room = new Room
            {
                RoomId = RoomId,
                BuildingId = SelectedBuilding.BuildingId,
                RoomNumber = RoomNumber,
                FloorNumber = SelectedFloor,
                RoomType = SelectedRoomType,
                Capacity = capacity,
                MonthlyRent = rent,
                GenderType = SelectedGenderType,
                Status = SelectedStatus,
                Description = Description
            };

            IsBusy = true;

            if (IsEditMode)
            {
                var updateRes = await _roomService.UpdateRoomAsync(room);
                IsBusy = false;
                if (updateRes.IsSuccess)
                {
                    CloseAction?.Invoke(true);
                }
                else
                {
                    ErrorMessage = updateRes.Message;
                }
            }
            else
            {
                var addRes = await _roomService.AddRoomAsync(room);
                IsBusy = false;
                if (addRes.IsSuccess)
                {
                    CloseAction?.Invoke(true);
                }
                else
                {
                    ErrorMessage = addRes.Message;
                }
            }
        }
    }
}
