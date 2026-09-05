using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace GryfLabelManager.Helpers
{
    /// <summary>
    /// Porównuje wartość enuma (np. ViewMode) z parametrem tekstowym z XAML
    /// i zwraca Visibility. Używane do pokazywania/ukrywania paneli zależnych od trybu.
    /// Użycie w XAML:
    /// Visibility="{Binding CurrentMode, Converter={StaticResource EnumToVisibility}, ConverterParameter=Dokumenty}"
    /// </summary>
    public class EnumToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null) return Visibility.Collapsed;
            return value.ToString() == parameter.ToString() ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    /// <summary>
    /// Porównuje enum z parametrem i zwraca bool - do bindowania RadioButton.IsChecked
    /// do wartości enuma (żeby zaznaczony przycisk odzwierciedlał aktualny tryb).
    /// </summary>
    public class EnumToBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value?.ToString() == parameter?.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Kliknięcie RadioButtona ustawia z powrotem wartość enuma na podstawie parametru
            return (value is bool isChecked && isChecked) ? Enum.Parse(targetType, parameter.ToString()) : Binding.DoNothing;
        }
    }
}
