using System;
using System.Windows.Input;

namespace MyUserApp.ViewModels
{
    /// <summary>
    /// A standard, reusable implementation of the ICommand interface.
    /// It relays the Execute and CanExecute logic to delegates provided
    /// during its construction.
    /// </summary>
    public class RelayCommand : ICommand
    {
        // The action to execute when the command is invoked.
        private readonly Action<object> _execute;

        // The function to determine if the command can be executed.
        private readonly Predicate<object> _canExecute;

        /// <summary>
        /// Occurs when changes happen that affect whether the command should execute.
        /// This event is wired to the CommandManager's RequerySuggested event,
        /// which automatically detects conditions like focus changes in the UI.
        /// </summary>
        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        /// <summary>
        /// Initializes a new instance of the RelayCommand.
        /// </summary>
        /// <param name="execute">The execution logic.</param>
        /// <param name="canExecute">The execution status logic.</param>
        public RelayCommand(Action<object> execute, Predicate<object> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        /// <summary>
        /// Defines the method that determines whether the command can execute in its current state.
        /// </summary>
        /// <param name="parameter">Data used by the command. Can be null.</param>
        /// <returns>true if this command can be executed; otherwise, false.</returns>
        public bool CanExecute(object parameter)
        {
            return _canExecute == null || _canExecute(parameter);
        }

        /// <summary>
        /// Defines the method to be called when the command is invoked.
        /// </summary>
        /// <param name="parameter">Data used by the command. Can be null.</param>
        public void Execute(object parameter)
        {
            _execute(parameter);
        }

        /// <summary>
        /// Manually raises the CanExecuteChanged event to force the UI
        /// to re-evaluate the CanExecute status of the command.
        /// </summary>
        public void RaiseCanExecuteChanged()
        {
            CommandManager.InvalidateRequerySuggested();
        }
    }
}