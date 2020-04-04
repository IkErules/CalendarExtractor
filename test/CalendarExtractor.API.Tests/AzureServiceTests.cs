using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CalendarExtractor.API.Tests
{
    public class AzureServiceTests
    {
        //[Fact]
        //public async void SayHello_CorrectResponseFromName()
        //{
        //    // given
        //    var logger = new Mock<ILogger<AzureService>>();
        //    var azureService = new AzureService(logger.Object);
        //    var expectedClientId = "1111-11111-11-111";
        //    var expecteTenantId = "1111-11111-11-111";
        //    var expectedClientSecret = "My-Secret";
        //    var request = new AzureRequest
        //    {
        //        ClientId = expectedClientId,
        //        ClientSecret = expectedClientSecret,
        //        TenantId = expecteTenantId
        //    };
        //    var expectedMessage = $"Hello {expectedClientId} your Secret is {expectedClientSecret} and you belong" +
        //                          $"to TenantID {expecteTenantId}";

        //    // when
        //    var actualResponse = await azureService.GetCalendarInformation(request, null);

        //    // then
        //    Assert.Equal(expectedMessage, actualResponse.Message);

        //}
    }
}
