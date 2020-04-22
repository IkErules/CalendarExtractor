using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CalendarExtractor.API;
using Grpc.Core;
using Grpc.Net.Client;

namespace CalendarExtractor.Web.Client.Data
{
    public class AzureService
    {
        public async Task<IEnumerable<room_reply>> GetAllRoomsFor(AzureRequestModel azureRequest)
        {
            var calendar = CreateCalendar(azureRequest);
            if (calendar == null) return new List<room_reply>();

            var request = CreateAzureRequest(calendar, azureRequest);
            // This switch must be set before creating the GrpcChannel/HttpClient for calls without TLS connection.
            AppContext.SetSwitch(
                "System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

            var serverAddress = Environment.GetEnvironmentVariable("API_URL") ?? "test";
            Console.WriteLine($"Environment Variable API URL: {serverAddress}");

            using var channel = GrpcChannel.ForAddress(serverAddress);
            var grpcClient = new calendar_access.calendar_accessClient(channel);

            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

            using var streamingCall = grpcClient.get_all_rooms_of_organisation(request, cancellationToken: cts.Token);

            var replyEventList = new List<room_reply>();
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
                throw ex;
            }
            catch (RpcException ex)
            {
                Console.WriteLine($"Exception occured: {ex}");
            }

            return replyEventList;
        }

        public async Task<IEnumerable<calendar_information_reply>> ReadCalendarEventsFor(AzureRequestModel azureRequest)
        {
            var calendar = CreateCalendar(azureRequest);
            if (calendar == null) return new List<calendar_information_reply>();

            var request = CreateAzureRequest(calendar, azureRequest);
            // This switch must be set before creating the GrpcChannel/HttpClient for calls without TLS connection.
            AppContext.SetSwitch(
                "System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

            var serverAddress = Environment.GetEnvironmentVariable("API_URL") ?? "test";
            Console.WriteLine($"Environment Variable API URL: {serverAddress}");

            using var channel = GrpcChannel.ForAddress(serverAddress);
            var grpcClient = new calendar_access.calendar_accessClient(channel);

            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            using var streamingCall = grpcClient.get_calendar_information(request, cancellationToken: cts.Token);

            //using var streamingCall = isTest
            //    ? azureClient.TestGetCalendarInformation(request, cancellationToken: cts.Token)
            //    : azureClient.GetCalendarInformation(request, cancellationToken: cts.Token);

            var replyEventList = new List<calendar_information_reply>();

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

        private calendar_information_request.Types.Calendar CreateCalendar(AzureRequestModel request)
        {

            var startTime = request.StartTime;
            var endTime = request.EndTime;

            Console.WriteLine("Beginn Abfrage: " + startTime.ToString("g"));
            Console.WriteLine("Ende Abfrage: " + endTime.ToString("g"));

            return new calendar_information_request.Types.Calendar
            {
                CalendarId = request.CalendarId,
                BeginTime = CreateUnixTimeOf(startTime),
                EndTime = CreateUnixTimeOf(endTime)
            };
        }
        
        private calendar_information_request CreateAzureRequest(calendar_information_request.Types.Calendar calendar, AzureRequestModel model)
        {

            return new calendar_information_request
            {
                Client = new calendar_information_request.Types.Client
                {
                    ClientId = model.ClientId,
                    ClientSecret = model.ClientSecret,
                    TenantId = model.TenantId,
                },
                Calendar = calendar
            };
        }

        private long CreateUnixTimeOf(DateTime dateTime)
        {
            DateTimeOffset offset = dateTime;

            return offset.ToUnixTimeSeconds();
        }
    }
}
