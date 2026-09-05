using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace GryfLabelManager.ViewModels
{
    /// <summary>
    /// Bazowa klasa dla wszystkich ViewModeli i modeli bindowanych w UI.
    /// [CallerMemberName] sam wstawia nazwę property, więc w setterach
    /// wystarczy wywołać OnPropertyChanged() bez argumentu.
    /// </summary>
    public abstract class BaseViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
