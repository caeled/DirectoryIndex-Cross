using System.Windows.Input;

namespace DirectoryIndexGenerator.ViewModels;

public class RelayCommand(Action execute, Func<bool>? canExecute = null) : ICommand
{
    public event EventHandler? CanExecuteChanged;
    public bool CanExecute(object? parameter) => canExecute?.Invoke() ?? true;
    public void Execute(object? parameter) => execute();
    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}

public class AsyncRelayCommand(Func<Task> execute, Func<bool>? canExecute = null) : ICommand
{
    private bool _isRunning;
    public event EventHandler? CanExecuteChanged;
    public bool CanExecute(object? parameter) => !_isRunning && (canExecute?.Invoke() ?? true);
    public void Execute(object? parameter) => ExecuteAsync();
    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);

    private async void ExecuteAsync()
    {
        _isRunning = true;
        RaiseCanExecuteChanged();
        try { await execute(); }
        finally
        {
            _isRunning = false;
            RaiseCanExecuteChanged();
        }
    }
}
