// File: MyUserApp/MainWindow.xaml.cs
using MyUserApp.ViewModels; // Required to reference the ViewModels
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MyUserApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        // ===================================================================
        // ==     NEW METHOD: This event fires just before the window closes  ==
        // ===================================================================
        /// <summary>
        /// This method is called when the user tries to close the main application window.
        /// It checks if the current view is the image editor and, if so, triggers a final save.
        /// </summary>
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // 1. Get the MainViewModel from the window's DataContext.
            if (this.DataContext is MainViewModel mainViewModel)
            {
                // 2. Check if the currently displayed view is the ImageEditorViewModel.
                if (mainViewModel.CurrentView is ImageEditorViewModel editorVM)
                {
                    // 3. If it is, call its public SaveAnnotations method to ensure all work is saved.
                    editorVM.SaveAnnotations();
                }
            }
        }
    }
}