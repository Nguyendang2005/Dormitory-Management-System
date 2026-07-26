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

        public ICommand RefreshCommand { get; }

        public OccupancyStatisticsViewModel(OccupancyService occupancyService)
        {
            Title = "Tổng quan tình trạng sử dụng";
            _occupancyService = occupancyService;

            RefreshCommand = new AsyncRelayCommand(LoadStatisticsAsync);
            _ = LoadStatisticsAsync();
        }

        public async Task LoadStatisticsAsync()
        {
            IsBusy = true;
            Statistics = await _occupancyService.GetOccupancyStatisticsAsync();
            IsBusy = false;
        }
    }
}
