extern alias BetaLib;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BetaLib::Microsoft.Graph;
using Microsoft.Extensions.Logging;
using IAuthenticationProvider = Microsoft.Graph.IAuthenticationProvider;


namespace CalendarExtractor.API.Helper
{
    public class GraphRoomHelper
    {
        private readonly GraphServiceClient _graphClient;
        private readonly ILogger _logger;

        public GraphRoomHelper(IAuthenticationProvider authProvider, ILogger logger)
        {
            _graphClient = new GraphServiceClient(authProvider);
            _logger = logger;
        }

        public async Task<IEnumerable<RoomReply>> GetAllRoomsAsnc(CalendarInformationRequest.Types.Calendar calendar)
        {
            _logger.LogInformation($"Getting all rooms for {calendar.CalendarId}...");

            var rooms = await _graphClient.Users[calendar.CalendarId].FindRooms().Request().GetAsync();
            var allRooms = GetAllRoomsOnPage(rooms);
            
            return ConvertToRoomReplies(allRooms);
        }

        private IEnumerable<EmailAddress> GetAllRoomsOnPage(IUserFindRoomsCollectionPage resultPage)
        {
            var allEvents = new List<EmailAddress>();
            do
            {
                var eventsOfCurrentPage = resultPage.CurrentPage.ToList();
                allEvents.AddRange(eventsOfCurrentPage);

                resultPage = resultPage.NextPageRequest?.GetAsync().Result;
            } while (resultPage != null);

            return allEvents;
        }

        private IEnumerable<RoomReply> ConvertToRoomReplies(IEnumerable<EmailAddress> emailAddresses)
        {
            return emailAddresses
                .Select(e => new RoomReply()
                {
                    Name = e.Name,
                    RoomId = e.Address
                });
        }
    }
}
