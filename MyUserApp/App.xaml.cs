// File: MyUserApp/App.xaml.cs

using System.Configuration;
using System.Data;
using System.IO; // <-- Add this using statement
using System;    // <-- Add this using statement
using System.Windows;

namespace MyUserApp
{
    public partial class App : Application
    {
        public App()
        {
            //// ===================================================================
            //// ==                 TEMPORARY SHADER CREATION CODE                ==
            //// ===================================================================
            //// This code will run ONCE to create the compiled shader file.
            //// After following all the steps, you will REMOVE this block.

            //string effectsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Effects");
            //Directory.CreateDirectory(effectsDir);
            //string shaderPath = Path.Combine(effectsDir, "BrightnessEffect.ps");

            //// This long string is the pre-compiled binary code for your shader.
            //string shaderBase64 = "AgAAADsTIBECAQAAAAEAAAAIAAAAAAAAABAAAAAFAAAARkZGRgMAAAAAAAAAAQAAAAAAAAAkAAAAYzAsIGZsb2F0AHN0LCBzYW1wbGVyMkQARkZGRgQAAAABAAAAAAAAAAAAAAAkAAAATWljcm9zb2Z0IChSKSBITFNMIFNoYWRlciBDb21waWxlciA5LjI5Ljk1Mi4zMTExAKsBAAACAAEAAACADwEAAACgDwIAAAAAgAAFAAEAgA/kpgAAoA+IAQAIAAAP5AIAAAEAAACgDwgA//8AAA==";

            //File.WriteAllBytes(shaderPath, Convert.FromBase64String(shaderBase64));

            //// ===================================================================
            //// ==                    END OF TEMPORARY CODE                    ==
            //// ===================================================================
        }
    }
}