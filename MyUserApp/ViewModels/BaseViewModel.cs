using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// ViewModels/BaseViewModel.cs
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MyUserApp.ViewModels
{
    // זוהי מחלקת בסיס שכל ה-ViewModels שלנו יירשו ממנה.
    // היא מממשת ממשק סטנדרטי שמאפשר ל-ViewModel להודיע ל-View על שינויים.
    public class BaseViewModel : INotifyPropertyChanged
    {
        // אירוע שה-View "מאזין" לו כדי לדעת מתי לעדכן את עצמו.
        public event PropertyChangedEventHandler PropertyChanged;

        // מתודה שמפעילה את האירוע. אנו קוראים לה בכל פעם שערך של מאפיין משתנה.
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
