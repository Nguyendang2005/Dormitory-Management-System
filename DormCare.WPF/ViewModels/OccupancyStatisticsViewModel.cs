using System.Threading.Tasks;
using System.Windows.Input;
using DormCare.Business.DTOs;
using DormCare.Business.Services;
using DormCare.WPF.Commands;

namespace DormCare.WPF.ViewModels
{
    public class OccupancyStatisticsViewModel : BaseViewModel
    {
        private readonly OccupancyService _occupancyService;

        private OccupancyStatisticsDto _statistics = new();
        public OccupancyStatisticsDto Statistics
        {
            get => _statistics;
            set => SetProperty(ref _statistics, value);
        }

        // View Mode Toggle (Card View vs DataGrid Table View)
        private bool _isCardView = true;
        public bool IsCardView
        {
            get => _isCardView;
            set
            {
                if (SetProperty(ref _isCardView, value))
                {
                    OnPropertyChanged(nameof(IsTableView));
                }
            }
        }
        public bool IsTableView => !IsCardView;

        public ICommand RefreshCommand { get; }
        public ICommand ToggleViewModeCommand { get; }

        public OccupancyStatisticsViewModel(OccupancyService occupancyService)
        {
            Title = "Báo cáo & Thống kê lấp đầy KTX";
            _occupancyService = occupancyService;

            RefreshCommand = new AsyncRelayCommand(LoadStatisticsAsync);
            ToggleViewModeCommand = new RelayCommand(_ => IsCardView = !IsCardView);

            _ = LoadStatisticsAsync();
        }

        public async Task LoadStatisticsAsync()
        {
            try
            {
                IsBusy = true;
                Statistics = await _occupancyService.GetOccupancyStatisticsAsync();
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
