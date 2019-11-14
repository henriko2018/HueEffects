using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace HueEffects.Web.IntegrationTests.HomeController
{
    [Collection("Integration tests collection")]
    public class IndexTests    
    {
        private readonly ITestOutputHelper _testOutputHelper;
        private readonly HttpClient _httpClient;

        public IndexTests(TestHostFixture testHostFixture, ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
            _httpClient = testHostFixture.Client;
        }

        [Fact]
        public async Task Returns_OK()
        {
            var response = await _httpClient.GetAsync("/");
            _testOutputHelper.WriteLine(await response.Content.ReadAsStringAsync());
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }
}
