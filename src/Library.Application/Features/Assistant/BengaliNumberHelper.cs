using System.Globalization;

namespace Library.Application.Features.Assistant;

/// <summary>
/// Converts ASCII digits to Bengali digits (০, ১, ২, ৩, ৪, ৫, ৬, ৭, ৮, ৯)
/// and assists with Bengali number formatting.
/// </summary>
public static class BengaliNumberHelper
{
    private static readonly char[] BanglaDigits = ['০', '১', '২', '৩', '৪', '৫', '৬', '৭', '৮', '৯'];

    public static string ToBanglaDigits(int number) =>
        ToBanglaDigits(number.ToString(CultureInfo.InvariantCulture));

    public static string ToBanglaDigits(long number) =>
        ToBanglaDigits(number.ToString(CultureInfo.InvariantCulture));

    public static string ToBanglaDigits(string text)
    {
        var chars = text.ToCharArray();
        for (var i = 0; i < chars.Length; i++)
        {
            if (chars[i] >= '0' && chars[i] <= '9')
            {
                chars[i] = BanglaDigits[chars[i] - '0'];
            }
        }
        return new string(chars);
    }

    public static string FormatCount(int count, bool isBengali, string enSingular, string enPlural, string bnUnit = "টি")
    {
        if (isBengali)
        {
            return $"{ToBanglaDigits(count)}{bnUnit}";
        }

        return count == 1 ? $"1 {enSingular}" : $"{count} {enPlural}";
    }
}

