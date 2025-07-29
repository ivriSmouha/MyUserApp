using MyUserApp.Models;
using MyUserApp.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;

namespace MyUserApp.ViewModels
{
    public class AdminPanelViewModel : BaseViewModel
    {
        public ObservableCollection<UserModel> Users => UserService.Instance.Users;
        private string _newUsername;
        public string NewUsername { get => _newUsername; set { _newUsername = value; OnPropertyChanged(); } }
        private string _newPassword;
        public string NewPassword { get => _newPassword; set { _newPassword = value; OnPropertyChanged(); } }
        private bool _newUserIsAdmin;
        public bool NewUserIsAdmin { get => _newUserIsAdmin; set { _newUserIsAdmin = value; OnPropertyChanged(); } }
        public AppOptionsModel AppOptions => OptionsService.Instance.Options;
        private string _newAircraftType;
        public string NewAircraftType { get => _newAircraftType; set { _newAircraftType = value; OnPropertyChanged(); } }
        private string _newTailNumber;
        public string NewTailNumber { get => _newTailNumber; set { _newTailNumber = value; OnPropertyChanged(); } }
        private string _newAircraftSide;
        public string NewAircraftSide { get => _newAircraftSide; set { _newAircraftSide = value; OnPropertyChanged(); } }
        private string _newReason;
        public string NewReason { get => _newReason; set { _newReason = value; OnPropertyChanged(); } }
        public ICommand AddUserCommand { get; }
        public ICommand LogoutCommand { get; }
        public ICommand AddAircraftTypeCommand { get; }
        public ICommand DeleteAircraftTypeCommand { get; }
        public ICommand AddTailNumberCommand { get; }
        public ICommand DeleteTailNumberCommand { get; }
        public ICommand AddAircraftSideCommand { get; }
        public ICommand DeleteAircraftSideCommand { get; }
        public ICommand AddReasonCommand { get; }
        public ICommand DeleteReasonCommand { get; }
        public event Action OnLogoutRequested;

        public AdminPanelViewModel()
        {
            AddUserCommand = new RelayCommand(AddNewUser, _ => CanAddNewUser());
            LogoutCommand = new RelayCommand(param => OnLogoutRequested?.Invoke());
            AddAircraftTypeCommand = new RelayCommand(AddAircraftType, _ => !string.IsNullOrEmpty(NewAircraftType));
            DeleteAircraftTypeCommand = new RelayCommand(DeleteAircraftType);
            AddTailNumberCommand = new RelayCommand(AddTailNumber, _ => !string.IsNullOrEmpty(NewTailNumber));
            DeleteTailNumberCommand = new RelayCommand(DeleteTailNumber);
            AddAircraftSideCommand = new RelayCommand(AddAircraftSide, _ => !string.IsNullOrEmpty(NewAircraftSide));
            DeleteAircraftSideCommand = new RelayCommand(DeleteAircraftSide);
            AddReasonCommand = new RelayCommand(AddReason, _ => !string.IsNullOrEmpty(NewReason));
            DeleteReasonCommand = new RelayCommand(DeleteReason);
        }

        private bool CanAddNewUser() => !string.IsNullOrEmpty(NewUsername) && !string.IsNullOrEmpty(NewPassword);
        private void AddNewUser(object obj)
        {
            var newUser = new UserModel { Username = this.NewUsername, Password = this.NewPassword, IsAdmin = this.NewUserIsAdmin };
            UserService.Instance.AddUser(newUser);
            NewUsername = ""; NewPassword = ""; NewUserIsAdmin = false;
        }
        private void AddAircraftType(object obj)
        { OptionsService.Instance.Options.AircraftTypes.Add(NewAircraftType); OptionsService.Instance.SaveOptions(); NewAircraftType = ""; OnPropertyChanged(nameof(AppOptions)); }
        private void DeleteAircraftType(object obj)
        { if (obj is string toDelete) { OptionsService.Instance.Options.AircraftTypes.Remove(toDelete); OptionsService.Instance.SaveOptions(); OnPropertyChanged(nameof(AppOptions)); } }
        private void AddTailNumber(object obj)
        { OptionsService.Instance.Options.TailNumbers.Add(NewTailNumber); OptionsService.Instance.SaveOptions(); NewTailNumber = ""; OnPropertyChanged(nameof(AppOptions)); }
        private void DeleteTailNumber(object obj)
        { if (obj is string toDelete) { OptionsService.Instance.Options.TailNumbers.Remove(toDelete); OptionsService.Instance.SaveOptions(); OnPropertyChanged(nameof(AppOptions)); } }
        private void AddAircraftSide(object obj)
        { OptionsService.Instance.Options.AircraftSides.Add(NewAircraftSide); OptionsService.Instance.SaveOptions(); NewAircraftSide = ""; OnPropertyChanged(nameof(AppOptions)); }
        private void DeleteAircraftSide(object obj)
        { if (obj is string toDelete) { OptionsService.Instance.Options.AircraftSides.Remove(toDelete); OptionsService.Instance.SaveOptions(); OnPropertyChanged(nameof(AppOptions)); } }
        private void AddReason(object obj)
        { OptionsService.Instance.Options.Reasons.Add(NewReason); OptionsService.Instance.SaveOptions(); NewReason = ""; OnPropertyChanged(nameof(AppOptions)); }
        private void DeleteReason(object obj)
        { if (obj is string toDelete) { OptionsService.Instance.Options.Reasons.Remove(toDelete); OptionsService.Instance.SaveOptions(); OnPropertyChanged(nameof(AppOptions)); } }
    }
}