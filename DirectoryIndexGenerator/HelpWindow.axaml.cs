using Avalonia.Controls;
using Avalonia.Interactivity;

namespace DirectoryIndexGenerator;

public partial class HelpWindow : Window
{
    public HelpWindow() => InitializeComponent();
    private void Close_Click(object? sender, RoutedEventArgs e) => Close();
}
