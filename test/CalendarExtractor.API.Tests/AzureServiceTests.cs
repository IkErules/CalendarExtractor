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
            var logger = new Mock<ILogger<AzureService>>();
            var azureService = new AzureService(logger.Object);
            var expectedName = "UnitTest";
            var request = new AzureRequest { Name = expectedName };
            var expectedMessage = $"Hello {expectedName}";

            // when
            var actualResponse = await azureService.SayHello(request, null);

            // then
            Assert.Equal(expectedMessage, actualResponse.Message);

        }
    }
}
