using System;
using System.Collections.Generic;
using System.Linq;
using CalendarExtractor.API.Helper;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Moq;
using Xunit;
using IAuthenticationProvider = Microsoft.Graph.IAuthenticationProvider;

namespace CalendarExtractor.API.Tests
{
    public class EventFilterTests
    {
        private readonly GraphCalendarHelper _graphCalendarHelper;

        public EventFilterTests()
        {
            var logger = new Mock<ILogger>().Object;
            var authenticationProvider = new Mock<IAuthenticationProvider>().Object;
            _graphCalendarHelper = new GraphCalendarHelper(authenticationProvider, logger);
        }

        [Fact]
        public void FilterEvents_NoMatch()
        {
            // given 
            var dateTimeNow = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);
            var graphEvents = new List<Event> { CreateGraphEvent(dateTimeNow, 60) };
            
            var filterTimeStart = DateTimeOffset.Now.AddHours(-2);
            var filterTimeEnd = filterTimeStart.AddHours(1);

            // when
            var actualFilteredEvents = _graphCalendarHelper
                .FilterEventsFor(graphEvents, filterTimeStart, filterTimeEnd);

            // then
            Assert.Empty(actualFilteredEvents);
        }

        [Fact]
        public void FilterEvents_ReturnsSameEvent()
        {
            // given 
            var dateTimeNow = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);
            var expectedGraphEvent = CreateGraphEvent(dateTimeNow, 120);

            var filterTimeStart = DateTimeOffset.Now.AddMinutes(60);
            var filterTimeEnd = filterTimeStart.AddMinutes(20);

            // when
            var actualFilteredEvents = _graphCalendarHelper
                .FilterEventsFor(new List<Event> { expectedGraphEvent }, filterTimeStart, filterTimeEnd);
            // then
            Assert.True(actualFilteredEvents.Count() == 1);
            var actualFilteredEvent = actualFilteredEvents.SingleOrDefault();
            Assert.Equal(expectedGraphEvent, actualFilteredEvent);
        }

        [Fact]
        public void FilterEvents_ReturnsOneEvent()
        {
            // given 
            var dateTimeNow = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);
            var expectedGraphEvent = CreateGraphEvent(dateTimeNow, 120);
            var secondGraphEvent = CreateGraphEvent(dateTimeNow.AddHours(5), 60);

            var filterTimeStart = DateTimeOffset.Now.AddMinutes(60);
            var filterTimeEnd = filterTimeStart.AddMinutes(80);

            // when
            var actualFilteredEvents = _graphCalendarHelper
                .FilterEventsFor(new List<Event> { expectedGraphEvent, secondGraphEvent }, filterTimeStart, filterTimeEnd);

            // then
            Assert.True(actualFilteredEvents.Count() == 1);
            var actualFilteredEvent = actualFilteredEvents.SingleOrDefault();
            Assert.Equal(expectedGraphEvent, actualFilteredEvent);
        }

        [Fact]
        public void FilterEvents_ReturnsTwoEvents()
        {
            // given 
            var dateTimeNow = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);
            var expectedGraphEvent = CreateGraphEvent(dateTimeNow, 30);
            var expectedSecondGraphEvent = CreateGraphEvent(dateTimeNow.AddHours(1), 60);

            var filterTimeStart = DateTimeOffset.Now.AddMinutes(-30);
            var filterTimeEnd = filterTimeStart.AddHours(3);

            // when
            var actualFilteredEvents = _graphCalendarHelper
                .FilterEventsFor(new List<Event> { expectedGraphEvent, expectedSecondGraphEvent }, filterTimeStart, filterTimeEnd);

            // then
            Assert.True(actualFilteredEvents.Count() == 2);
            Assert.Contains(expectedGraphEvent, actualFilteredEvents);
            Assert.Contains(expectedSecondGraphEvent, actualFilteredEvents);
        }

        [Fact]
        public void FilterEvents_EqualBeginAndEndFilter_ReturnsOneEvent()
        {
            // given 
            var dateTimeNow = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);
            var expectedGraphEvent = CreateGraphEvent(dateTimeNow, 120);
            var secondGraphEvent = CreateGraphEvent(dateTimeNow.AddHours(5), 60);

            var filterDateTime = DateTimeOffset.Now.AddMinutes(60);

            // when
            var actualFilteredEvents = _graphCalendarHelper
                .FilterEventsFor(new List<Event> { expectedGraphEvent, secondGraphEvent }, filterDateTime, filterDateTime);

            // then
            Assert.True(actualFilteredEvents.Count() == 1);
            var actualFilteredEvent = actualFilteredEvents.SingleOrDefault();
            Assert.Equal(expectedGraphEvent, actualFilteredEvent);
        }

        private Event CreateGraphEvent(DateTimeOffset start, int durationInMinutes)
        {
            const string timeZoneUtc = "UTC";
            var end = start.AddMinutes(durationInMinutes);

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
