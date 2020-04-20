using System;
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

        public override async Task GetCalendarInformation(AzureRequest request, IServerStreamWriter<AzureReplyEvent> responseStream,
            ServerCallContext context)
        {
            _logger.LogInformation($"GetCalendarInformation request for {request.Calendar.CalendarId}");

            _requestValidator.Validate(request);

            var graphCalendarClient = CreateGraphCalendarClient(request.Client);
            var graphEvents = graphCalendarClient.GetEventsAsync(request.Calendar).Result;
            var azureReplyEvents = CreateReplyEventsOf(graphEvents);

            _logger.LogInformation($"Sending response for request {request}");

            foreach (var replyEvent in azureReplyEvents)
            {
                if (context.CancellationToken.IsCancellationRequested) break;
                await responseStream.WriteAsync(replyEvent);
            }
        }

        /// TODO: Delete before finish proj
        public override async Task TestGetCalendarInformation(AzureRequest request, IServerStreamWriter<AzureReplyEvent> responseStream, 
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

        private GraphCalendarHelper CreateGraphCalendarClient(AzureRequest.Types.Client client)
        {
            var authority = string.Join('/', BaseAuthorityUrl, client.TenantId);
            var authProvider = new DeviceCodeAuthProvider(client.ClientId, client.ClientSecret, authority);
            
            return new GraphCalendarHelper(authProvider, _logger);
        }

        private IEnumerable<AzureReplyEvent> CreateReplyEventsOf(IEnumerable<Event> graphEvents)
        {
            return graphEvents
                .OrderBy(g => g.Start.DateTime)
                .Select(e => new AzureReplyEvent
                {
                    Subject = e.Subject,
                    BeginTimestamp = FormatDateTimeTimeZoneToLocal(e.Start).ToTimestamp(),
                    EndTimestamp = FormatDateTimeTimeZoneToLocal(e.End).ToTimestamp()
                });
        }

        /// TODO: Delete before finish proj
        private IEnumerable<AzureReplyEvent> CreateTestReplyEventsOf(IEnumerable<Event> graphEvents)
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
                    
                    return new AzureReplyEvent
                    {
                        Subject = builder.ToString(),
                        BeginTimestamp = FormatDateTimeTimeZoneToLocal(e.Start).ToTimestamp(),
                        EndTimestamp = FormatDateTimeTimeZoneToLocal(e.End).ToTimestamp()
                    };
                });
        }
    }
}
