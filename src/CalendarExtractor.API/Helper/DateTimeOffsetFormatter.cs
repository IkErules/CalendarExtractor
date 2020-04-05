using System;
using Microsoft.Graph;

namespace CalendarExtractor.API.Helper
{
    public class DateTimeOffsetFormatter
    {
        public static DateTimeOffset FormatDateTimeTimeZoneToLocal(DateTimeTimeZone value)
        {
            var timeZone = TimeZoneInfo.FindSystemTimeZoneById(value.TimeZone);
            var dateTime = DateTime.Parse(value.DateTime);

            var dateTimeWithTz = new DateTimeOffset(dateTime, timeZone.BaseUtcOffset)
                .ToLocalTime();

            return dateTimeWithTz;
        }
    }
}
