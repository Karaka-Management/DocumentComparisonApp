using System.Windows;

namespace DocumentComparisonApp.Views
{
    /// <summary>
    /// Interaction logic for Info.xaml
    /// </summary>
    public partial class Info : Window
    {
        public static bool isOpen = false;

        public Info()
        {
            InitializeComponent();
            isOpen = true;
        }

        private void InfoClose_Click(object sender, RoutedEventArgs e)
        {
            isOpen = false;
            this.Close();
        }

        private void InfoWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            isOpen = false;
        }
    }
}
