using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace StoneStorySaveEditor.ViewModels;

public partial class PasteSaveViewModel : ViewModelBase
{
    private readonly MainWindowViewModel main;

    [ObservableProperty]
    private string saveText = "";

    [ObservableProperty]
    private string statusText = "Ready. Paste save text into the box.";

    public PasteSaveViewModel(MainWindowViewModel main)
    {
        this.main = main;
    }

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

        if (ImportDataValid(SaveText))
        {
            StatusText = $"Looks like full export text. Length: {SaveText.Length:N0} characters.";
        }
        else
        {
            StatusText = $"Text loaded, but format looks invalid";
            return;
        }

        main.GoToEditor(SaveText);
    }

    // Source Code from StoneStory
    private bool ImportDataValid(string text)
	{
		if (text != null && text.Length > 200)
		{
			text = text.Trim();
            text = text.Replace("\r", "");
            text = text.Replace("\n", "");
			if (text.StartsWith('{') && text.EndsWith('}'))
			{
				if (text.Contains("STRING_KEYS:"))
				{
					return true;
				}
			}
		}
		return false;
	}
}