using System;
using System.Collections.Generic;
using System.Threading;
using System.Timers;
using System.Threading.Tasks;
using HueEffects.Web.Models;
using Microsoft.Extensions.Logging;
using Q42.HueApi.Interfaces;
using Timer = System.Timers.Timer;

namespace HueEffects.Web.EffectHandlers
{
    public class WarmupHandler : EffectHandler
    {
        private readonly WarmupEffectConfig _config;
        private readonly ILogger<WarmupHandler> _logger;
        private readonly List<Timer> _timers = new List<Timer>();
        private List<string> _lightIds;

        public WarmupHandler(WarmupEffectConfig config, ILoggerFactory loggerFactory, ILocalHueClient hueClient) : base(hueClient, loggerFactory)
        {
            _config = config;
            _logger = loggerFactory.CreateLogger<WarmupHandler>();
        }

        protected override async Task DoWork()
        {
			try
			{
				_lightIds = await GetLightsWithCapability(_config.LightGroup, capabilities => capabilities.Control.ColorTemperature != null);
				AddTurnOnTimer();
				AddTurnOffTimer();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Exception caught in " + nameof(DoWork));
			}
        }

        private void StopTimers()
        {
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

        private async void OnTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (_cancellationToken.IsCancellationRequested)
            {
                StopTimers();
                return;
            }

            // Turn on first
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
			SwitchOn(_lightIds, _config.UseMinTemp);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

			// Warm up
			// Calculate time between each step. We want to go from min to max during configured warm-up.
			var msDelta = (int) (_config.TurnOnAt.TransitionTime.TotalMilliseconds / (_config.UseMaxTemp - _config.UseMinTemp));
            for (var temp = _config.UseMinTemp; temp <= _config.UseMaxTemp && !_cancellationToken.IsCancellationRequested; temp++)
            {
                await Task.Delay(msDelta, _cancellationToken);
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
				UpdateColorTemp(temp, _lightIds);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
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

        private async void OffTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (_cancellationToken.IsCancellationRequested)
            {
                StopTimers();
                return;
            }

            // Cool down
            // Calculate time between each step. We want to go from max to min during configured cool-down.
            var msDelta = (int) (_config.TurnOffAt.TransitionTime.TotalMilliseconds / (_config.UseMaxTemp - _config.UseMinTemp));
            for (var temp = _config.UseMaxTemp; temp >= _config.UseMinTemp && !_cancellationToken.IsCancellationRequested; temp--)
            {
                await Task.Delay(msDelta, _cancellationToken);
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
				UpdateColorTemp(temp, _lightIds);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
			}

			// Turn off
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
			SwitchOff(_lightIds);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

			// Lastly, schedule new timers
			AddTurnOnTimer();
			AddTurnOffTimer();
		}
	}
}
