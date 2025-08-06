using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyUserApp.ViewModels.Commands
{
    /// <summary>
    /// Defines the contract for a command that can be executed and un-executed.
    /// This is the core of the Undo/Redo system, following the Command Pattern.
    /// Each user action (like adding or deleting an annotation) will be encapsulated
    /// in a class that implements this interface.
    /// </summary>
    public interface IUndoableCommand
    {
        /// <summary>
        /// Performs the action.
        /// </summary>
        void Execute();

        /// <summary>
        /// Reverses the action, restoring the state to before it was executed.
        /// </summary>
        void UnExecute();
    }
}