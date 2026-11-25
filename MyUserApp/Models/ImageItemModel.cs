using MyUserApp.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace MyUserApp.Models
{
    public class ImageItemModel : BaseViewModel
    {
        public string FilePath { get; set; }

        private string _status = "שמיש";
        public string Status
        {
            get => _status;
            set { _status = value; OnPropertyChanged(); }
        }

        // תיקון: הגדרת המאפיין בצורה תקינה עם OnPropertyChanged כדי שה-WPF יעביר את ה-V
        private bool _isSelectedForExport;
        public bool IsSelectedForExport
        {
            get => _isSelectedForExport;
            set
            {
                if (_isSelectedForExport == value) return;
                _isSelectedForExport = value;
                OnPropertyChanged(); // חובה כדי שה-ViewModel יזהה את השינוי
            }
        }

        public ICommand ToggleStatusCommand { get; }

        public ImageItemModel(string path, Action setDirtyAction)
        {
            FilePath = path;
            ToggleStatusCommand = new RelayCommand(_ =>
            {
                Status = Status == "שמיש" ? "גו\"ז" : "שמיש";
                setDirtyAction?.Invoke();
            });
        }
    }
}
