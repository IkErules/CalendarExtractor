using System;
using System.Globalization;
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
            return Task.FromResult(new AzureReply
            {
                Message = $"Hello {request.ClientId} your Secret is {request.ClientSecret} and you belong" +
                          $"to TenantID {request.TenantId}"
            });
        }

        private void Test(AzureRequest request)
        {
            var authority = string.Format(CultureInfo.InvariantCulture, "https://login.microsoftonline.com/{0}", 
                request.TenantId);
            const string scopesString = "https://graph.microsoft.com/.default";
            var scopes = scopesString.Split(';');

            var authProvider = new DeviceCodeAuthProvider(request.ClientId, request.ClientSecret, authority, scopes);

            // Initialize Graph client
            var graphHelper = new GraphHelper(authProvider);

            ListCalendarEvents(graphHelper);
        }

        private void ListCalendarEvents(GraphHelper graphHelper)
        {
            var events = graphHelper.GetEventsAsync().Result;

            Console.WriteLine("Events:");

            foreach (var calendarEvent in events)
            {
                // List of Users
                //Console.WriteLine($"DisplayName: {calendarEvent.DisplayName}");
                //Console.WriteLine($"Mail: {calendarEvent.Mail}");
                //Console.WriteLine($"UserPrincipalName: {calendarEvent.UserPrincipalName}");
                //Console.WriteLine($"Id: {calendarEvent.Id}");

                Console.WriteLine($"Subject: {calendarEvent.Subject}");
                Console.WriteLine($"  Organizer: {calendarEvent.Organizer.EmailAddress.Name}");
                Console.WriteLine($"  Start: {FormatDateTimeTimeZone(calendarEvent.Start)}");
                Console.WriteLine($"  End: {FormatDateTimeTimeZone(calendarEvent.End)}");
            }
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
