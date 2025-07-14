using System;
using System.Windows.Input;

namespace RevitAdjustWall.ViewModels;

/// <summary>
/// A command implementation that relays its functionality to delegates
/// Implements the Command pattern for MVVM with proper nullable reference type support
/// </summary>
public class RelayCommand : ICommand
{
    private readonly Action<object> _execute;
    private readonly Predicate<object>? _canExecute;

    /// <summary>
    /// Initializes a new instance of RelayCommand
    /// </summary>
    /// <param name="execute">The execution logic</param>
    /// <param name="canExecute">The execution status logic</param>
    public RelayCommand(Action<object>? execute, Predicate<object>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    /// <summary>
    /// Initializes a new instance of RelayCommand with no parameter
    /// </summary>
    /// <param name="execute">The execution logic</param>
    /// <param name="canExecute">The execution status logic</param>
    public RelayCommand(Action? execute, Func<bool>? canExecute = null)
        : this(
            execute != null ? new Action<object>(_ => execute()) : null,
            canExecute != null ? new Predicate<object>(_ => canExecute()) : null)
    {
    }

    /// <summary>
    /// Occurs when changes occur that affect whether or not the command should execute
    /// </summary>
    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    public bool CanExecute(object? parameter)
    {
        return _canExecute?.Invoke(parameter) ?? true;
    }

    public void Execute(object parameter)
    {
        _execute(parameter);
    }

    /// <summary>
    /// Raises the CanExecuteChanged event to notify the UI that the command's execution state may have changed
    /// </summary>
    public void RaiseCanExecuteChanged()
    {
        CommandManager.InvalidateRequerySuggested();
    }
}