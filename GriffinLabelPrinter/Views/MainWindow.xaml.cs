using System.Windows;
using GryfLabelManager.ViewModels;

namespace GryfLabelManager.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow(MainViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}
