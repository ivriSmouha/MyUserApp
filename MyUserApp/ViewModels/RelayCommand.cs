// ViewModels/RelayCommand.cs
using System;
using System.Windows.Input;

namespace MyUserApp.ViewModels 
{
    // זוהי מחלקת עזר סטנדרטית שמפשטת את השימוש בפקודות (Commands) ב-MVVM.
    // היא מחברת בין לחיצת כפתור ב-View לבין מתודה ב-ViewModel.
    public class RelayCommand : ICommand
    {
        private readonly Action<object> _execute;
        private readonly Predicate<object> _canExecute;

        public RelayCommand(Action<object> execute, Predicate<object> canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter) => _canExecute == null || _canExecute(parameter);
        public void Execute(object parameter) => _execute(parameter);
        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
    }
}