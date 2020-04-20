using System;
using System.Linq;
using Grpc.Core;
using Microsoft.Extensions.Logging;

namespace CalendarExtractor.API.Helper
{
    public class AzureRequestValidator : IRequestValidator
    {
        private const string EmptyStringMessage = "must not be empty";
        private const string NoValidGuidErrorErrorMessage = "is not a valid GUID";
        private const string NotNullErrorMessage = "must not be null";
        private const string NotValidTimestampsErrorMessage = "endTimestamp must be not before beginTimestamp";

        private readonly Metadata _errors = new Metadata();
        private ILogger<AzureRequestValidator> _logger;

        public AzureRequestValidator(ILogger<AzureRequestValidator> logger)
        {
            _logger = logger;
        }

        public bool Validate(calendar_information_request request)
        {
            ValidateEmptyString(request);
            ValidateValidGuid(request);
            ValidateNotNullTimestamps(request);
            ValidateValidTimestamps(request);

            return true;
        }

        private void ValidateValidTimestamps(calendar_information_request request)
        {
            if (request.Calendar.EndTimestamp < request.Calendar.BeginTimestamp)
                _errors.Add(new Metadata.Entry("Timestamps", NotValidTimestampsErrorMessage));

            if (_errors.Any())
            {
                _logger.LogInformation($"Validation Errors: Not valid Timestamps in Request {request}");
                throw new RpcException(new Status(StatusCode.InvalidArgument, 
                    $"{GetErrorKeys()} {NotValidTimestampsErrorMessage}"), _errors);
            }
        }

        private void ValidateNotNullTimestamps(calendar_information_request request)
        {
            if (request.Calendar.BeginTimestamp == null)
                _errors.Add(new Metadata.Entry("BeginTimestamp", NotNullErrorMessage));

            if (request.Calendar.EndTimestamp == null)
                _errors.Add(new Metadata.Entry("EndTImestamp", NotNullErrorMessage));

            if (_errors.Any())
            {
                _logger.LogInformation($"Validation Errors: Not valid Timestamps in Request {request}");
                throw new RpcException(new Status(StatusCode.InvalidArgument, 
                    $"{GetErrorKeys()} {NotNullErrorMessage}"), _errors);
            }
        }

        private void ValidateValidGuid(calendar_information_request request)
        {
            if (!IsValidGuid(request.Client.ClientId))
                _errors.Add(new Metadata.Entry("ClientId", NoValidGuidErrorErrorMessage));

            if (!IsValidGuid(request.Client.TenantId))
                _errors.Add(new Metadata.Entry("TenantId", NoValidGuidErrorErrorMessage));

            if (_errors.Any())
            {
                _logger.LogInformation($"Validation Errors: Not valid Guids in Request {request}");
                throw new RpcException(new Status(StatusCode.InvalidArgument, 
                    $"{GetErrorKeys()} {NoValidGuidErrorErrorMessage}"), _errors);
            }
                
        }

        private bool IsValidGuid(string toValidate)
        {
            return Guid.TryParse(toValidate, out _);
        }

        private void ValidateEmptyString(calendar_information_request request)
        {
            if (string.IsNullOrEmpty(request.Client?.ClientId))
                _errors.Add(new Metadata.Entry("ClientId", EmptyStringMessage));

            if (string.IsNullOrEmpty(request.Client?.TenantId))
                _errors.Add(new Metadata.Entry("TenantId", EmptyStringMessage));

            if (string.IsNullOrEmpty(request.Client?.ClientSecret))
                _errors.Add(new Metadata.Entry("ClientSecret", EmptyStringMessage));

            if (string.IsNullOrEmpty(request.Calendar?.CalendarId))
                _errors.Add(new Metadata.Entry("CalendarId", EmptyStringMessage));

            if (_errors.Any())
            {
                _logger.LogInformation($"Validation Errors: No empty Strings in Request {request.ToString()}");
                throw new RpcException(new Status(StatusCode.InvalidArgument, $"{GetErrorKeys()} {EmptyStringMessage}"), _errors);
            }
        }

        private string GetErrorKeys()
        {
            return _errors
                    .Select(e => e.Key)
                    .Aggregate((a, b) => string.Join(", ", a, b));
        }
    }
}
