using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;

namespace CrossfadER;

public partial class MainWindow : Window
{
    private string selectedFilePath;
    
    public static ProgressBar progressBar;
    public static TextBlock statusTextBlock;
    public static TextBlock note;
    
    public MainWindow()
    {
        InitializeComponent();

        progressBar = ProgressBar;
        statusTextBlock = StatusDisplayText;
        note = Note;
        
        ProgressBar.Value = 0;
        
        FileSelectionButton.Click += FileSelectionButton_OnClick;
        ExportButton.Click += ExportButton_Click;
    }

    private async void FileSelectionButton_OnClick(object? sender, RoutedEventArgs e)
    {
        Console.WriteLine("FileSelectionButton_OnClick");

        Note.Text = String.Empty;

        var selectedFile = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions()
        {
            Title = "Select an Audio File",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Audio File")
                {
                    Patterns = new[] { "*.wav" , "*.mp3"},
                },
            }
        });

        if (selectedFile.Any())
        {
            var file = selectedFile.First();
            var localPath = file.TryGetLocalPath();

            if (localPath != null)
            {
                selectedFilePath = localPath;
                SelectedFileTextDisplay.Text = selectedFilePath;
            }
            else
            {
                SelectedFileTextDisplay.Text = "File error";
            }
        }

        ProgressBar.IsVisible = false;
        StatusDisplayText.Text = string.Empty;
    }

    private void ExportButton_Click(object? sender, RoutedEventArgs e)
    {
        Console.WriteLine("ExportButton_Click");
        
        Note.Text = String.Empty;
        ProgressBar.IsVisible = false;
        StatusDisplayText.Text = string.Empty;
        
        if (string.IsNullOrEmpty(selectedFilePath))
        {
            Note.Text = "Please select a file to export";
            return;
        }
        
        var crossfadeTime = (CrossfadeTime.SelectedItem as ComboBoxItem)?.Content?.ToString();
        string fadeTime =  crossfadeTime ?? string.Empty;
        var crossfadeMode = (CrossfadeMode.SelectedItem as ComboBoxItem)?.Content?.ToString();
        string fadeMode = crossfadeMode ?? string.Empty;
        
        
        
        Console.WriteLine($"Cross fade mode: {crossfadeMode}");
        Console.WriteLine($"Cross fade time: {crossfadeTime}");
        
        string outputName = (OutputNameBox.Text == null) ? AutoNaming(selectedFilePath, fadeMode) : CustomizedNaming(selectedFilePath, OutputNameBox.Text);
        
        CrossfadeHandler.Execute(selectedFilePath, fadeTime, GetCrossfadeMode(fadeMode), outputName);
        
        ProgressBar.IsVisible = true;
    }

    private string AutoNaming(string filePath, string fadeMode)
    {
        return $"{System.IO.Path.GetDirectoryName(filePath)}\\{System.IO.Path.GetFileNameWithoutExtension(filePath)}_crossfaded_({fadeMode}).wav";
    }

    private string CustomizedNaming(string filePath, string outputName)
    {
        return $"{System.IO.Path.GetDirectoryName(filePath)}\\{outputName}.wav";
    }

    private CrossfadeMode GetCrossfadeMode(string crossfadeMode)
    {
        switch (crossfadeMode)
        {
            case "Linear":
                return CrossfadER.CrossfadeMode.LINEAR;
            case "Logarithmic":
                return CrossfadER.CrossfadeMode.LOG;
            case "Sine":
                return CrossfadER.CrossfadeMode.SINE;
            default:
                break;
        }

        return CrossfadER.CrossfadeMode.LINEAR;
    }

    public static void UpdateProgressBar(float progress, string? statusText, ProgressBar? progressBar, TextBlock textBlock)
    {
        Dispatcher.UIThread.Post(() =>
        {
            try
            {
                progressBar.Value = progress * 100;
                if (statusText != null)
                    textBlock.Text = statusText;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        });
    }

    public static void UpdateNote(string? noteText, TextBlock textBlock)
    {
        Dispatcher.UIThread.Post(() =>
            {
                try
                {
                    if(noteText != null) 
                        textBlock.Text = noteText;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
        );
    }
}