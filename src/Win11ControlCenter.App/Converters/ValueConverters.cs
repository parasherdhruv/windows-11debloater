using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using Win11ControlCenter.Core.Enums;

namespace Win11ControlCenter.App.Converters;

public class InverseBooleanToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool b)
        {
            return b ? Visibility.Collapsed : Visibility.Visible;
        }
        return Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Visibility v)
        {
            return v != Visibility.Visible;
        }
        return false;
    }
}

public class InverseBooleanConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is bool b ? !b : false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is bool b ? !b : false;
    }
}

public class CountToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var isInverse = string.Equals(parameter?.ToString(), "Inverse", StringComparison.OrdinalIgnoreCase);
        if (value is int count)
        {
            var isPositive = count > 0;
            var visible = isInverse ? !isPositive : isPositive;
            return visible ? Visibility.Visible : Visibility.Collapsed;
        }
        return isInverse ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class NullOrEmptyToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null) return Visibility.Collapsed;
        if (value is string s && string.IsNullOrWhiteSpace(s)) return Visibility.Collapsed;
        return Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class RiskToBrushConverter : IValueConverter
{
    private static readonly SolidColorBrush SafeBrush = new(Color.FromRgb(0x10, 0xB9, 0x81));
    private static readonly SolidColorBrush ModerateBrush = new(Color.FromRgb(0xF5, 0x9E, 0x0B));
    private static readonly SolidColorBrush HighBrush = new(Color.FromRgb(0xEF, 0x44, 0x44));
    private static readonly SolidColorBrush DefaultBrush = new(Color.FromRgb(0xA0, 0xA0, 0xA0));

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is OperationRiskLevel risk)
        {
            return risk switch
            {
                OperationRiskLevel.Safe => SafeBrush,
                OperationRiskLevel.Moderate => ModerateBrush,
                OperationRiskLevel.Advanced or OperationRiskLevel.Critical => HighBrush,
                _ => DefaultBrush
            };
        }
        return DefaultBrush;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
