using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace RdaWizard.App.Converters;

/// <summary>Formats byte counts as human-readable sizes (KB/MB/GB/TB, 1 divisor 1024).</summary>
public sealed class ByteSizeConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is long bytes)
        {
            return FormatBytes(bytes);
        }
        if (value is int intBytes)
        {
            return FormatBytes(intBytes);
        }
        if (value is ulong ulongBytes)
        {
            return FormatBytes((long)Math.Min(ulongBytes, long.MaxValue));
        }
        return "—";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();

    internal static string FormatBytes(long bytes) => bytes switch
    {
        < 0 => "—",
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024.0:0.#} KB",
        < 1024L * 1024 * 1024 => $"{bytes / (1024.0 * 1024):0.#} MB",
        < 1024L * 1024 * 1024 * 1024 => $"{bytes / (1024.0 * 1024 * 1024):0.##} GB",
        _ => $"{bytes / (1024.0 * 1024 * 1024 * 1024):0.##} TB",
    };
}

/// <summary>Null → Visibility (null/invisible-false = Collapsed, parameter inverts).</summary>
public sealed class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var isVisible = value is not null;
        if (parameter is string { Length: > 0 } inverse && inverse == "invert")
        {
            isVisible = !isVisible;
        }
        return isVisible ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>Bool → Visibility (false = Collapsed, parameter "hidden" = Hidden).</summary>
public sealed class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var isVisible = value is true;
        if (parameter is string { Length: > 0 } mode && mode == "invert")
        {
            isVisible = !isVisible;
        }
        if (!isVisible && parameter is string hidden && hidden == "hidden")
        {
            return Visibility.Hidden;
        }
        return isVisible ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>Null → bool inversion: non-null = true (enables controls gated
/// on an optional object being present, e.g. Recover All with a session).</summary>
public sealed class NullToBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is not null;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>Equality → Visibility: value equals parameter (string) → Visible.</summary>
public sealed class EqualityToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var isEqual = string.Equals(value?.ToString(), parameter?.ToString(), StringComparison.OrdinalIgnoreCase);
        return isEqual ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>Enum index → int for the ConfidenceBadge Grade property.</summary>
public sealed class EnumToIntConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is Enum enumValue ? System.Convert.ToInt32(enumValue, culture) : 0;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>Count (int/long) → Visibility: 0 = Collapsed, >0 = Visible.</summary>
public sealed class CountToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value switch
        {
            int i => i > 0 ? Visibility.Visible : Visibility.Collapsed,
            long l => l > 0 ? Visibility.Visible : Visibility.Collapsed,
            _ => Visibility.Collapsed,
        };

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>Enum equality → bool (RadioButtons/selection): value equals parameter.</summary>
public sealed class EnumMatchConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        Equals(value, parameter);

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true ? parameter! : Binding.DoNothing;
}

/// <summary>Date → short local string; null-safe (G-24 timestamps render as "—").</summary>
public sealed class NullableDateConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is DateTimeOffset dateTime ? dateTime.ToLocalTime().ToString("yyyy-MM-dd HH:mm", culture) : "—";

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>Enum value → bool by NAME (XAML string parameter): value.ToString() equals parameter.</summary>
public sealed class EnumNameMatchConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is not null &&
        parameter is string name &&
        string.Equals(value.ToString(), name, StringComparison.OrdinalIgnoreCase);

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true ? parameter! : Binding.DoNothing;
}
