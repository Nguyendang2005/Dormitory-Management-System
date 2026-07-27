using System.Windows;
using DormCare.WPF.ViewModels;

namespace DormCare.WPF.Views.Manager
{
    public partial class CheckInWindow : Window
    {
        public CheckInWindow()
        {
            InitializeComponent();
        }

        public CheckInWindow(CheckInViewModel viewModel) : this()
        {
            DataContext = viewModel;
            viewModel.RequestClose += saved =>
            {
                DialogResult = saved;
                Close();
            };
        }
    }
}
