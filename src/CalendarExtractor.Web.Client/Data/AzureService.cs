using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CalendarExtractor.API;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Net.Client;

namespace CalendarExtractor.Web.Client.Data
{
    public class AzureService
    {
        public async Task<IEnumerable<AzureReplyEvent>> ReadCalendarEventsFor(AzureRequestModel azureRequest)
        {
            var calendar = CreateCalendar(azureRequest.Minutes, azureRequest.CalendarId);
            if (calendar == null) return new List<AzureReplyEvent>();

            var request = CreateAzureRequest(calendar, azureRequest);

            // This switch must be set before creating the GrpcChannel/HttpClient for calls without TLS connection.
            AppContext.SetSwitch(
                "System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

            var serverAddress = Environment.GetEnvironmentVariable("API_URL") ?? "test";
            Console.WriteLine($"Environment Variable API URL: {serverAddress}");

            using var channel = GrpcChannel.ForAddress(serverAddress);
            var azureClient = new calendar_access.calendar_accessClient(channel);

            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

            using var streamingCall = azureClient.GetCalendarInformation(request, cancellationToken: cts.Token);

            //using var streamingCall = isTest
            //    ? azureClient.TestGetCalendarInformation(request, cancellationToken: cts.Token)
            //    : azureClient.GetCalendarInformation(request, cancellationToken: cts.Token);

            var replyEventList = new List<AzureReplyEvent>();

            try
            {
                await foreach (var replyEvent in streamingCall.ResponseStream.ReadAllAsync(cts.Token))
                {
                    replyEventList.Add(replyEvent);
                }

            }
            catch (RpcException ex) when (ex.StatusCode == StatusCode.Cancelled)
            {
                Console.WriteLine("Stream cancelled because of cancellation token.");
            }
            catch (RpcException ex)
            {
                Console.WriteLine($"Exception occured: {ex}");
            }

            return replyEventList;
        }

        private AzureRequest.Types.Calendar CreateCalendar(int minutes, string calendarId)
        {
            var startTime = DateTime.Now.ToLocalTime();
            var endTime = startTime.AddMinutes(minutes);

            Console.WriteLine("Beginn Abfrage: " + startTime.ToString("g"));
            Console.WriteLine("Ende Abfrage: " + endTime.ToString("g"));

            return new AzureRequest.Types.Calendar
            {
                CalendarId = calendarId,
                BeginTimestamp = CreateTimestampOf(startTime),
                EndTimestamp = CreateTimestampOf(endTime)
            };
        }
        
        private AzureRequest CreateAzureRequest(AzureRequest.Types.Calendar calendar, AzureRequestModel model)
        {

            return new AzureRequest
            {
                Client = new AzureRequest.Types.Client
                {
                    ClientId = model.ClientId,
                    ClientSecret = model.ClientSecret,
                    TenantId = model.TenantId,
                },
                Calendar = calendar
            };
        }

        private Timestamp CreateTimestampOf(DateTime dateTime)
        {
            DateTimeOffset offset = dateTime;

            return offset.ToTimestamp();
        }
    }
}
