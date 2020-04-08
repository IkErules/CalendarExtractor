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
using static CalendarExtractor.API.Helper.DateTimeOffsetFormatter;
using Status = Grpc.Core.Status;

namespace CalendarExtractor.API
{
    public class AzureService : Azure.AzureBase
    {
        private readonly ILogger<AzureService> _logger;
        private readonly IRequestValidator _requestValidator;
        private const string BaseAuthorityUrl = "https://login.microsoftonline.com";

        public AzureService(ILogger<AzureService> logger, IRequestValidator requestValidator)
        {
            _logger = logger;
            _requestValidator = requestValidator;
        }

        public override Task<AzureReply> GetCalendarInformation(AzureRequest request, ServerCallContext context)
        {
            _logger.LogInformation($"Get Request for {request.Calendar.CalendarId}");

            _requestValidator.Validate(request);

            var graphCalendarClient = CreateGraphCalendarClient(request.Client);
            var events = ListCalendarEventsFor(request.Calendar, graphCalendarClient);

            return Task.FromResult(CreateAzureReplyOf(events));
        }
        
        private GraphCalendarHelper CreateGraphCalendarClient(AzureRequest.Types.Client client)
        {
            var authority = string.Join('/', BaseAuthorityUrl, client.TenantId);
            var authProvider = new DeviceCodeAuthProvider(client.ClientId, client.ClientSecret, authority);
            
            return new GraphCalendarHelper(authProvider);
        }

        private IEnumerable<Event> ListCalendarEventsFor(AzureRequest.Types.Calendar calendar, GraphCalendarHelper graphCalendarHelper)
        {
            var events = graphCalendarHelper.GetEventsAsync(calendar).Result;
            return FilterEventsWithStartAndEndTimeOf(calendar, events);
        }

        private IEnumerable<Event> FilterEventsWithStartAndEndTimeOf(AzureRequest.Types.Calendar calendar, IEnumerable<Event> events)
        {
            DateTimeOffset startTime = calendar.BeginnDateTime.ToDateTime().ToLocalTime();
            DateTimeOffset endTime = calendar.EndDateTime.ToDateTime().ToLocalTime();

            _logger.LogInformation($"Sent startTime: {startTime}");
            _logger.LogInformation($"Sent endTime: {endTime}");

            var eventFilter = new EventFilter(events);
            return eventFilter.FilterEventsFor(startTime, endTime);
        }

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
    }
}
