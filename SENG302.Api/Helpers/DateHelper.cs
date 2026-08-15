
namespace SENG302.Api.Helpers;

public static class DateHelper
{
    /// <summary>
    /// Turns a year, month, day into a date time offset which starts at 0:00 and covers a span of zero
    /// </summary>
    /// <param name="date">The date</param>
    /// <returns>The date time offset, starting at 0:00, on the date provided</returns>
    public static DateTimeOffset DateToDateTimeOffset(int year, int month, int day)
    {
        return new DateTimeOffset(new DateOnly(year, month, day), TimeOnly.MinValue, TimeSpan.Zero);
    }
}