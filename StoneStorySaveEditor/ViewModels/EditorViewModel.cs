using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StoneStorySaveEditor.Services;
using System;
using System.Threading.Tasks;

namespace StoneStorySaveEditor.ViewModels;

public partial class EditorViewModel : ViewModelBase
{
    private readonly MainWindowViewModel main;

    [ObservableProperty]
    private string originalText;

    [ObservableProperty]
    private string decryptedText = "";

    [ObservableProperty]
    private string statusText = "Decrypting...";

    [ObservableProperty]
    private bool isBusy = true;

    [ObservableProperty]
    private object? progressObject;  // Json Object

    [ObservableProperty]
    private string progressJson = "";  // Just for debug

    [ObservableProperty]
    private string outputText = "";

    [ObservableProperty]
    private bool hasOutput = false;
    [ObservableProperty]
    private string searchText = "";

    [ObservableProperty]
    private int lastSearchIndex = -1;

    public EditorViewModel(MainWindowViewModel main, string saveText)
    {
        this.main = main;
        originalText = saveText;

        _ = DecryptOnLoadAsync();
    }

    private async Task DecryptOnLoadAsync()
    {
        try
        {
            IsBusy = true;
            StatusText = "Decrypting pasted save text...";

            StatusText = "Decrypting progress_data...";
            string slimJson = await SaveToolRunner.DecryptTextAsync(ExtractProgressDataLine(OriginalText));

            StatusText = "Converting SlimJson to object...";
            ProgressObject = SlimJsonConverter.ToObject(slimJson);

            StatusText = "Formatting JSON...";
            ProgressJson = SlimJsonConverter.ToPrettyJson(ProgressObject);

            DecryptedText = ProgressJson;

            StatusText = $"Decrypted successfully. Length: {DecryptedText.Length:N0} characters.";
        }
        catch (Exception ex)
        {
            StatusText = "Decrypt failed: " + ex.Message;
            DecryptedText = "";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task Export()
    {
        try
        {
            IsBusy = true;
            HasOutput = false;
            OutputText = "";

            StatusText = "Converting edited JSON back to SlimJson...";

            string slimJson = JsonToSlimJsonConverter.FromPrettyJson(DecryptedText);

            StatusText = "Encrypting edited progress_data...";

            string newProgressData = await SaveToolRunner.EncryptTextAsync(slimJson);

            StatusText = "Replacing progress_data in original save text...";

            OutputText = ReplaceProgressData(OriginalText, newProgressData);
            HasOutput = true;

            StatusText = $"Export ready! Copy the following data to clipboard and import it in your game.";
        }
        catch (Exception ex)
        {
            StatusText = "Export failed: " + ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private static string ReplaceProgressData(string originalText, string newProgressData)
    {
        string key = "progress_data:";
        int start = originalText.IndexOf(key, StringComparison.Ordinal);

        if (start < 0)
        {
            throw new Exception("Could not find progress_data:");
        }

        start += key.Length;

        int end = originalText.IndexOf(',', start);

        if (end < 0)
        {
            end = originalText.IndexOf('}', start);
        }

        if (end < 0)
        {
            throw new Exception("Could not find end of progress_data value.");
        }

        return originalText.Substring(0, start)
            + newProgressData
            + originalText.Substring(end);
    }

    private static string ExtractProgressDataLine(string pastedText)
    {
        if (string.IsNullOrWhiteSpace(pastedText))
        {
            throw new ArgumentException("Pasted text is empty.");
        }

        string[] lines = pastedText.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');

        foreach (string rawLine in lines)
        {
            string line = rawLine.Trim();

            if (!line.StartsWith("progress_data:"))
            {
                continue;
            }

            // Remove "progress_data:"
            string value = line.Substring("progress_data:".Length).Trim();

            // Remove trailing comma
            if (value.EndsWith(","))
            {
                value = value.Substring(0, value.Length - 1).Trim();
            }

            // Remove optional quotes, just in case
            value = value.Trim('"');

            if (string.IsNullOrWhiteSpace(value))
            {
                throw new Exception("Found progress_data line, but it was empty.");
            }

            return value;
        }

        throw new Exception("Could not find a line starting with progress_data:");
    }

    [RelayCommand]
    private void Back()
    {
        main.GoBackToPaste(OriginalText);
    }

    [RelayCommand]
    private void Find()
    {
        LastSearchIndex = -1;
        FindNext();
    }

    [RelayCommand]
    private void FindNext()
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            StatusText = "Search text is empty.";
            return;
        }

        if (string.IsNullOrEmpty(DecryptedText))
        {
            StatusText = "No JSON text to search.";
            return;
        }

        int startIndex = LastSearchIndex + 1;

        if (startIndex >= DecryptedText.Length)
        {
            startIndex = 0;
        }

        int index = DecryptedText.IndexOf(
            SearchText,
            startIndex,
            StringComparison.OrdinalIgnoreCase
        );

        if (index < 0 && startIndex > 0)
        {
            index = DecryptedText.IndexOf(
                SearchText,
                0,
                StringComparison.OrdinalIgnoreCase
            );
        }

        if (index < 0)
        {
            StatusText = $"Could not find: {SearchText}";
            return;
        }

        LastSearchIndex = index;
        StatusText = $"Found '{SearchText}' at index {index:N0}.";
    }

    [RelayCommand]
    private void UnlockAllCosmetics()
    {
        try
        {
            StatusText = "Unlocking all cosmetics...";

            DecryptedText = SaveEditService.UnlockAllCosmetics(DecryptedText);

            StatusText = "All cosmetics unlocked. Click Export to generate the final save text.";
        }
        catch (Exception ex)
        {
            StatusText = "Unlock all cosmetics failed: " + ex.Message;
        }
    }
}