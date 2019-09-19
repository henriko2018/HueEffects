using System;
using System.Collections.Generic;
using System.Threading;
using System.Timers;
using System.Threading.Tasks;
using HueEffects.Web.Models;
using Microsoft.Extensions.Logging;
using Q42.HueApi;
using Q42.HueApi.Interfaces;
using Timer = System.Timers.Timer;

namespace HueEffects.Web.EffectHandlers
{
    public class WarmupHandler : EffectHandler
    {
        private readonly WarmupEffectConfig _config;
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger<WarmupHandler> _logger;
        private readonly List<Timer> _timers = new List<Timer>();
        private readonly Random _random = new Random();
        private List<string> _lightIds;

        public WarmupHandler(WarmupEffectConfig config, ILoggerFactory loggerFactory, ILocalHueClient hueClient) : base(hueClient, loggerFactory)
        {
            _config = config;
            _loggerFactory = loggerFactory;
            _logger = loggerFactory.CreateLogger<WarmupHandler>();
        }

        protected override async Task DoWork()
        {
            _lightIds = await GetLightsWithCapability(_config.LightGroup, capabilities => capabilities.Control.ColorTemperature != null);
            AddTurnOnTimer();
            AddTurnOffTimer();
        }

        public override void Stop()
        {
            base.Stop();
            foreach (var timer in _timers)
            {
                timer.Stop();
            }
            _timers.Clear();
        }

        private void AddTurnOnTimer()
        {
            var msUntilOn = _config.TurnOnAt.GetUntil(false);
            var timer = new Timer {AutoReset = false, Interval = msUntilOn };
            timer.Elapsed += OnTimer_Elapsed;
            timer.Start();
            _timers.Add(timer);
            _logger.LogDebug($"On timer will fire at {DateTime.Now.AddMilliseconds(timer.Interval)}.");
        }

        private void OnTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            // Turn on first
            var command = new LightCommand {On = true, ColorTemperature = _config.UseMinTemp};
            HueClient.SendCommandAsync(command, _lightIds);
            // Warm up
            // Calculate time between each step. We want to go from min to max during configured warm-up.
            var msDelta = (int) (_config.TurnOnAt.TransitionTime.TotalMilliseconds / (_config.UseMaxTemp - _config.UseMinTemp));
            for (var temp = _config.UseMinTemp; temp <= _config.UseMaxTemp; temp++)
            {
                if (StopFlag) return;
                Thread.Sleep(msDelta);
#pragma warning disable 4014
                UpdateColorTemp(temp, _lightIds);
#pragma warning restore 4014
            }
        }

        private void AddTurnOffTimer()
        {
            var msUntilEvent = _config.TurnOffAt.GetUntil(true);
            var timer = new Timer {AutoReset = false, Interval = msUntilEvent};
            timer.Elapsed += OffTimer_Elapsed;
            timer.Start();
            _timers.Add(timer);
            _logger.LogDebug($"Off timer will fire at {DateTime.Now.AddMilliseconds(timer.Interval)}.");
        }

        private void OffTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            // Cool down
            // Calculate time between each step. We want to go from max to min during configured cool-down.
            var msDelta = (int) (_config.TurnOffAt.TransitionTime.TotalMilliseconds / (_config.UseMaxTemp - _config.UseMinTemp));
            for (var temp = _config.UseMaxTemp; temp >= _config.UseMinTemp; temp--)
            {
                if (StopFlag) return;
                Thread.Sleep(msDelta);
#pragma warning disable 4014
                UpdateColorTemp(temp, _lightIds);
#pragma warning restore 4014
            }

            // Turn off last
            var command = new LightCommand { On = false };
            HueClient.SendCommandAsync(command, _lightIds);
        }
    }
}
