using System.Windows;
using System.Windows.Controls;
using Kozy.App.ViewModels;
using Wpf.Ui.Controls;

namespace Kozy.App;

public partial class MainWindow : FluentWindow
{
    private readonly MainViewModel _viewModel;

    public MainWindow()
    {
        InitializeComponent();
        _viewModel = new MainViewModel();
        DataContext = _viewModel;
    }

    private void NavButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is RadioButton { Tag: string tag })
        {
            _viewModel.NavigateTo(tag);
        }
    }
}
