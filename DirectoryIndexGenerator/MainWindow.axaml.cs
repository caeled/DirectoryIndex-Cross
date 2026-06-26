using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using DirectoryIndexGenerator.ViewModels;

namespace DirectoryIndexGenerator;

public partial class MainWindow : Window
{
    private MainViewModel Vm => (MainViewModel)DataContext!;

    public MainWindow()
    {
        InitializeComponent();

        // Wire up drag-and-drop on the named DropZone border
        var dropZone = this.FindControl<Border>("DropZone")!;
        DragDrop.SetAllowDrop(dropZone, true);
        dropZone.AddHandler(DragDrop.DropEvent,     OnDrop);
        dropZone.AddHandler(DragDrop.DragOverEvent, OnDragOver);
        dropZone.AddHandler(DragDrop.DragEnterEvent, OnDragEnter);
        dropZone.AddHandler(DragDrop.DragLeaveEvent, OnDragLeave);

        // Give the ViewModel a reference for file/folder dialogs
        Vm.SetWindow(this);
    }

    private void OnDragEnter(object? sender, DragEventArgs e)
    {
        if (e.Data.Contains(DataFormats.Files))
            Vm.IsDragOver = true;
    }

    private void OnDragLeave(object? sender, RoutedEventArgs e)
    {
        Vm.IsDragOver = false;
    }

    private void OnDragOver(object? sender, DragEventArgs e)
    {
        e.DragEffects = e.Data.Contains(DataFormats.Files)
            ? DragDropEffects.Copy
            : DragDropEffects.None;
        e.Handled = true;
    }

    private void OnDrop(object? sender, DragEventArgs e)
    {
        Vm.IsDragOver = false;
        if (!e.Data.Contains(DataFormats.Files)) return;

        var files = e.Data.GetFiles();
        if (files is null) return;

        foreach (var f in files)
        {
            var path = f.TryGetLocalPath();
            if (path != null && Directory.Exists(path))
            {
                Vm.SetFolder(path);
                break;
            }
        }
        e.Handled = true;
    }
}
