using System.Windows;

namespace DormCare.WPF.Views.Manager
{
    public partial class BuildingDetailWindow : Window
    {
        public BuildingDetailWindow()
        {
            InitializeComponent();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
