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
        // ==     UPDATED METHOD: This now handles the unsaved changes prompt ==
        // ===================================================================
        /// <summary>
        /// This method is called when the user tries to close the main application window.
        /// It now checks if the Image Editor has unsaved changes and prompts the user to save.
        /// </summary>
        private async void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // 1. Get the MainViewModel from the window's DataContext.
            if (this.DataContext is MainViewModel mainViewModel)
            {
                // 2. Check if the currently displayed view is the ImageEditorViewModel.
                if (mainViewModel.CurrentView is ImageEditorViewModel editorVM)
                {
                    // This is a special flag to prevent the async method from returning control
                    // to the OS and closing the window before we get a response from the user.
                    e.Cancel = true;

                    // 3. Call the ViewModel's new method to see if it's safe to close.
                    //    This method contains the logic to show the "Yes/No/Cancel" dialog if IsDirty is true.
                    bool canClose = await editorVM.CanCloseAsync();

                    // 4. If the user did NOT cancel the operation, close the application.
                    if (canClose)
                    {
                        // We must shut down the application manually here because we initially cancelled the event.
                        Application.Current.Shutdown();
                    }
                    // If canClose is false, it means the user clicked "Cancel", and we do nothing,
                    // leaving them in the editor. The initial e.Cancel = true prevents the close.
                }
            }
        }
    }
}