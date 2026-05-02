using CommunityToolkit.Mvvm.ComponentModel;

namespace StoneStorySaveEditor.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private ViewModelBase currentPage;

    public MainWindowViewModel()
    {
        CurrentPage = new PasteSaveViewModel(this);
    }

    public void GoToEditor(string saveText)
    {
        CurrentPage = new EditorViewModel(this, saveText);
    }

    public void GoBackToPaste(string saveText)
    {
        CurrentPage = new PasteSaveViewModel(this)
        {
            SaveText = saveText
        };
    }
}