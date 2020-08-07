using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;

namespace ChatZ.Client
{
  /// <summary>
  /// Returns true is all provided values are equal
  /// </summary>
  public sealed class AllValuesTrueConverter : IMultiValueConverter
  {
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    { 
      if (values?.Length == 0) { return false; }

      if (values.Length == 1) { return true; }
       
      var first = values[0];
      return values.Skip(1).Aggregate(true, (t, v) => t && (v == first));    
    }
    
    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) 
      => (object[]) DependencyProperty.UnsetValue;
  }
}
