using System;
using System.Threading.Tasks;
using HueEffects.Web.Models;
using Microsoft.Extensions.Logging;
using Q42.HueApi.Interfaces;

namespace HueEffects.Web.EffectHandlers
{
    public class WarmupHandler : EffectHandler
    {
        private readonly WarmupEffect _config;
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger<WarmupHandler> _logger;

        public WarmupHandler(WarmupEffect config, ILoggerFactory loggerFactory, ILocalHueClient hueClient) : base(hueClient)
        {
            _config = config;
            _loggerFactory = loggerFactory;
            _logger = loggerFactory.CreateLogger<WarmupHandler>();
        }

        protected override Task DoWork()
        {
            return Task.CompletedTask;
        }
    }
}
