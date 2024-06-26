using System.Linq;
using Avalonia.Input;
using Avalonia.ReactiveUI;
using LazerMP3Swap.ViewModels;

namespace LazerMP3Swap.Views;

public partial class MainWindow : ReactiveWindow<MainWindowViewModel>
{
    public MainWindow()
    {
        InitializeComponent();
    }
}
