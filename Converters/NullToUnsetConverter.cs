using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Dreamine.UI.Wpf.Controls.Converters;

/// <summary>
/// \if KO
/// <para>null 값을 <see cref="DependencyProperty.UnsetValue"/>로 바꾸어 WPF 값 상속 체인을 유지합니다.</para>
/// \endif
/// \if EN
/// <para>Replaces null with <see cref="DependencyProperty.UnsetValue"/> so the WPF value-inheritance chain remains active.</para>
/// \endif
/// </summary>
[ValueConversion(typeof(object), typeof(object))]
public sealed class NullToUnsetConverter : IValueConverter
{
    /// <summary>
    /// \if KO
    /// <para>공유 변환기 인스턴스를 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the shared converter instance.</para>
    /// \endif
    /// </summary>
    public static readonly NullToUnsetConverter Instance = new();

    /// <summary>
    /// \if KO
    /// <para>null 원본을 설정되지 않음 표식으로, 그 외 값은 그대로 반환합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Converts a null source to the unset-value sentinel and returns other values unchanged.</para>
    /// \endif
    /// </summary>
    /// <param name="value">
    /// \if KO
    /// <para>원본 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The source value.</para>
    /// \endif
    /// </param>
    /// <param name="targetType">
    /// \if KO
    /// <para>대상 형식입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The target type.</para>
    /// \endif
    /// </param>
    /// <param name="parameter">
    /// \if KO
    /// <para>사용하지 않는 매개변수입니다.</para>
    /// \endif
    /// \if EN
    /// <para>An unused parameter.</para>
    /// \endif
    /// </param>
    /// <param name="culture">
    /// \if KO
    /// <para>사용하지 않는 문화권입니다.</para>
    /// \endif
    /// \if EN
    /// <para>An unused culture.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>원본 값 또는 <see cref="DependencyProperty.UnsetValue"/>입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The source value or <see cref="DependencyProperty.UnsetValue"/>.</para>
    /// \endif
    /// </returns>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value ?? DependencyProperty.UnsetValue;

    /// <summary>
    /// \if KO
    /// <para>대상 값을 변경하지 않고 원본으로 전달합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Passes the target value back to the source unchanged.</para>
    /// \endif
    /// </summary>
    /// <param name="value">
    /// \if KO
    /// <para>대상 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The target value.</para>
    /// \endif
    /// </param>
    /// <param name="targetType">
    /// \if KO
    /// <para>원본 형식입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The source type.</para>
    /// \endif
    /// </param>
    /// <param name="parameter">
    /// \if KO
    /// <para>사용하지 않는 매개변수입니다.</para>
    /// \endif
    /// \if EN
    /// <para>An unused parameter.</para>
    /// \endif
    /// </param>
    /// <param name="culture">
    /// \if KO
    /// <para>사용하지 않는 문화권입니다.</para>
    /// \endif
    /// \if EN
    /// <para>An unused culture.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>입력 값이며 런타임상 null일 수 있습니다.</para>
    /// \endif
    /// \if EN
    /// <para>The input value, which may be runtime null.</para>
    /// \endif
    /// </returns>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value!;
}
