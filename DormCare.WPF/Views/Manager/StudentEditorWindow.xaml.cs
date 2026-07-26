using System.Windows;
using DormCare.WPF.ViewModels;

namespace DormCare.WPF.Views.Manager
{
    public partial class StudentEditorWindow : Window
    {
        public StudentEditorWindow(StudentEditorViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
            viewModel.RequestClose += saved =>
            {
                DialogResult = saved;
                Close();
            };
        }
    }
}
