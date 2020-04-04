using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Graph;

namespace CalendarExtractor.API.Helper
{
    public class GraphCalendarHelper
    {
        private readonly GraphServiceClient _graphClient;

        public GraphCalendarHelper(IAuthenticationProvider authProvider)
        {
            _graphClient = new GraphServiceClient(authProvider);
        }

        public async Task<IEnumerable<Event>> GetEventsAsync(string calendarId)
        {
            try
            {
                var resultPage = await _graphClient.Users[calendarId].Events.Request()
                    //.Select("subject,organizer,start,end")
                    .OrderBy("createdDateTime DESC")
                    .GetAsync();

                return resultPage.CurrentPage;
            }
            catch (ServiceException ex)
            {
                Console.WriteLine($"Error getting events: {ex.Message}");
                return null;
            }
        }
    }
}