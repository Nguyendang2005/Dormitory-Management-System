using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using DormCare.Business.DTOs;
using DormCare.Business.Services;
using DormCare.WPF.Commands;

namespace DormCare.WPF.ViewModels
{
    /// <summary>
    /// ViewModel cho RoomDetailWindow — hỗ trợ 4 tab:
    /// 0=Tổng quan, 1=Sơ đồ giường, 2=Sinh viên đang ở, 3=Lịch sử cư trú
    /// </summary>
    public class RoomDetailViewModel : BaseViewModel
    {
        private readonly RoomService _roomService;

        // ───────────────────────────────────────────────
        //  Core data
        // ───────────────────────────────────────────────
        private RoomDetailDto? _roomDetail;
        public RoomDetailDto? RoomDetail
        {
            get => _roomDetail;
            set => SetProperty(ref _roomDetail, value);
        }

        private ObservableCollection<RoomResidentDto> _residents = new();
        public ObservableCollection<RoomResidentDto> Residents
        {
            get => _residents;
            set => SetProperty(ref _residents, value);
        }

        private ObservableCollection<RoomHistoryEntryDto> _history = new();
        public ObservableCollection<RoomHistoryEntryDto> History
        {
            get => _history;
            set => SetProperty(ref _history, value);
        }

        // ───────────────────────────────────────────────
        //  Tab navigation (0=Tổng quan, 1=Sơ đồ giường, 2=Sinh viên, 3=Lịch sử)
        // ───────────────────────────────────────────────
        private int _activeTab;
        public int ActiveTab
        {
            get => _activeTab;
            set => SetProperty(ref _activeTab, value);
        }

        // ───────────────────────────────────────────────
        //  Loading states per tab
        // ───────────────────────────────────────────────
        private bool _isLoadingResidents;
        public bool IsLoadingResidents
        {
            get => _isLoadingResidents;
            set => SetProperty(ref _isLoadingResidents, value);
        }

        private bool _isLoadingHistory;
        public bool IsLoadingHistory
        {
            get => _isLoadingHistory;
            set => SetProperty(ref _isLoadingHistory, value);
        }

        public bool HasResidents => Residents.Count > 0;
        public bool HasHistory => History.Count > 0;

        // ───────────────────────────────────────────────
        //  Commands
        // ───────────────────────────────────────────────
        public ICommand SelectTabCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand CloseCommand { get; }

        public Action? CloseAction { get; set; }

        // ───────────────────────────────────────────────
        //  Constructors
        // ───────────────────────────────────────────────
        /// <summary>Constructor đầy đủ — dùng khi mở từ Room list với service.</summary>
        public RoomDetailViewModel(RoomService roomService, RoomDetailDto roomDetail)
        {
            _roomService = roomService;
            Title = $"Chi Tiết Phòng {roomDetail.RoomNumber}";
            RoomDetail = roomDetail;

            SelectTabCommand = new AsyncRelayCommand(ExecuteSelectTabAsync);
            RefreshCommand = new AsyncRelayCommand(RefreshCurrentTabAsync);
            CloseCommand = new RelayCommand(_ => CloseAction?.Invoke());
        }

        /// <summary>Constructor backward-compatible — dùng khi không có service (cũ).</summary>
        public RoomDetailViewModel(RoomDetailDto roomDetail)
            : this(null!, roomDetail)
        {
        }

        // ───────────────────────────────────────────────
        //  Tab switching
        // ───────────────────────────────────────────────
        private async Task ExecuteSelectTabAsync(object? param)
        {
            if (param != null && int.TryParse(param.ToString(), out int tab))
            {
                ActiveTab = tab;
                await LoadTabDataAsync(tab);
            }
        }

        public async Task LoadTabDataAsync(int tab)
        {
            if (_roomService == null || RoomDetail == null) return;

            switch (tab)
            {
                case 2: // Sinh viên đang ở
                    await LoadResidentsAsync();
                    break;
                case 3: // Lịch sử
                    await LoadHistoryAsync();
                    break;
            }
        }

        private async Task LoadResidentsAsync()
        {
            if (_roomService == null || RoomDetail == null) return;
            try
            {
                IsLoadingResidents = true;
                var residents = await _roomService.GetRoomResidentsAsync(RoomDetail.RoomId);
                Residents = new ObservableCollection<RoomResidentDto>(residents);
                OnPropertyChanged(nameof(HasResidents));
            }
            finally
            {
                IsLoadingResidents = false;
            }
        }

        private async Task LoadHistoryAsync()
        {
            if (_roomService == null || RoomDetail == null) return;
            try
            {
                IsLoadingHistory = true;
                var history = await _roomService.GetRoomHistoryAsync(RoomDetail.RoomId);
                History = new ObservableCollection<RoomHistoryEntryDto>(history);
                OnPropertyChanged(nameof(HasHistory));
            }
            finally
            {
                IsLoadingHistory = false;
            }
        }

        private async Task RefreshCurrentTabAsync()
        {
            await LoadTabDataAsync(ActiveTab);
        }
    }
}
