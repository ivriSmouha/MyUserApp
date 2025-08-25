using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyUserApp.Models
{
    /// <summary>
    /// Holds the brightness and contrast settings for an image.
    /// </summary>
    public class ImageAdjustmentModel
    {
        /// <summary>
        /// The brightness level for the image. Default is 0f (no change).
        /// </summary>
        public float Brightness { get; set; } = 0f;

        /// <summary>
        /// The contrast level for the image. Default is 1f (no change).
        /// </summary>
        public float Contrast { get; set; } = 1f;
    }
}