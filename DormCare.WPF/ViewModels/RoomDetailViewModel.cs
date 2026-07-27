using System;
using System.Threading.Tasks;

namespace DormCare.WPF.ViewModels
{
    public class RoomDetailViewModel : BaseViewModel
    {
        private Business.DTOs.RoomDetailDto? _roomDetail;
        public Business.DTOs.RoomDetailDto? RoomDetail
        {
            get => _roomDetail;
            set => SetProperty(ref _roomDetail, value);
        }

        public RoomDetailViewModel(Business.DTOs.RoomDetailDto roomDetail)
        {
            Title = $"Chi Tiết Phòng {roomDetail.RoomNumber}";
            RoomDetail = roomDetail;
        }
    }
}
