using MyUserApp.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyUserApp.Services
{
    /// <summary>
    /// Defines the contract for a service that can generate or retrieve annotations for a given image.
    /// Using an interface allows us to easily swap implementations (e.g., from a mock service to a real AI service)
    /// without changing the ViewModel.
    /// </summary>
    public interface IAnnotationService
    {
        /// <summary>
        /// Asynchronously gets annotations for a specific image file.
        /// </summary>
        /// <param name="imagePath">The file path of the image to analyze.</param>
        /// <returns>A task that represents the asynchronous operation, yielding a list of annotations.</returns>
        Task<List<AnnotationModel>> GetAnnotationsAsync(string imagePath);
    }
}