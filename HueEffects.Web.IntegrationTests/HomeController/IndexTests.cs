using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace HueEffects.Web.IntegrationTests.HomeController
{
    [Collection("Integration tests collection")]
    public class IndexTests
    {
        private readonly HttpClient _httpClient;

        public IndexTests(TestHostFixture testHostFixture)
        {
            _httpClient = testHostFixture.Client;
        }

        [Fact]
        public async Task Returns_OK()
        {
            var response = await _httpClient.GetAsync("/");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }
}
