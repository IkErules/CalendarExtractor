using System;
using CalendarExtractor.API.Helper;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CalendarExtractor.API.Tests
{
    public class AzureRequestValidatorTests
    {
        private const string ValidGuidString = "816c3a23-7568-40a1-b12a-c65795fcbd92";
        private ILogger<AzureRequestValidator> _logger;

        public AzureRequestValidatorTests()
        {
            _logger = new Mock<ILogger<AzureRequestValidator>>().Object;
        }

        [Fact]
        public void Validate_EmptyParams_ThrowsError()
        {
            // given
            var request = new AzureRequest
            {
                Client = new AzureRequest.Types.Client
                {
                    ClientId = string.Empty,
                    TenantId = string.Empty,
                },
                Calendar = new AzureRequest.Types.Calendar
                {
                    CalendarId = string.Empty,
                    BeginTimestamp = CreateTimestampOf(DateTime.Now),
                    EndTimestamp = CreateTimestampOf(DateTime.Now.AddDays(10))
                }
            };

            // when
            var ex = Assert.Throws<RpcException>(() => new AzureRequestValidator(_logger).Validate(request));

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
                    BeginTimestamp = CreateTimestampOf(DateTime.Now),
                    EndTimestamp = CreateTimestampOf(DateTime.Now.AddDays(10))
                }
            };

            // when
            var ex = Assert.Throws<RpcException>(() => new AzureRequestValidator(_logger).Validate(request));

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
                    BeginTimestamp = CreateTimestampOf(DateTime.Now),
                    EndTimestamp = CreateTimestampOf(DateTime.Now.AddDays(10))
                }
            };

            // when
            var ex = Assert.Throws<RpcException>(() => new AzureRequestValidator(_logger).Validate(request));

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
                    BeginTimestamp = CreateTimestampOf(DateTime.Now),
                    EndTimestamp = CreateTimestampOf(DateTime.Now.AddDays(10))
                }
            };

            // when
            var actualRequestIsValid = new AzureRequestValidator(_logger).Validate(request);

            // then
            Assert.True(actualRequestIsValid);
        }

        [Fact]
        public void Validate_NullTimestamps_ThrowsException()
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
                    EndTimestamp = null
                }
            };

            // when
            var ex = Assert.Throws<RpcException>(() => new AzureRequestValidator(_logger).Validate(request));

            // then
            Assert.Equal(StatusCode.InvalidArgument, ex.StatusCode);
            Assert.Contains("begintimestamp, endtimestamp must not be null", ex.Message);
        }

        [Fact]
        public void Validate_EndBeforeBeginTimestamps_ThrowsError()
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
                    BeginTimestamp = CreateTimestampOf(DateTime.Now),
                    EndTimestamp = CreateTimestampOf(DateTime.Now.AddSeconds(-5))
                }
            };

            // when
            var ex = Assert.Throws<RpcException>(() => new AzureRequestValidator(_logger).Validate(request));

            // then
            Assert.Equal(StatusCode.InvalidArgument, ex.StatusCode);
            Assert.Contains("timestamps endTimestamp must be not before beginTimestamp", ex.Message);
        }

        [Fact]
        public void Validate_EndEqualToBeginTimestamp_NoError()
        {
            // given
            var timestamp = CreateTimestampOf(DateTime.Now);
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
                    BeginTimestamp = timestamp,
                    EndTimestamp = timestamp
                }
            };

            // when
            var actualValid = new AzureRequestValidator(_logger).Validate(request);

            // then
            Assert.True(actualValid);
        }

        private Timestamp CreateTimestampOf(DateTime dateTime)
        {
            DateTimeOffset offset = dateTime;

            return offset.ToTimestamp();
        }
    }
}
