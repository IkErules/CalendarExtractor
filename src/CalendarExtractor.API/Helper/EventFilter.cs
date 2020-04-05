using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Graph;
using static CalendarExtractor.API.Helper.DateTimeOffsetFormatter;

namespace CalendarExtractor.API.Helper
{
    public class EventFilter
    {
        private readonly IEnumerable<Event> _events;

        public EventFilter(IEnumerable<Event> events)
        {
            _events = events;
        }

        public IEnumerable<Event> FilterEventsFor(DateTimeOffset startTime, DateTimeOffset endTime)
        {
            return _events
                .Where(e => (FormatDateTimeTimeZoneToLocal(e.Start).DateTime.CompareTo(startTime.DateTime) < 0
                             && FormatDateTimeTimeZoneToLocal(e.End).DateTime.CompareTo(startTime.DateTime) > 0)
                            || (FormatDateTimeTimeZoneToLocal(e.Start).DateTime.CompareTo(startTime.DateTime) > 0
                                && FormatDateTimeTimeZoneToLocal(e.End).DateTime.CompareTo(endTime.DateTime) < 0)
                            || (FormatDateTimeTimeZoneToLocal(e.Start).DateTime.CompareTo(endTime.DateTime) < 0
                                && FormatDateTimeTimeZoneToLocal(e.End).DateTime.CompareTo(endTime.DateTime) > 0));
        }
    }
}
