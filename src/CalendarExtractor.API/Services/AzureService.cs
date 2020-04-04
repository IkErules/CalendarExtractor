using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CalendarExtractor.API.Helper;
using Grpc.Core;
using Microsoft.Extensions.Logging;

namespace CalendarExtractor.API
{
    public class AzureService : Azure.AzureBase
    {
        private readonly ILogger<AzureService> _logger;

        public AzureService(ILogger<AzureService> logger)
        {
            _logger = logger;
        }

        public override Task<AzureReply> SayHello(AzureRequest request, ServerCallContext context)
        {
            _logger.LogInformation($"Get Request for {request.CalendarId}");
            return Task.FromResult(new AzureReply
            {
                Message = Test(request)
            });
        }

        private string Test(AzureRequest request)
        {
            var authority = string.Format(CultureInfo.InvariantCulture, "https://login.microsoftonline.com/{0}",
                request.TenantId);
                
            const string scopesString = "https://graph.microsoft.com/.default";
            var scopes = scopesString.Split(';');

            var authProvider = new DeviceCodeAuthProvider(request.ClientId, request.ClientSecret, authority, scopes);

            // Initialize Graph client
            var graphHelper = new GraphHelper(authProvider);

            return ListCalendarEventsFor(request.CalendarId, graphHelper);
        }

        private string ListCalendarEventsFor(string calendarId, GraphHelper graphHelper)
        {
            var events = graphHelper.GetEventsAsync(calendarId).Result;
            
            var builder = new StringBuilder();
            builder.AppendLine("Events:");

            foreach (var calendarEvent in events)
            {
                builder.AppendLine("-------------NEW EVENT---------------");
                builder.AppendLine($"Subject: {calendarEvent.Subject}");
                if (calendarEvent.Importance != null) builder.AppendLine($"Subject: {calendarEvent.Importance.Value}");
                builder.AppendLine($"Subject: {calendarEvent.Subject}");
                builder.AppendLine($"  Organizer: {calendarEvent.Organizer.EmailAddress.Name}");
                calendarEvent.Attendees.ToList().ForEach(a => builder.AppendLine($"  Attendee: {a.EmailAddress.Name}"));
                builder.AppendLine($"  Start: {FormatDateTimeTimeZone(calendarEvent.Start)}");
                builder.AppendLine($"  End: {FormatDateTimeTimeZone(calendarEvent.End)}");
            }

            return builder.ToString();
        }

        private string FormatDateTimeTimeZone(Microsoft.Graph.DateTimeTimeZone value)
        {
            // Get the timezone specified in the Graph value
            var timeZone = TimeZoneInfo.FindSystemTimeZoneById(value.TimeZone);
            // Parse the date/time string from Graph into a DateTime
            var dateTime = DateTime.Parse(value.DateTime);

            // Create a DateTimeOffset in the specific timezone indicated by Graph
            var dateTimeWithTZ = new DateTimeOffset(dateTime, timeZone.BaseUtcOffset)
                .ToLocalTime();

            return dateTimeWithTZ.ToString("g");
        }
    }
}
