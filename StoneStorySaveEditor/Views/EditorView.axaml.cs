using Avalonia.Controls;
using Avalonia.Input;
using StoneStorySaveEditor.ViewModels;
using System;

namespace StoneStorySaveEditor.Views;

public partial class EditorView : UserControl
{
    private int lastSearchIndex = -1;

    public EditorView()
    {
        InitializeComponent();
        KeyDown += OnKeyDown;
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.F && e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            SearchBox.Focus();
            SearchBox.SelectAll();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Enter && SearchBox.IsFocused)
        {
            FindNext();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.F3)
        {
            FindNext();
            e.Handled = true;
        }
    }

    private void FindNext()
    {
        string query = SearchBox.Text ?? "";
        string text = JsonEditor.Text ?? "";

        if (string.IsNullOrWhiteSpace(query) || string.IsNullOrEmpty(text))
        {
            return;
        }

        int start = lastSearchIndex + 1;

        if (start >= text.Length)
        {
            start = 0;
        }

        int index = text.IndexOf(query, start, StringComparison.OrdinalIgnoreCase);

        if (index < 0 && start > 0)
        {
            index = text.IndexOf(query, 0, StringComparison.OrdinalIgnoreCase);
        }

        if (index < 0)
        {
            if (DataContext is EditorViewModel vm)
            {
                vm.StatusText = $"Could not find: {query}";
            }

            return;
        }

        lastSearchIndex = index;

        JsonEditor.Focus();
        JsonEditor.SelectionStart = index;
        JsonEditor.SelectionEnd = index + query.Length;

        if (DataContext is EditorViewModel vm2)
        {
            vm2.StatusText = $"Found '{query}' at index {index:N0}.";
        }
    }

    private void FindButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        lastSearchIndex = -1;
        FindNext();
    }

    private void FindNextButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        FindNext();
    }
}