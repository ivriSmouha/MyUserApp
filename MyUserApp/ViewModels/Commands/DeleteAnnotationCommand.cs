using MyUserApp.Models;
using System.Collections.ObjectModel;

namespace MyUserApp.ViewModels.Commands
{
    /// <summary>
    /// Implements the IUndoableCommand interface to remove an annotation from a collection.
    /// It stores the annotation and its original index so the operation can be perfectly reversed.
    /// </summary>
    public class DeleteAnnotationCommand : IUndoableCommand
    {
        private readonly ObservableCollection<AnnotationModel> _collection;
        private readonly AnnotationModel _annotationToDelete;
        private int _originalIndex;

        public DeleteAnnotationCommand(ObservableCollection<AnnotationModel> collection, AnnotationModel annotationToDelete)
        {
            _collection = collection;
            _annotationToDelete = annotationToDelete;
        }

        /// <summary>
        /// The 'Do' action: finds the annotation's index, then removes it.
        /// </summary>
        public void Execute()
        {
            // Store the original index before removing, for the undo operation.
            _originalIndex = _collection.IndexOf(_annotationToDelete);
            if (_originalIndex != -1)
            {
                _collection.RemoveAt(_originalIndex);
            }
        }

        /// <summary>
        /// The 'Undo' action: inserts the annotation back into its original position in the list.
        /// </summary>
        public void UnExecute()
        {
            if (_annotationToDelete != null && _originalIndex != -1)
            {
                _collection.Insert(_originalIndex, _annotationToDelete);
            }
        }
    }
}