using System;
using System.Linq;
using Grpc.Core;
using Microsoft.Extensions.Logging;

namespace CalendarExtractor.API.Helper
{
    public class AzureRequestValidator : IRequestValidator
    {
        private const string EmptyStringMessage = "must not be empty";
        private const string NoValidGuidErrorMessage = "is not a valid GUID";

        private readonly Metadata _errors = new Metadata();
        private ILogger<AzureRequestValidator> _logger;

        public AzureRequestValidator(ILogger<AzureRequestValidator> logger)
        {
            _logger = logger;
        }

        public bool Validate(AzureRequest request)
        {
            ValidateEmptyString(request);
            ValidateValidGuid(request);

            return true;
        }

        private void ValidateValidGuid(AzureRequest request)
        {
            if (!IsValidGuid(request.Client.ClientId))
                _errors.Add(new Metadata.Entry("ClientId", NoValidGuidErrorMessage));

            if (!IsValidGuid(request.Client.TenantId))
                _errors.Add(new Metadata.Entry("TenantId", NoValidGuidErrorMessage));

            if (_errors.Any())
            {
                _logger.LogInformation($"Validation Errors: Not valid Guids in Request {request}");
                throw new RpcException(new Status(StatusCode.InvalidArgument, $"{GetErrorKeys()} {NoValidGuidErrorMessage}"), _errors);
            }
                
        }

        private bool IsValidGuid(string toValidate)
        {
            return Guid.TryParse(toValidate, out _);
        }

        private void ValidateEmptyString(AzureRequest request)
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
