using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HueEffects.Web.Models;
using HueEffects.Web.Services;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HueEffects.Web.IntegrationTests.HomeController
{
    [Collection("Integration tests collection")]
	public class UseXasEffectTests : IAsyncLifetime
	{
        private readonly HttpClient _client;
        private readonly IStorageService _storageService;
        private FormUrlEncodedContent _testContent;

        public UseXasEffectTests(TestHostFixture testHostFixture)
        {
            _storageService = testHostFixture.WebApplicationFactory.Services.GetRequiredService<IStorageService>();
            _client = testHostFixture.Client;
        }

        public async Task InitializeAsync()
        {
            var responseBody = await _client.GetStringAsync("/Home");

            // Get request verification token form field:
            var regex = new Regex("<input name=\"__RequestVerificationToken\" .*? value=\"([^\"]*)\"");
            var match = regex.Match(responseBody);
            var requestVerificationToken = match.Groups[1].Value;

            // Setup test data to post:
            _testContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("__RequestVerificationToken", requestVerificationToken), 
                new KeyValuePair<string, string>("XmasEffectConfig.LightGroup", "1"),
                new KeyValuePair<string, string>("XmasEffectConfig.CycleLength", "123"),
            });
        }

        public Task DisposeAsync()
        {
            return Task.CompletedTask;
        }
		
        [Fact]
		public async Task Returns_redirect()
        {
            var response = await _client.PostAsync("/Home/UseXmasEffect", _testContent);
            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Equal("/", response.Headers.Location.ToString());
        }
    
        [Fact]
        public async Task Persists_updated_configuration()
        {
            // Write default values
            await _storageService.SaveConfig(new XmasEffectConfig());

            // Post
            await _client.PostAsync("/Home/UseXmasEffect", _testContent);

            // Check that new values were persisted.
            var config = await _storageService.LoadConfig<XmasEffectConfig>();
            Assert.Equal(123, config.CycleLength);
        }
    }
}
