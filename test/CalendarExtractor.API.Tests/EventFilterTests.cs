using System;
using System.Collections.Generic;
using CalendarExtractor.API.Helper;
using Microsoft.Graph;
using Xunit;

namespace CalendarExtractor.API.Tests
{
    public class EventFilterTests
    {
        [Fact]
        public void FilterEvents_NoMatch()
        {
            // given 
            var dateTimeNow = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);
            var graphEvents = new List<Event> { CreateEvent(dateTimeNow, dateTimeNow.AddHours(2)) };
            var eventTimeStart = DateTimeOffset.Now.AddHours(-2);
            var eventTimeEnd = eventTimeStart.AddHours(1);

            // when
            var eventFilter = new EventFilter(graphEvents);
            var actualFilteredEvents = eventFilter.FilterEventsFor(eventTimeStart, eventTimeEnd);

            // then
            Assert.Empty(actualFilteredEvents);
        }

        private Event CreateEvent(DateTimeOffset start, DateTimeOffset end)
        {
            const string timeZoneUtc = "UTC";

            var startTime = DateTimeTimeZone.FromDateTime(start.DateTime);
            startTime.TimeZone = timeZoneUtc;
            var endTime = DateTimeTimeZone.FromDateTime(end.DateTime);
            endTime.TimeZone = timeZoneUtc;

            return new Event
            {
                Start = startTime,
                End = endTime
            };
        }
    }
}
