using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace StoneStorySaveEditor.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private string saveText = "";

    [ObservableProperty]
    private string statusText = "Ready. Paste save text into the box.";

    [RelayCommand]
    private void Clear()
    {
        SaveText = "";
        StatusText = "Cleared.";
    }

    [RelayCommand]
    private void Next()
    {
        if (string.IsNullOrWhiteSpace(SaveText))
        {
            StatusText = "Paste save text first.";
            return;
        }

        if (SaveText.Contains("STRING_KEYS:"))
        {
            StatusText = $"Looks like full export text. Length: {SaveText.Length:N0} characters.";
        }
        else if (SaveText.Contains("progress_data:") || SaveText.Contains("\"progress_data\""))
        {
            StatusText = $"Looks like save metadata with progress_data. Length: {SaveText.Length:N0} characters.";
        }
        else
        {
            StatusText = $"Text loaded, but format is unknown. Length: {SaveText.Length:N0} characters.";
        }
    }
}