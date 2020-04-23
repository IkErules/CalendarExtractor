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
        public async Task<IEnumerable<RoomReply>> GetAllRoomsFor(AzureRequestModel azureRequest)
        {
            var calendar = CreateCalendar(azureRequest);
            if (calendar == null) return new List<RoomReply>();

            var request = CreateAzureRequest(calendar, azureRequest);
            // This switch must be set before creating the GrpcChannel/HttpClient for calls without TLS connection.
            AppContext.SetSwitch(
                "System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

            var serverAddress = Environment.GetEnvironmentVariable("API_URL") ?? "test";
            Console.WriteLine($"Environment Variable API URL: {serverAddress}");

            using var channel = GrpcChannel.ForAddress(serverAddress);
            var grpcClient = new CalendarAccess.CalendarAccessClient(channel);

            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

            using var streamingCall = grpcClient.get_all_rooms_of_organisation(request, cancellationToken: cts.Token);

            var replyEventList = new List<RoomReply>();
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

        public async Task<IEnumerable<CalendarInformationReply>> ReadCalendarEventsFor(AzureRequestModel azureRequest)
        {
            var calendar = CreateCalendar(azureRequest);
            if (calendar == null) return new List<CalendarInformationReply>();

            var request = CreateAzureRequest(calendar, azureRequest);
            // This switch must be set before creating the GrpcChannel/HttpClient for calls without TLS connection.
            AppContext.SetSwitch(
                "System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

            var serverAddress = Environment.GetEnvironmentVariable("API_URL") ?? "test";
            Console.WriteLine($"Environment Variable API URL: {serverAddress}");

            using var channel = GrpcChannel.ForAddress(serverAddress);
            var grpcClient = new CalendarAccess.CalendarAccessClient(channel);

            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            using var streamingCall = grpcClient.get_calendar_information(request, cancellationToken: cts.Token);

            var replyEventList = new List<CalendarInformationReply>();

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

        private CalendarInformationRequest.Types.Calendar CreateCalendar(AzureRequestModel request)
        {

            var startTime = request.StartTime;
            var endTime = request.EndTime;

            Console.WriteLine("Beginn Abfrage: " + startTime.ToString("g"));
            Console.WriteLine("Ende Abfrage: " + endTime.ToString("g"));

            return new CalendarInformationRequest.Types.Calendar
            {
                CalendarId = request.CalendarId,
                BeginTime = CreateUnixTimeOf(startTime),
                EndTime = CreateUnixTimeOf(endTime)
            };
        }
        
        private CalendarInformationRequest CreateAzureRequest(CalendarInformationRequest.Types.Calendar calendar, AzureRequestModel model)
        {

            return new CalendarInformationRequest
            {
                Client = new CalendarInformationRequest.Types.Client
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
