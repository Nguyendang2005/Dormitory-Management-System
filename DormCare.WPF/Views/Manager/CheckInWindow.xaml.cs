using System.Windows;
using DormCare.WPF.ViewModels;

namespace DormCare.WPF.Views.Manager
{
    public partial class CheckInWindow : Window
    {
        public CheckInWindow(CheckInViewModel viewModel)
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
