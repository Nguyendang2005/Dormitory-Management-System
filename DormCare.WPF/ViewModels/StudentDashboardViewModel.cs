using System.Threading.Tasks;
using DormCare.Business.Services;
using DormCare.Domain.Entities;

namespace DormCare.WPF.ViewModels
{
    public class StudentDashboardViewModel : BaseViewModel
    {
        private readonly StudentService _studentService;
        private readonly User _currentUser;

        private Student? _student;
        public Student? Student
        {
            get => _student;
            set => SetProperty(ref _student, value);
        }

        public StudentDashboardViewModel(StudentService studentService, User currentUser)
        {
            Title = "Trang chủ Sinh viên";
            _studentService = studentService;
            _currentUser = currentUser;

            _ = LoadStudentDataAsync();
        }

        public async Task LoadStudentDataAsync()
        {
            IsBusy = true;
            Student = await _studentService.GetStudentByUserIdAsync(_currentUser.UserId);
            IsBusy = false;
        }
    }
}
