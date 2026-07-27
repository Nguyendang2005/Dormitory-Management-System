using System.Windows;
using DormCare.WPF.ViewModels;

namespace DormCare.WPF.Views.Manager
{
    public partial class RoomDeleteBlockedWindow : Window
    {
        public RoomDeleteBlockedWindow()
        {
            InitializeComponent();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void Deactivate_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}
