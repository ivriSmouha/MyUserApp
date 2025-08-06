using MyUserApp.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyUserApp.Services
{
    /// <summary>
    /// A mock implementation of the IAnnotationService.
    /// This class simulates a call to a real AI service by returning a hard-coded
    /// list of annotations after a simulated delay. It is used for development and testing.
    /// </summary>
    public class MockAnnotationService : IAnnotationService
    {
        public async Task<List<AnnotationModel>> GetAnnotationsAsync(string imagePath)
        {
            // Simulate network latency or processing time of 1.5 seconds.
            // 'await' ensures this operation does not block the UI thread.
            await Task.Delay(1500);

            // In a real application, this is where you would call your AI model/API.
            // For now, we return a predefined list of "AI-found" issues.
            var aiAnnotations = new List<AnnotationModel>
            {
                new AnnotationModel
                {
                    Author = AuthorType.AI,
                    CenterX = 0.25,
                    CenterY = 0.3,
                    Radius = 0.05
                },
                new AnnotationModel
                {
                    Author = AuthorType.AI,
                    CenterX = 0.7,
                    CenterY = 0.8,
                    Radius = 0.08
                }
            };

            // Randomly decide if the AI finds anything, to make it more realistic.
            if (new Random().Next(0, 2) > 0)
            {
                return aiAnnotations;
            }
            else
            {
                return new List<AnnotationModel>(); // AI found nothing.
            }
        }
    }
}