using Avalonia.Controls;
using StoneStorySaveEditor.ViewModels;

namespace StoneStorySaveEditor.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainWindowViewModel();
    }
}