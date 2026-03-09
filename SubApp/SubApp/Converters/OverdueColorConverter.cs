using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace SubApp.Converters;

public class OverdueColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var isOverdue = value is bool b and true;
        return isOverdue ? Brush.Parse("#FF453A") : Brush.Parse("#2C2C2E");
    }
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => null;
}