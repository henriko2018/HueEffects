using System.Collections.Generic;
using HueEffects.Web.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Q42.HueApi;
using Q42.HueApi.Interfaces;
using Q42.HueApi.Models.Groups;

namespace HueEffects.Web.IntegrationTests
{
    public class IntegrationTestStartup : Startup
    {
        public IntegrationTestStartup(IConfiguration configuration) : base(configuration)
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
