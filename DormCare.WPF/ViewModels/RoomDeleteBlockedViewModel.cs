using System;
using DormCare.Business.DTOs;

namespace DormCare.WPF.ViewModels
{
    public class RoomDeleteBlockedViewModel : BaseViewModel
    {
        public string RoomNumber { get; }
        public RoomDeleteResult DeleteResult { get; }
        public Action<bool?>? CloseAction { get; set; }

        public RoomDeleteBlockedViewModel(string roomNumber, RoomDeleteResult deleteResult)
        {
            Title = "Không Thể Xóa Phòng";
            RoomNumber = roomNumber;
            DeleteResult = deleteResult;
        }
    }
}
