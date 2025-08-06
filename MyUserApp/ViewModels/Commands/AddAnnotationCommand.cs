using MyUserApp.Models;
using System.Collections.ObjectModel;

namespace MyUserApp.ViewModels.Commands
{
    /// <summary>
    /// Implements the IUndoableCommand interface to add an annotation to a collection.
    /// This object holds all the information needed to both perform and reverse the action.
    /// </summary>
    public class AddAnnotationCommand : IUndoableCommand
    {
        private readonly ObservableCollection<AnnotationModel> _collection;
        private readonly AnnotationModel _annotationToAdd;

        public AddAnnotationCommand(ObservableCollection<AnnotationModel> collection, AnnotationModel annotationToAdd)
        {
            _collection = collection;
            _annotationToAdd = annotationToAdd;
        }

        /// <summary>
        /// The 'Do' action: adds the annotation to the list.
        /// </summary>
        public void Execute()
        {
            _collection.Add(_annotationToAdd);
        }

        /// <summary>
        /// The 'Undo' action: removes the annotation from the list.
        /// </summary>
        public void UnExecute()
        {
            _collection.Remove(_annotationToAdd);
        }
    }
}