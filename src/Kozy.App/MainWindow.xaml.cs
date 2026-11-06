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

    private void AuthorCredit_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "https://github.com/mrKozy",
                UseShellExecute = true,
            });
        }
        catch { }
    }
}
