using System.Diagnostics;
using System.Threading.Tasks;
using HueEffects.Web.EffectHandlers;
using Microsoft.AspNetCore.Mvc;
using HueEffects.Web.Models;
using Microsoft.Extensions.Logging;
using Q42.HueApi.Interfaces;
using HueEffects.Web.Services;
using Microsoft.Extensions.Options;

namespace HueEffects.Web.Controllers
{
	public class HomeController : Controller
    {
        private readonly ILocalHueClient _hueClient;
        private readonly ILoggerFactory _loggerFactory;
		private readonly BackgroundService _backgroundService;
		private readonly StorageService _storageService;
        private readonly Options _options;

		public HomeController(ILocalHueClient hueClient, ILoggerFactory loggerFactory, Microsoft.Extensions.Hosting.IHostedService backgroundService, StorageService storageService, IOptionsMonitor<Options> optionsAccessor)
        {
            _hueClient = hueClient;
            _loggerFactory = loggerFactory;
			_backgroundService = (BackgroundService)backgroundService;
			_storageService = storageService;
            _options = optionsAccessor.CurrentValue;
        }

        #region Http action methods

        public async Task<IActionResult> Index()
        {
            var model = new EffectsConfig
            {
                LightGroups = await _hueClient.GetGroupsAsync(),
                SunPhases = TimeConfig.GetSunPhases(_options.Location),
                XmasEffectConfig = await _storageService.LoadConfig<XmasEffectConfig>(),
                WarmupEffectConfig = await _storageService.LoadConfig<WarmupEffectConfig>()
            };
            model.XmasEffectConfig.Active = _backgroundService.ActiveHandler != null && _backgroundService.ActiveHandler.GetType() == typeof(XmasHandler);
            model.WarmupEffectConfig.Active = _backgroundService.ActiveHandler != null && _backgroundService.ActiveHandler.GetType() == typeof(WarmupHandler);
            model.WarmupEffectConfig.TurnOnAt.Location = _options.Location; // TODO: It would be nice to have this injected into TimeConfig
            model.WarmupEffectConfig.TurnOffAt.Location = _options.Location;

			return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UseXmasEffect(EffectsConfig config)
        {
			if (ModelState.IsValid)
			{
				await _backgroundService.StartEffect(config.XmasEffectConfig, new XmasHandler(config.XmasEffectConfig, _loggerFactory, _hueClient));
				return RedirectToAction("Index");
			}
            return View("Index", config);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UseWarmupEffect(EffectsConfig config)
        {
			if (ModelState.IsValid)
			{
                config.WarmupEffectConfig.TurnOnAt.Location = _options.Location; // TODO: It would be nice to have this injected into TimeConfig
                config.WarmupEffectConfig.TurnOffAt.Location = _options.Location;
                await _backgroundService.StartEffect(config.WarmupEffectConfig, new WarmupHandler(config.WarmupEffectConfig, _loggerFactory, _hueClient));
				return RedirectToAction("Index");
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

        #endregion
    }
}
