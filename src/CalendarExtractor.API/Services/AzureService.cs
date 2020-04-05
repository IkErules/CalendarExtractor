using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CalendarExtractor.API.Helper;
using Google.Protobuf.Collections;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;

namespace CalendarExtractor.API
{
    public class AzureService : Azure.AzureBase
    {
        private readonly ILogger<AzureService> _logger;
        private const string BaseAuthorityUrl = "https://login.microsoftonline.com";

        public AzureService(ILogger<AzureService> logger)
        {
            _logger = logger;
        }

        public override Task<AzureReply> GetCalendarInformation(AzureRequest request, ServerCallContext context)
        {
            _logger.LogInformation($"Get Request for {request.Calendar.CalendarId}");

            var graphCalendarClient = CreateGraphCalendarClient(request.Client);

            return Task.FromResult(ListCalendarEventsFor(request.Calendar, graphCalendarClient));
        }

        private GraphCalendarHelper CreateGraphCalendarClient(AzureRequest.Types.Client client)
        {
            var authority = string.Join('/', BaseAuthorityUrl, client.TenantId);
            var authProvider = new DeviceCodeAuthProvider(client.ClientId, client.ClientSecret, authority);
            
            return new GraphCalendarHelper(authProvider);
        }

        private AzureReply ListCalendarEventsFor(AzureRequest.Types.Calendar calendar, GraphCalendarHelper graphCalendarHelper)
        {
            var events = graphCalendarHelper.GetEventsAsync(calendar).Result;

            events = FilterEventsWithStartAndEndTimeOf(calendar, events);
            var azureReply = CreateAzureReplyOf(events);

            return azureReply;
        }

        internal IEnumerable<Event> FilterEventsWithStartAndEndTimeOf(AzureRequest.Types.Calendar calendar, IEnumerable<Event> events)
        {
            DateTimeOffset startTime = calendar.BeginnDateTime.ToDateTime().ToLocalTime();
            DateTimeOffset endTime = calendar.EndDateTime.ToDateTime().ToLocalTime();

            _logger.LogInformation($"Send startTime: {startTime}");
            _logger.LogInformation($"Send endTime: {endTime}");

            return events
                .Where(e => (FormatDateTimeTimeZoneToLocal(e.Start).DateTime.CompareTo(startTime.DateTime) < 0
                                && FormatDateTimeTimeZoneToLocal(e.End).DateTime.CompareTo(startTime.DateTime) > 0)
                            || (FormatDateTimeTimeZoneToLocal(e.Start).DateTime.CompareTo(startTime.DateTime) > 0 
                                && FormatDateTimeTimeZoneToLocal(e.End).DateTime.CompareTo(endTime.DateTime) < 0)
                            || (FormatDateTimeTimeZoneToLocal(e.Start).DateTime.CompareTo(endTime.DateTime) < 0 
                                && FormatDateTimeTimeZoneToLocal(e.End).DateTime.CompareTo(endTime.DateTime) > 0));
        }

        //private Predicate<>

        private AzureReply CreateAzureReplyOf(IEnumerable<Event> events)
        {
            var azureReply = new AzureReply();
            if (events?.Any() ?? false)
            {
                azureReply.IsOccupied = true;
                azureReply.Event.AddRange(CreateReplyEventsOf(events));
            }

            return azureReply;
        }

        private IEnumerable<AzureReply.Types.Event> CreateReplyEventsOf(IEnumerable<Event> graphEvents)
        {
            return graphEvents
                .OrderBy(g => g.Start.DateTime)
                .Select(e => new AzureReply.Types.Event
                {
                    Subject = e.Subject,
                    BeginnDateTime = FormatDateTimeTimeZoneToLocal(e.Start).ToString("g"),
                    EndDateTime = FormatDateTimeTimeZoneToLocal(e.End).ToString("g")
                });
        }

        private string ListCalendarEventsForTest(AzureRequest.Types.Calendar calendar,
            GraphCalendarHelper graphCalendarHelper)
        {
            var events = graphCalendarHelper.GetEventsAsync(calendar).Result;

            var builder = new StringBuilder();

            foreach (var calendarEvent in events)
            {
                builder.AppendLine($"Subject: {calendarEvent.Subject}");
                if (calendarEvent.Importance != null)
                    builder.AppendLine($"Importance: {calendarEvent.Importance.Value}");
                builder.AppendLine($"  Organizer: {calendarEvent.Organizer.EmailAddress.Name}");
                calendarEvent.Attendees.ToList().ForEach(a => builder.AppendLine($"  Attendee: {a.EmailAddress.Name}"));
                builder.AppendLine($"  Start: {FormatDateTimeTimeZoneToLocal(calendarEvent.Start)}");
                builder.AppendLine($"  End: {FormatDateTimeTimeZoneToLocal(calendarEvent.End)}");
            }

            return builder.ToString();
        }

        private DateTimeOffset FormatDateTimeTimeZoneToLocal(DateTimeTimeZone value)
        {
            var timeZone = TimeZoneInfo.FindSystemTimeZoneById(value.TimeZone);
            var dateTime = DateTime.Parse(value.DateTime);

            var dateTimeWithTZ = new DateTimeOffset(dateTime, timeZone.BaseUtcOffset)
                .ToLocalTime();

            return dateTimeWithTZ;
        }
    }
}
