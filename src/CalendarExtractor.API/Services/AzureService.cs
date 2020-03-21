using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
                Message = "Hello " + request.Name
            });
        }
    }
}
