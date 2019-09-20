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
            // TODO: Read from config file
            var model = new EffectsConfig
            {
                LightGroups = await _hueClient.GetGroupsAsync(),
                XmasEffectConfig = new XmasEffectConfig(),
                WarmupEffectConfig = new WarmupEffectConfig()
            };
            _cache.TryGetValue(ActiveHandlerKey, out EffectHandler activeHandler);
            model.XmasEffectConfig.Active = activeHandler != null && activeHandler.GetType() == typeof(XmasHandler);
            model.WarmupEffectConfig.Active = activeHandler != null && activeHandler.GetType() == typeof(WarmupHandler);

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UseXmasEffect(EffectsConfig config)
        {
            if (ModelState.IsValid)
                return await UseEffect(config, new XmasHandler(config.XmasEffectConfig, _loggerFactory, _hueClient));
            return View("Index", config);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UseWarmupEffect(EffectsConfig config)
        {
            if (ModelState.IsValid)
            {
                return await UseEffect(config,
                    new WarmupHandler(config.WarmupEffectConfig, _loggerFactory, _hueClient));
            }

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

        private async Task<IActionResult> UseEffect(EffectsConfig config, EffectHandler handler)
        {
            await SaveConfig(config);

            // Stop previous handler if there is one
            if (_cache.TryGetValue(ActiveHandlerKey, out EffectHandler activeHandler))
                activeHandler.Stop();

            handler.Start();
            _cache.Set(ActiveHandlerKey, handler);
            return RedirectToAction("Index");
        }

        private async Task<EffectsConfig> SaveConfig(EffectsConfig newConfig)
        {
            var oldConfig = new EffectsConfig();
            var path = Path.Combine(_environment.ContentRootPath, "EffectsConfig.json");
            if (System.IO.File.Exists(path))
            {
                var s = await System.IO.File.ReadAllTextAsync(path);
                oldConfig = JsonConvert.DeserializeObject<EffectsConfig>(s);
            }
            if (newConfig.WarmupEffectConfig != null)
                oldConfig.WarmupEffectConfig = newConfig.WarmupEffectConfig;
            if (newConfig.XmasEffectConfig != null)
                oldConfig.XmasEffectConfig = newConfig.XmasEffectConfig;
            await System.IO.File.WriteAllTextAsync(path, JsonConvert.SerializeObject(oldConfig));
            return newConfig;
        }

        #endregion
    }
}
