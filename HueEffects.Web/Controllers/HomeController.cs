using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HueEffects.Web.EffectHandlers;
using Microsoft.AspNetCore.Mvc;
using HueEffects.Web.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Q42.HueApi.Interfaces;
using Q42.HueApi.Models.Groups;
using SunCalcNet;
using SunCalcNet.Model;

namespace HueEffects.Web.Controllers
{
    public class HomeController : Controller
    {
        private const string ActiveHandlerKey = "ActiveHandler";
        private readonly ILocalHueClient _hueClient;
        private readonly IHostingEnvironment _environment;
        private readonly IMemoryCache _cache;
        private readonly ILoggerFactory _loggerFactory;

        public HomeController(ILocalHueClient hueClient, IHostingEnvironment environment, IMemoryCache cache, ILoggerFactory loggerFactory)
        {
            _hueClient = hueClient;
            _environment = environment;
            _cache = cache;
            _loggerFactory = loggerFactory;
        }

        #region Http action methods

        public async Task<IActionResult> Index()
        {
            var (sunSet, sunRise) = GetSunPhases();
            var model = new Effects
            {
                LightGroups = await _hueClient.GetGroupsAsync(),
                XmasEffect = new XmasEffect {Active = false, CycleLength = 60},
                WarmupEffect = new WarmupEffect
                {
                    Active = false, TurnOnAt = sunSet, WarmUp = new TimeSpan(0, 2, 0, 0),
                    CoolDown = new TimeSpan(0, 2, 0, 0), TurnOffAt = sunRise,
                    UseMinTemp = WarmupEffect.MinTemp,
                    UseMaxTemp = WarmupEffect.MaxTemp
                }
            };
            _cache.TryGetValue(ActiveHandlerKey, out EffectHandler activeHandler);
            model.XmasEffect.Active = activeHandler != null && activeHandler.GetType() == typeof(XmasHandler);
            model.WarmupEffect.Active = activeHandler != null && activeHandler.GetType() == typeof(WarmupHandler);

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UseXmasEffect(Effects config)
        {
            if (ModelState.IsValid)
                return await UseEffect(config, new XmasHandler(config.XmasEffect, _loggerFactory, _hueClient));
            return View("Index", config);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UseWarmupEffect(Effects config)
        {
            if (ModelState.IsValid)
                return await UseEffect(config, new WarmupHandler(config.WarmupEffect, _loggerFactory, _hueClient));
            return View("Index", config);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        #endregion

        #region Private methods

        private static (DateTime sunSet, DateTime sunRise) GetSunPhases()
        {
            var sunPhases = SunCalc.GetSunPhases(DateTime.Now, 59.4664329, 18.0842061).ToList();
            var sunSet = sunPhases.Single(sp => sp.Name.Value == SunPhaseName.Sunset.Value).PhaseTime.ToLocalTime();
            var sunRise = sunPhases.Single(sp => sp.Name.Value == SunPhaseName.Sunrise.Value).PhaseTime.ToLocalTime();
            return (sunSet, sunRise);
        }

        private async Task<IActionResult> UseEffect(Effects config, EffectHandler handler)
        {
            await SaveConfig(config);

            // Stop previous handler if there is one
            if (_cache.TryGetValue(ActiveHandlerKey, out EffectHandler activeHandler))
                activeHandler.Stop();

            handler.Start();
            _cache.Set(ActiveHandlerKey, handler);
            return RedirectToAction("Index");
        }

        private async Task<Effects> SaveConfig(Effects newConfig)
        {
            var oldConfig = new Effects();
            var path = Path.Combine(_environment.ContentRootPath, "EffectsConfig.json");
            if (System.IO.File.Exists(path))
            {
                var s = await System.IO.File.ReadAllTextAsync(path);
                oldConfig = JsonConvert.DeserializeObject<Effects>(s);
            }
            if (newConfig.WarmupEffect != null)
                oldConfig.WarmupEffect = newConfig.WarmupEffect;
            if (newConfig.XmasEffect != null)
                oldConfig.XmasEffect = newConfig.XmasEffect;
            await System.IO.File.WriteAllTextAsync(path, JsonConvert.SerializeObject(oldConfig));
            return newConfig;
        }

        #endregion
    }
}
