using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalendarExtractor.API.Helper;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using static CalendarExtractor.API.Helper.DateTimeOffsetFormatter;

namespace CalendarExtractor.API.Services
{
    public class CalendarAccessController : CalendarAccess.CalendarAccessBase
    {
        private readonly ILogger<CalendarAccessController> _logger;
        private readonly IRequestValidator _requestValidator;
        private const string BaseAuthorityUrl = "https://login.microsoftonline.com";

        public CalendarAccessController(ILogger<CalendarAccessController> logger, IRequestValidator requestValidator)
        {
            _logger = logger;
            _requestValidator = requestValidator;
        }

        public override async Task get_calendar_information(CalendarInformationRequest request, IServerStreamWriter<CalendarInformationReply> responseStream,
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

        public override async Task get_all_rooms_of_organisation(CalendarInformationRequest request, IServerStreamWriter<RoomReply> responseStream,
            ServerCallContext context)
        {
            _logger.LogInformation($"GetCalendarInformation request for {request.Calendar.CalendarId}");

            _requestValidator.Validate(request);

            var graphCalendarClient = CreateGraphRoomHelper(request.Client);
            var allRooms = await graphCalendarClient.GetAllRoomsAsnc(request.Calendar);

            _logger.LogInformation($"Sending response for request {request}");

            foreach (var replyEvent in allRooms)
            {
                if (context.CancellationToken.IsCancellationRequested) break;
                await responseStream.WriteAsync(replyEvent);
            }
        }

        private GraphCalendarHelper CreateGraphCalendarClient(CalendarInformationRequest.Types.Client client)
        {
            var authProvider = CreateDeviceCodeAuthProvider(client);

            return new GraphCalendarHelper(authProvider, _logger);
        }

        private GraphRoomHelper CreateGraphRoomHelper(CalendarInformationRequest.Types.Client client)
        {
            var authProvider = CreateDeviceCodeAuthProvider(client);

            return new GraphRoomHelper(authProvider, _logger);
        }

        private static DeviceCodeAuthProvider CreateDeviceCodeAuthProvider(CalendarInformationRequest.Types.Client client)
        {
            var authority = string.Join('/', BaseAuthorityUrl, client.TenantId);
            var authProvider = new DeviceCodeAuthProvider(client.ClientId, client.ClientSecret, authority);
            return authProvider;
        }

        private IEnumerable<CalendarInformationReply> CreateReplyEventsOf(IEnumerable<Event> graphEvents)
        {
            return graphEvents
                .OrderBy(g => g.Start.DateTime)
                .Select(e => new CalendarInformationReply
                {
                    Subject = e.Subject,
                    BeginTime = FormatDateTimeTimeZoneUnixTime(e.Start),
                    EndTime = FormatDateTimeTimeZoneUnixTime(e.End)
                });
        }
    }
}
