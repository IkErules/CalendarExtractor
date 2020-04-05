using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CalendarExtractor.API.Tests
{
    public class AzureServiceTests
    {
        [Fact]
        public async void SayHello_CorrectResponseFromName()
        {
            // given

            var test = new AzureService(null);

        }
    }
}
