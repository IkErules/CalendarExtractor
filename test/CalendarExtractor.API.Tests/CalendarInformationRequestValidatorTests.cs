﻿using System;
using CalendarExtractor.API.Helper;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CalendarExtractor.API.Tests
{
    public class CalendarInformationRequestValidatorTests
    {
        private const string ValidGuidString = "816c3a23-7568-40a1-b12a-c65795fcbd92";
        private ILogger<CalendarInformationRequestValidator> _logger;

        public CalendarInformationRequestValidatorTests()
        {
            _logger = new Mock<ILogger<CalendarInformationRequestValidator>>().Object;
        }

        [Fact]
        public void Validate_EmptyParams_ThrowsError()
        {
            // given
            var request = new CalendarInformationRequest
            {
                Client = new CalendarInformationRequest.Types.Client
                {
                    ClientId = string.Empty,
                    TenantId = string.Empty,
                },
                Calendar = new CalendarInformationRequest.Types.Calendar
                {
                    CalendarId = string.Empty,
                    BeginTime = CreateUnixTimeOf(DateTime.Now),
                    EndTime = CreateUnixTimeOf(DateTime.Now.AddDays(10))
                }
            };

            // when
            var ex = Assert.Throws<RpcException>(() => new CalendarInformationRequestValidator(_logger).Validate(request));

            // then
            Assert.Equal(StatusCode.InvalidArgument, ex.StatusCode);
            Assert.Contains("clientid, tenantid, clientsecret, calendarid must not be empty", ex.Message);
        }

        [Fact]
        public void Validate_InvalidGuids_ThrowsError()
        {
            // given
            var invalidGuidString = "816c3a23-7568-40a1-b12-c65795fcbd";

            var request = new CalendarInformationRequest
            {
                Client = new CalendarInformationRequest.Types.Client
                {
                    ClientId = invalidGuidString,
                    ClientSecret = "mySecret",
                    TenantId = ValidGuidString,
                },
                Calendar = new CalendarInformationRequest.Types.Calendar
                {
                    CalendarId = "myCalendar@microsoft.com",
                    BeginTime = CreateUnixTimeOf(DateTime.Now),
                    EndTime = CreateUnixTimeOf(DateTime.Now.AddDays(10))
                }
            };

            // when
            var ex = Assert.Throws<RpcException>(() => new CalendarInformationRequestValidator(_logger).Validate(request));

            // then
            Assert.Equal(StatusCode.InvalidArgument, ex.StatusCode);
            Assert.Contains("clientid is not a valid GUID", ex.Message);
        }

        [Fact]
        public void Validate_NoClientSent_ThrowsError()
        {
            // given
            var request = new CalendarInformationRequest
            {
                Calendar = new CalendarInformationRequest.Types.Calendar
                {
                    CalendarId = "myCalendar@microsoft.com",
                    BeginTime = CreateUnixTimeOf(DateTime.Now),
                    EndTime = CreateUnixTimeOf(DateTime.Now.AddDays(10))
                }
            };

            // when
            var ex = Assert.Throws<RpcException>(() => new CalendarInformationRequestValidator(_logger).Validate(request));

            // then
            Assert.Equal(StatusCode.InvalidArgument, ex.StatusCode);
            Assert.Contains("clientid, tenantid, clientsecret must not be empty", ex.Message);
        }

        [Fact]
        public void Validate_NoCalendarSent_ThrowsError()
        {
            // given
            var request = new CalendarInformationRequest
            {
                Client = new CalendarInformationRequest.Types.Client
                {
                    ClientId = ValidGuidString,
                    ClientSecret = "mySecret",
                    TenantId = ValidGuidString,
                }
            };

            // when
            var ex = Assert.Throws<RpcException>(() => new CalendarInformationRequestValidator(_logger).Validate(request));

            // then
            Assert.Equal(StatusCode.InvalidArgument, ex.StatusCode);
            Assert.Contains("calendarid must not be empty", ex.Message);
        }

        [Fact]
        public void Validate_NoErrorsFound()
        {
            // given
            var request = new CalendarInformationRequest
            {
                Client = new CalendarInformationRequest.Types.Client
                {
                    ClientId = ValidGuidString,
                    ClientSecret = "mySecret",
                    TenantId = ValidGuidString,
                },
                Calendar = new CalendarInformationRequest.Types.Calendar
                {
                    CalendarId = "myCalendar@microsoft.com",
                    BeginTime = CreateUnixTimeOf(DateTime.Now),
                    EndTime = CreateUnixTimeOf(DateTime.Now.AddDays(10))
                }
            };

            // when
            var actualRequestIsValid = new CalendarInformationRequestValidator(_logger).Validate(request);

            // then
            Assert.True(actualRequestIsValid);
        }

        [Fact]
        public void Validate_DefaultTimeValues_ThrowsException()
        {
            // given
            var request = new CalendarInformationRequest
            {
                Client = new CalendarInformationRequest.Types.Client
                {
                    ClientId = ValidGuidString,
                    ClientSecret = "mySecret",
                    TenantId = ValidGuidString,
                },
                Calendar = new CalendarInformationRequest.Types.Calendar
                {
                    CalendarId = "myCalendar@microsoft.com"
                }
            };

            // when
            var ex = Assert.Throws<RpcException>(() => new CalendarInformationRequestValidator(_logger).Validate(request));

            // then
            Assert.Equal(StatusCode.InvalidArgument, ex.StatusCode);
            Assert.Contains("timestamps not valid start_time and end_time", ex.Message);
        }

        [Fact]
        public void Validate_EndBeforeBeginTimestamps_ThrowsError()
        {
            // given
            var request = new CalendarInformationRequest
            {
                Client = new CalendarInformationRequest.Types.Client
                {
                    ClientId = ValidGuidString,
                    ClientSecret = "mySecret",
                    TenantId = ValidGuidString,
                },
                Calendar = new CalendarInformationRequest.Types.Calendar
                {
                    CalendarId = "myCalendar@microsoft.com",
                    BeginTime = CreateUnixTimeOf(DateTime.Now),
                    EndTime = CreateUnixTimeOf(DateTime.Now.AddSeconds(-5))
                }
            };

            // when
            var ex = Assert.Throws<RpcException>(() => new CalendarInformationRequestValidator(_logger).Validate(request));

            // then
            Assert.Equal(StatusCode.InvalidArgument, ex.StatusCode);
            Assert.Contains("timestamps not valid start_time and end_time", ex.Message);
        }

        [Fact]
        public void Validate_EndEqualToBeginTimestamp_NoError()
        {
            // given
            var timestamp = CreateUnixTimeOf(DateTime.Now);
            var request = new CalendarInformationRequest
            {
                Client = new CalendarInformationRequest.Types.Client
                {
                    ClientId = ValidGuidString,
                    ClientSecret = "mySecret",
                    TenantId = ValidGuidString,
                },
                Calendar = new CalendarInformationRequest.Types.Calendar
                {
                    CalendarId = "myCalendar@microsoft.com",
                    BeginTime = timestamp,
                    EndTime = timestamp
                }
            };

            // when
            var actualValid = new CalendarInformationRequestValidator(_logger).Validate(request);

            // then
            Assert.True(actualValid);
        }

        private long CreateUnixTimeOf(DateTime dateTime)
        {
            DateTimeOffset offset = dateTime;

            return offset.ToUnixTimeSeconds();
        }
    }
}
