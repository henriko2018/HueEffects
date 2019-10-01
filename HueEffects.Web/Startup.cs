using System;
using System.Linq;
using HueEffects.Web.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Q42.HueApi;
using Q42.HueApi.Interfaces;

namespace HueEffects.Web
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });

            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
            
            AddPhilipsHueClient(services);
			//services.AddHostedService<BackgroundService>(); // Doesn't work - see https://github.com/aspnet/Extensions/issues/553
			services.AddSingleton<Microsoft.Extensions.Hosting.IHostedService, BackgroundService>();
			services.AddSingleton<StorageService>();
        }

        private void AddPhilipsHueClient(IServiceCollection services)
        {
            // Find bridge and so on.
            var locator = new HttpBridgeLocator();
            var bridges = locator.LocateBridgesAsync(TimeSpan.FromSeconds(5)).Result;
            // Assume it is the only one
            var bridge = bridges.SingleOrDefault();
            if (bridge == null)
                throw new ApplicationException("Found no Philips Hue Bridge.");
            var client = new LocalHueClient(bridge.IpAddress, "qcENpxmyTZpc8ZMJmrm4KY9-Sn8VTRHSXbr9JbdH"); // TODO: Store somewhere else
            services.AddSingleton<ILocalHueClient>(client);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseCookiePolicy();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
