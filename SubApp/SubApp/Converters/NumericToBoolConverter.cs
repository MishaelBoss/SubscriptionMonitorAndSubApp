using System;
using System.Collections;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;

namespace SubApp.Converters;

public class NumericToBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is ICollection collection)
        {
            return collection.Count > 0;
        }
        
        return false;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return AvaloniaProperty.UnsetValue;
    }
}