using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ChatZ.Client
{
  /// <summary>
  /// Converts UTC DateTime into local DateTime (if possible)
  /// </summary>
  public sealed class LocalTimeUTCConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    { 
      if (value != null && value is DateTimeOffset date)
      {
        var loc = date.ToLocalTime();
        switch (targetType)
        { 
          case Type t when t == typeof(DateTime):       return loc.DateTime;
          case Type t when t == typeof(DateTimeOffset): return loc;
          case Type t when t == typeof(string):         return loc.ToString();
          
          default: return null;
        }
      }
      return null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) 
      => DependencyProperty.UnsetValue;
  }
}
