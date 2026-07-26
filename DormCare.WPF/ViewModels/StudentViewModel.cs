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
    public class StudentViewModel : BaseViewModel
    {
        private readonly StudentService _studentService;

        private ObservableCollection<StudentDto> _allStudents = new();

        private ObservableCollection<StudentDto> _students = new();
        public ObservableCollection<StudentDto> Students
        {
            get => _students;
            set => SetProperty(ref _students, value);
        }

        private StudentDto? _selectedStudent;
        public StudentDto? SelectedStudent
        {
            get => _selectedStudent;
            set => SetProperty(ref _selectedStudent, value);
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

        public ICommand RefreshCommand { get; }

        public StudentViewModel(StudentService studentService)
        {
            Title = "Quản lý Sinh viên";
            _studentService = studentService;

            RefreshCommand = new AsyncRelayCommand(LoadStudentsAsync);
            _ = LoadStudentsAsync();
        }

        public async Task LoadStudentsAsync()
        {
            IsBusy = true;
            var dtos = await _studentService.GetAllStudentsAsync();
            _allStudents = new ObservableCollection<StudentDto>(dtos);
            ApplyFilters();
            IsBusy = false;
        }

        private void ApplyFilters()
        {
            var query = _allStudents.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                query = query.Where(s => s.FullName.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                                         s.StudentCode.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                                         s.Major.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                                         s.ClassName.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
            }

            Students = new ObservableCollection<StudentDto>(query);
        }
    }
}
