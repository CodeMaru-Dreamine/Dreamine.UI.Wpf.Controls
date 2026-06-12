using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Dreamine.UI.Wpf.Controls.Converters;

/// <summary>
/// null이면 DependencyProperty.UnsetValue를 반환하여 WPF 상속(Inheritance) 체인이 동작하도록 합니다.
/// </summary>
[ValueConversion(typeof(object), typeof(object))]
public sealed class NullToUnsetConverter : IValueConverter
{
    public static readonly NullToUnsetConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value ?? DependencyProperty.UnsetValue;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value!;
}
