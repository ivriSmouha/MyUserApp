using System;
using System.ComponentModel;
using System.Reflection;

namespace MyUserApp.ViewModels.Commands
{
    /// <summary>
    /// A generic, reusable command for changing a property on a ViewModel in an undoable way.
    /// It uses reflection to set a property by its name and stores the old and new values.
    /// </summary>
    /// <typeparam name="T">The type of the property being changed.</typeparam>
    public class ChangePropertyValueCommand<T> : IUndoableCommand
    {
        private readonly INotifyPropertyChanged _target;
        private readonly PropertyInfo _propertyInfo;
        private readonly T _oldValue;
        private readonly T _newValue;

        /// <summary>
        /// Constructor for the command.
        /// </summary>
        /// <param name="target">The ViewModel object whose property will be changed.</param>
        /// <param name="propertyName">The name of the property to change.</param>
        /// <param name="oldValue">The value of the property before the change.</param>
        /// <param name="newValue">The new value for the property.</param>
        public ChangePropertyValueCommand(INotifyPropertyChanged target, string propertyName, T oldValue, T newValue)
        {
            _target = target;

            _propertyInfo = target.GetType().GetProperty(propertyName) ??
                throw new ArgumentException($"Property '{propertyName}' not found on target type '{target.GetType().Name}'.");

            _oldValue = oldValue;
            _newValue = newValue;
        }

        public void Execute()
        {
            _propertyInfo.SetValue(_target, _newValue);
        }

        public void UnExecute()
        {
            _propertyInfo.SetValue(_target, _oldValue);
        }
    }
}