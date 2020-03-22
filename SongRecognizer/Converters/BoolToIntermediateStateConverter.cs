using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Shell;

namespace SongRecognizer.Converters
{
    [ValueConversion(typeof(bool), typeof(TaskbarItemProgressState))]
    public class BoolToIntermediateStateConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool)value ? TaskbarItemProgressState.Indeterminate : TaskbarItemProgressState.None;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
