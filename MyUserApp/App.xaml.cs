// File: MyUserApp/App.xaml.cs

using System.Windows;
using FellowOakDicom;
using FellowOakDicom.Imaging;

namespace MyUserApp
{
    public partial class App : Application
    {
        public App()
        {
            new DicomSetupBuilder()
          .RegisterServices(s => s.AddFellowOakDicom().AddImageManager<RawImageManager>())
          .Build();
        }
    }
}