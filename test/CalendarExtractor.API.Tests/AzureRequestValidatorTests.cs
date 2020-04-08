using System;
using CalendarExtractor.API.Helper;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Xunit;

namespace CalendarExtractor.API.Tests
{
    public class AzureRequestValidatorTests
    {
        private const string ValidGuidString = "816c3a23-7568-40a1-b12a-c65795fcbd92";

        [Fact]
        public void Validate_EmptyParams_ThrowsError()
        {
            // given
            var request = new AzureRequest
            {
                Client = new AzureRequest.Types.Client
                {
                    ClientId = string.Empty,
                    ClientSecret = string.Empty,
                    TenantId = string.Empty,
                },
                Calendar = new AzureRequest.Types.Calendar
                {
                    CalendarId = string.Empty,
                    BeginnDateTime = CreateTimestampOf(DateTime.Now),
                    EndDateTime = CreateTimestampOf(DateTime.Now.AddDays(10))
                }
            };

            // when
            var ex = Assert.Throws<RpcException>(() => new AzureRequestValidator().Validate(request));

            // then
            Assert.Equal(StatusCode.InvalidArgument, ex.StatusCode);
            Assert.Contains("clientid, tenantid, clientsecret, calendarid must not be empty", ex.Message);
        }

        [Fact]
        public void Validate_InvalidGuids_ThrowsError()
        {
            // given
            var invalidGuidString = "816c3a23-7568-40a1-b12-c65795fcbd";

            var request = new AzureRequest
            {
                Client = new AzureRequest.Types.Client
                {
                    ClientId = invalidGuidString,
                    ClientSecret = "mySecret",
                    TenantId = ValidGuidString,
                },
                Calendar = new AzureRequest.Types.Calendar
                {
                    CalendarId = "myCalendar@microsoft.com",
                    BeginnDateTime = CreateTimestampOf(DateTime.Now),
                    EndDateTime = CreateTimestampOf(DateTime.Now.AddDays(10))
                }
            };

            // when
            var ex = Assert.Throws<RpcException>(() => new AzureRequestValidator().Validate(request));

            // then
            Assert.Equal(StatusCode.InvalidArgument, ex.StatusCode);
            Assert.Contains("clientid is not a valid GUID", ex.Message);
        }

        [Fact]
        public void Validate_NoClientSent_ThrowsError()
        {
            // given
            var request = new AzureRequest
            {
                Calendar = new AzureRequest.Types.Calendar
                {
                    CalendarId = "myCalendar@microsoft.com",
                    BeginnDateTime = CreateTimestampOf(DateTime.Now),
                    EndDateTime = CreateTimestampOf(DateTime.Now.AddDays(10))
                }
            };

            // when
            var ex = Assert.Throws<RpcException>(() => new AzureRequestValidator().Validate(request));

            // then
            Assert.Equal(StatusCode.InvalidArgument, ex.StatusCode);
            Assert.Contains("clientid, tenantid, clientsecret must not be empty", ex.Message);
        }

        [Fact]
        public void Validate_NoErrorsFound()
        {
            // given
            var request = new AzureRequest
            {
                Client = new AzureRequest.Types.Client
                {
                    ClientId = ValidGuidString,
                    ClientSecret = "mySecret",
                    TenantId = ValidGuidString,
                },
                Calendar = new AzureRequest.Types.Calendar
                {
                    CalendarId = "myCalendar@microsoft.com",
                    BeginnDateTime = CreateTimestampOf(DateTime.Now),
                    EndDateTime = CreateTimestampOf(DateTime.Now.AddDays(10))
                }
            };

            // when
            var actualRequestIsValid = new AzureRequestValidator().Validate(request);

            // then
            Assert.True(actualRequestIsValid);
        }

        private Timestamp CreateTimestampOf(DateTime dateTime)
        {
            DateTimeOffset offset = dateTime;

            return offset.ToTimestamp();
        }
    }
}
