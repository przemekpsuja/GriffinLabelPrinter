using Wpf.Ui.Controls;
using GryfLabelManager.ViewModels;

namespace GryfLabelManager.Views
{
    public partial class MainWindow : FluentWindow
    {
        public MainWindow(MainViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}
