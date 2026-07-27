using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Input;
using DormCare.Business.Services;
using DormCare.Domain.Entities;
using DormCare.WPF.Commands;

namespace DormCare.WPF.ViewModels
{
    public class BuildingFormViewModel : BaseViewModel
    {
        private readonly BuildingService _buildingService;
        public Action<bool?>? CloseAction { get; set; }

        public bool IsEditMode { get; }
        public int BuildingId { get; }

        public string WindowTitle => IsEditMode ? "SỬA TÒA NHÀ" : "THÊM TÒA NHÀ";
        public string WindowSubtitle => IsEditMode ? "Cập nhật thông tin tòa nhà ký túc xá" : "Nhập đầy đủ thông tin để tạo tòa nhà mới";
        public string HeaderIcon => IsEditMode ? "✏️" : "➕";
        public string SubmitButtonText => IsEditMode ? "Lưu Thay Đổi" : "Thêm Tòa Nhà";
        public string BuildingCodeHint => IsEditMode ? "(🔒 Không thể sửa mã khi cập nhật)" : "Mã tòa nhà (Ví dụ: A, A-01, D01)";

        public List<string> StatusOptions { get; } = new() { "Active", "Inactive", "Maintenance" };

        private string _buildingCode = string.Empty;
        public string BuildingCode
        {
            get => _buildingCode;
            set => SetProperty(ref _buildingCode, value);
        }

        private string _buildingName = string.Empty;
        public string BuildingName
        {
            get => _buildingName;
            set => SetProperty(ref _buildingName, value);
        }

        private string _address = string.Empty;
        public string Address
        {
            get => _address;
            set => SetProperty(ref _address, value);
        }

        private string _numberOfFloorsText = "5";
        public string NumberOfFloorsText
        {
            get => _numberOfFloorsText;
            set => SetProperty(ref _numberOfFloorsText, value);
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
        public string DescriptionCounterColor => (Description?.Length ?? 0) > 500 ? "#EF4444" : "#94A3B8";

        private string _selectedStatus = "Active";
        public string SelectedStatus
        {
            get => _selectedStatus;
            set => SetProperty(ref _selectedStatus, value);
        }

        private string _errorMessage = string.Empty;
        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        // Constructor for Add
        public BuildingFormViewModel(BuildingService buildingService)
        {
            _buildingService = buildingService;
            IsEditMode = false;

            SaveCommand = new AsyncRelayCommand(ExecuteSaveAsync);
            CancelCommand = new RelayCommand(_ => CloseAction?.Invoke(false));
        }

        // Constructor for Edit
        public BuildingFormViewModel(BuildingService buildingService, Building building)
        {
            _buildingService = buildingService;
            IsEditMode = true;
            BuildingId = building.BuildingId;
            BuildingCode = building.BuildingCode;
            BuildingName = building.BuildingName;
            Address = building.Address;
            NumberOfFloorsText = building.NumberOfFloors.ToString();
            Description = building.Description ?? string.Empty;
            SelectedStatus = building.Status;

            SaveCommand = new AsyncRelayCommand(ExecuteSaveAsync);
            CancelCommand = new RelayCommand(_ => CloseAction?.Invoke(false));
        }

        private async Task ExecuteSaveAsync()
        {
            ErrorMessage = string.Empty;

            if (!int.TryParse(NumberOfFloorsText?.Trim(), out int floors))
            {
                ErrorMessage = "Số tầng phải là số nguyên (từ 1 đến 100).";
                return;
            }

            // Sanitize status string in case WPF passes ComboBoxItem object string representation
            string sanitizedStatus = SelectedStatus ?? "Active";
            if (sanitizedStatus.Contains(":"))
            {
                var parts = sanitizedStatus.Split(':');
                sanitizedStatus = parts[parts.Length - 1].Trim();
            }

            var building = new Building
            {
                BuildingId = BuildingId,
                BuildingCode = BuildingCode,
                BuildingName = BuildingName,
                Address = Address,
                NumberOfFloors = floors,
                Description = Description,
                Status = sanitizedStatus
            };

            IsBusy = true;
            var result = IsEditMode
                ? await _buildingService.UpdateBuildingAsync(building)
                : await _buildingService.AddBuildingAsync(building);
            IsBusy = false;

            if (result.IsSuccess)
            {
                CloseAction?.Invoke(true);
            }
            else
            {
                ErrorMessage = result.Message;
            }
        }
    }
}
