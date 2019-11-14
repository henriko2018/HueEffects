using System;
using System.Collections.Generic;
using System.Net.Http;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Q42.HueApi;
using Q42.HueApi.Interfaces;
using Q42.HueApi.Models.Groups;
using Xunit;

namespace HueEffects.Web.IntegrationTests
{
    /// <summary>
    /// One instance of this will be created per test collection.
    /// </summary>
    public class TestHostFixture : ICollectionFixture<WebApplicationFactory<Startup>>
    {
        public readonly HttpClient Client;
        public readonly CustomWebApplicationFactory WebApplicationFactory;

        public TestHostFixture()
        {
            WebApplicationFactory = new CustomWebApplicationFactory();
            Client = WebApplicationFactory.CreateClient(new WebApplicationFactoryClientOptions{AllowAutoRedirect = false});
        }
    }

    [CollectionDefinition("Integration tests collection")]
    public class IntegrationTestsCollection : ICollectionFixture<TestHostFixture>
    {
        // This class has no code, and is never created. Its purpose is simply
        // to be the place to apply [CollectionDefinition] and all the
        // ICollectionFixture<> interfaces.
    }

    public class CustomWebApplicationFactory : WebApplicationFactory<TestStartup>
    {
        protected override IWebHostBuilder CreateWebHostBuilder()
        {
            return WebHost
                .CreateDefaultBuilder(Array.Empty<string>())
                .UseStartup<TestStartup>();
        }
    }

    public class TestStartup : Startup
    {
        public TestStartup(IConfiguration configuration) : base(configuration)
        {
        }
        
        protected override void AddPhilipsHueClient(IServiceCollection services)
        {
            var mock = new Mock<ILocalHueClient>();
            mock.Setup(client => client.GetGroupsAsync())
                .ReturnsAsync(new[] { new Group { Id = "1", Name = "Group 1" } });
            mock.Setup(client => client.GetGroupAsync(It.IsAny<string>()))
                .ReturnsAsync((string groupId) => new Group { Id = groupId, Lights = new List<string> { "1", "2" } });
            mock.Setup(client => client.GetLightAsync(It.IsAny<string>()))
                .ReturnsAsync((string lightId) => new Light
                {
                    Id = lightId,
                    Capabilities = new LightCapabilities
                        { Control = new Control { ColorTemperature = new ColorTemperature() } }
                });
            services.AddSingleton(mock.Object);
        }
    }
}
