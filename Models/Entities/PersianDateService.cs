using System.Globalization;

namespace Factors.Web.Models.Entities;

/// <summary>
/// سرویس تبدیل تاریخ میلادی به شمسی و بالعکس
/// </summary>
public static class PersianDateService
{
    private static readonly PersianCalendar _calendar = new();

    /// <summary>
    /// تبدیل تاریخ میلادی به شمسی با فرمت yyyy/MM/dd
    /// </summary>
    public static string ToPersian(DateTime gregorianDate, bool showTime = false)
    {
        int year = _calendar.GetYear(gregorianDate);
        int month = _calendar.GetMonth(gregorianDate);
        int day = _calendar.GetDayOfMonth(gregorianDate);
        bool isLeap = _calendar.IsLeapYear(year);

        string dateStr = $"{year}/{month:00}/{day:00}";
        string leapIndicator = isLeap ? " (کبیسه)" : "";
        string timeStr = showTime ? $" {gregorianDate:HH:mm:ss}" : "";

        return "\u200F" + dateStr + leapIndicator + timeStr;
    }

    /// <summary>
    /// تبدیل تاریخ میلادی nullable به شمسی
    /// </summary>
    public static string ToPersian(DateTime? gregorianDate, bool showTime = true, string nullValue = "\u200F-")
    {
        if (!gregorianDate.HasValue)
            return nullValue;
        return ToPersian(gregorianDate.Value, showTime);
    }

    /// <summary>
    /// تبدیل تاریخ شمسی به میلادی
    /// </summary>
    public static DateTime ToGregorian(int persianYear, int persianMonth, int persianDay)
    {
        return _calendar.ToDateTime(persianYear, persianMonth, persianDay, 0, 0, 0, 0);
    }

    /// <summary>
    /// تبدیل رشته تاریخ شمسی (yyyy/MM/dd) به میلادی
    /// </summary>
    public static DateTime? ParsePersianDate(string persianDate)
    {
        if (string.IsNullOrWhiteSpace(persianDate))
            return null;

        try
        {
            var parts = persianDate.Trim().Replace("-", "/").Split('/');
            if (parts.Length != 3)
                return null;

            int year = int.Parse(parts[0]);
            int month = int.Parse(parts[1]);
            int day = int.Parse(parts[2]);

            return ToGregorian(year, month, day);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// دریافت تاریخ و زمان فعلی به شمسی
    /// </summary>
    public static string Now => ToPersian(DateTime.UtcNow, true);

    /// <summary>
    /// دریافت سال شمسی فعلی
    /// </summary>
    public static int CurrentPersianYear => _calendar.GetYear(DateTime.UtcNow);
}
