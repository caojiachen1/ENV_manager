// SelectScopeWindow.xaml.cs
using System.Windows;

namespace YourNamespace
{
    public partial class SelectScopeWindow : Window
    {
        public event RoutedEventHandler UserSelected;
        public event RoutedEventHandler SystemSelected;

        public SelectScopeWindow()
        {
            InitializeComponent();
        }

        private void UserButton_Click(object sender, RoutedEventArgs e)
        {
            UserSelected?.Invoke(this, e);
            this.Close();
        }

        private void SystemButton_Click(object sender, RoutedEventArgs e)
        {
            SystemSelected?.Invoke(this, e);
            this.Close();
        }
    }
}