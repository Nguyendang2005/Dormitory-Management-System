using System.Windows;

namespace DormCare.WPF.Views.Manager
{
    public partial class BuildingDeleteBlockedWindow : Window
    {
        public BuildingDeleteBlockedWindow()
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
