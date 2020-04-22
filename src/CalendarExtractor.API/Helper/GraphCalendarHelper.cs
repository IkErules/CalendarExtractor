using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using static CalendarExtractor.API.Helper.DateTimeOffsetFormatter;
using Status = Grpc.Core.Status;

namespace CalendarExtractor.API.Helper
{
    public class GraphCalendarHelper
    {
        private const string SearchTermStart = "start/dateTime";
        private const string SearchTermEnd = "end/dateTime";

        private readonly GraphServiceClient _graphClient;
        private readonly ILogger _logger;

        public GraphCalendarHelper(IAuthenticationProvider authProvider, ILogger logger)
        {
            _graphClient = new GraphServiceClient(authProvider);
            _logger = logger;
        }

        public async Task<IEnumerable<Event>> GetEventsAsync(calendar_information_request.Types.Calendar calendar)
        {
            try
            {
                var startFilter = DateTimeOffset.FromUnixTimeSeconds(calendar.BeginTime).DateTime.ToString("o");
                var endFilter = DateTimeOffset.FromUnixTimeSeconds(calendar.EndTime).DateTime.ToString("o");

                _logger.LogInformation($"Start filter datetime UTC: {startFilter}");
                _logger.LogInformation($"End filter dateTime UTC: {endFilter}");

                var resultPage = await _graphClient.Users[calendar.CalendarId].Events
                    .Request()
                    .Select("subject,organizer,start,end")
                    .Filter($"{SearchTermStart} gt '{startFilter}' and {SearchTermStart} lt '{endFilter}'" +
                            $"or {SearchTermEnd} gt '{startFilter}' and {SearchTermEnd} lt '{endFilter}'" +
                            $"or {SearchTermStart} lt '{startFilter}' and {SearchTermEnd} gt '{endFilter}'")
                    .GetAsync();

                return GetAllEventsBasedOnPage(resultPage);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting events for calendarId {calendar.CalendarId}: {ex.Message}");
                throw new RpcException(new Status(StatusCode.Internal, $"Error while reading event: {ex.Message}"));
            }
        }

        /// <summary>
        /// Only for test purposes, to test filter of <see cref="GetEventsAsync(AzureRequest.Types.Calendar)"/>
        /// </summary>
        public IEnumerable<Event> FilterEventsFor(IEnumerable<Event> events, DateTimeOffset startFilter, DateTimeOffset endFilter)
        {
            return events
                .Where(e => (FormatDateTimeTimeZoneToLocal(e.Start).DateTime.CompareTo(startFilter.DateTime) > 0
                             && FormatDateTimeTimeZoneToLocal(e.Start).DateTime.CompareTo(endFilter.DateTime) < 0)
                            || (FormatDateTimeTimeZoneToLocal(e.End).DateTime.CompareTo(startFilter.DateTime) > 0
                                && FormatDateTimeTimeZoneToLocal(e.End).DateTime.CompareTo(endFilter.DateTime) < 0)
                            || (FormatDateTimeTimeZoneToLocal(e.Start).DateTime.CompareTo(startFilter.DateTime) < 0
                                && FormatDateTimeTimeZoneToLocal(e.End).DateTime.CompareTo(endFilter.DateTime) > 0));
        }

        private IEnumerable<Event> GetAllEventsBasedOnPage(IUserEventsCollectionPage resultPage)
        {
            var allEvents = new List<Event>();
            do
            {
                var eventsOfCurrentPage = resultPage.CurrentPage.ToList();
                allEvents.AddRange(eventsOfCurrentPage);

                resultPage = resultPage.NextPageRequest?.GetAsync().Result;
            } while (resultPage != null);

            return allEvents;
        }
    }
}