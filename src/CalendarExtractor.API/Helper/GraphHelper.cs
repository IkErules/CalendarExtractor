using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Graph;

namespace CalendarExtractor.API.Helper
{
    public class GraphHelper
    {
        private GraphServiceClient _graphClient;

        public GraphHelper(IAuthenticationProvider authProvider)
        {
            _graphClient = new GraphServiceClient(authProvider);
        }

        public async Task<IEnumerable<Event>> GetEventsAsync()
        {
            try
            {
                //var resultPage = await graphClient.Users.Request().GetAsync();

                // GET /me/events
                var resultPage = await _graphClient.Users["b203f6a3-c3bd-48bf-9183-563b78ee852b"].Events.Request()
                    // Only return the fields used by the application
                    .Select("subject,organizer,start,end")
                    // Sort results by when they were created, newest first
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