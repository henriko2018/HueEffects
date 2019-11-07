using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace HueEffects.Web.IntegrationTests
{
    public class CustomWebApplicationFactory : WebApplicationFactory<IntegrationTestStartup>
    {
        protected override IWebHostBuilder CreateWebHostBuilder()
        {
            return WebHost.CreateDefaultBuilder(new string[0])
                .UseStartup<IntegrationTestStartup>();
        }
    }
}
