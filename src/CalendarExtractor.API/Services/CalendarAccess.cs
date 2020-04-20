using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CalendarExtractor.API.Helper;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using static CalendarExtractor.API.Helper.DateTimeOffsetFormatter;

namespace CalendarExtractor.API.Services
{
    public class CalendarAccess : calendar_access.calendar_accessBase
    {
        private readonly ILogger<CalendarAccess> _logger;
        private readonly IRequestValidator _requestValidator;
        private const string BaseAuthorityUrl = "https://login.microsoftonline.com";

        public CalendarAccess(ILogger<CalendarAccess> logger, IRequestValidator requestValidator)
        {
            _logger = logger;
            _requestValidator = requestValidator;
        }

        public override async Task get_calendar_information(calendar_information_request request, IServerStreamWriter<calendar_information_reply> responseStream,
            ServerCallContext context)
        {
            _logger.LogInformation($"GetCalendarInformation request for {request.Calendar.CalendarId}");

            _requestValidator.Validate(request);

            var graphCalendarClient = CreateGraphCalendarClient(request.Client);
            var graphEvents = await graphCalendarClient.GetEventsAsync(request.Calendar);
            var azureReplyEvents = CreateReplyEventsOf(graphEvents);

            _logger.LogInformation($"Sending response for request {request}");

            foreach (var replyEvent in azureReplyEvents)
            {
                if (context.CancellationToken.IsCancellationRequested) break;
                await responseStream.WriteAsync(replyEvent);
            }
        }

        /// TODO: Delete before finish proj
        public override async Task test_get_calendar_information(calendar_information_request request, IServerStreamWriter<calendar_information_reply> responseStream, 
            ServerCallContext context)
        {
            _logger.LogInformation($"Get Request for {request.Calendar.CalendarId}");

            _requestValidator.Validate(request);

            var graphCalendarClient = CreateGraphCalendarClient(request.Client);
            var graphEvents = graphCalendarClient.GetEventsAsync(request.Calendar).Result;
            var azureReplyEvents = CreateTestReplyEventsOf(graphEvents);

            _logger.LogInformation($"Sending response for request {request}");

            foreach (var replyEvent in azureReplyEvents)
            {
                if (context.CancellationToken.IsCancellationRequested) break;
                await responseStream.WriteAsync(replyEvent);
            }
        }

        private GraphCalendarHelper CreateGraphCalendarClient(calendar_information_request.Types.Client client)
        {
            var authority = string.Join('/', BaseAuthorityUrl, client.TenantId);
            var authProvider = new DeviceCodeAuthProvider(client.ClientId, client.ClientSecret, authority);
            
            return new GraphCalendarHelper(authProvider, _logger);
        }

        private IEnumerable<calendar_information_reply> CreateReplyEventsOf(IEnumerable<Event> graphEvents)
        {
            return graphEvents
                .OrderBy(g => g.Start.DateTime)
                .Select(e => new calendar_information_reply
                {
                    Subject = e.Subject,
                    BeginTimestamp = FormatDateTimeTimeZoneToLocal(e.Start).ToTimestamp(),
                    EndTimestamp = FormatDateTimeTimeZoneToLocal(e.End).ToTimestamp()
                });
        }

        /// TODO: Delete before finish proj
        private IEnumerable<calendar_information_reply> CreateTestReplyEventsOf(IEnumerable<Event> graphEvents)
        {
            return graphEvents
                .OrderBy(g => g.Start.DateTime)
                .Select(e =>
                {
                    var builder = new StringBuilder();

                    builder.AppendLine($"  Subject: {e.Subject}");
                    if (e.Importance != null)
                        builder.AppendLine($"  Importance: {e.Importance.Value}");
                    builder.AppendLine($"  Organizer: {e.Organizer.EmailAddress.Name}");
                    e.Attendees.ToList().ForEach(a => builder.AppendLine($"    Attendee: {a.EmailAddress.Name}"));
                    builder.AppendLine($"  Start: {FormatDateTimeTimeZoneToLocal(e.Start)}");
                    builder.AppendLine($"  End: {FormatDateTimeTimeZoneToLocal(e.End)}");
                    builder.AppendLine($"  Location Name {e.Location.DisplayName}"); 
                    
                    return new calendar_information_reply
                    {
                        Subject = builder.ToString(),
                        BeginTimestamp = FormatDateTimeTimeZoneToLocal(e.Start).ToTimestamp(),
                        EndTimestamp = FormatDateTimeTimeZoneToLocal(e.End).ToTimestamp()
                    };
                });
        }
    }
}
