using System.Windows;

namespace DormCare.WPF.Views.Manager
{
    public partial class RoomDetailWindow : Window
    {
        public RoomDetailWindow()
        {
            InitializeComponent();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
