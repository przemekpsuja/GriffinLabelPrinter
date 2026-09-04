using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace GryfLabelManager.ViewModels
{
    // Bazowa klasa dla wszystkich ViewModeli - odchudza kod, żeby nie powtarzać
    // implementacji INotifyPropertyChanged w każdym ViewModelu osobno.
    public class BaseViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}