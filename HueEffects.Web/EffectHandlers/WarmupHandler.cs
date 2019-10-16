using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using System.Threading.Tasks;
using HueEffects.Web.Models;
using Microsoft.Extensions.Logging;
using Q42.HueApi.Interfaces;

namespace HueEffects.Web.EffectHandlers
{
    public class WarmupHandler : EffectHandler, IDisposable
    {
        private readonly WarmupEffectConfig _config;
        private readonly ILogger<WarmupHandler> _logger;
        private readonly List<Timer> _timers = new List<Timer>(); // Timers must be referenced so that they are not garbage collected.
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
                _lightIds = (await GetLights(_config.LightGroup))
                    .Where(light => light.Capabilities.Control.ColorTemperature != null)
                    .Select(light => light.Id)
                    .ToList();
				AddTurnOnTimer();
				AddTurnOffTimer();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Exception caught in " + nameof(DoWork));
			}
        }
        
        public void Dispose()
        {
            StopTimers();
        }

        private void StopTimers()
        {
            foreach (var timer in _timers)
            {
                timer.Stop();
                _logger.LogInformation("Timer stopped.");
            }
            _timers.Clear();
        }

        private void AddTurnOnTimer()
        {
            var msUntilOn = _config.TurnOnAt.GetUntil(false);
            _logger.LogInformation($"On timer will fire at {DateTime.Now.AddMilliseconds(msUntilOn)}.");
            var timer = new Timer {AutoReset = false, Interval = msUntilOn };
            timer.Elapsed += OnTimer_Elapsed;
            timer.Start();
            _timers.Add(timer);
        }

        private async void OnTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                _logger.LogInformation("On timer elapsed.");
                // Turn on first
    #pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                SwitchOn(_lightIds, _config.UseMinTemp);
    #pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

                // Warm up
                // Calculate time between each step. We want to go from min to max during configured warm-up.
                var msDelta = (int) (_config.TurnOnAt.TransitionTime.TotalMilliseconds / (_config.UseMaxTemp - _config.UseMinTemp));
                for (var temp = _config.UseMinTemp; temp <= _config.UseMaxTemp && !CancellationToken.IsCancellationRequested; temp++)
                {
                    await Task.Delay(msDelta, CancellationToken);
    #pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                    UpdateColorTemp(temp, _lightIds);
    #pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                }
            }
            catch (OperationCanceledException)
            {
                StopTimers();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Exception caught in {nameof(OnTimer_Elapsed)}");
            }
		}

        private void AddTurnOffTimer()
        {
            var msUntilEvent = _config.TurnOffAt.GetUntil(true);
            _logger.LogInformation($"Off timer will fire at {DateTime.Now.AddMilliseconds(msUntilEvent)}.");
            var timer = new Timer {AutoReset = false, Interval = msUntilEvent};
            timer.Elapsed += OffTimer_Elapsed;
            timer.Start();
            _timers.Add(timer);
        }

        private async void OffTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                _logger.LogInformation("Off timer elapsed.");
                // Cool down
                // Calculate time between each step. We want to go from max to min during configured cool-down.
                var msDelta = (int) (_config.TurnOffAt.TransitionTime.TotalMilliseconds / (_config.UseMaxTemp - _config.UseMinTemp));
                for (var temp = _config.UseMaxTemp; temp >= _config.UseMinTemp && !CancellationToken.IsCancellationRequested; temp--)
                {
                    await Task.Delay(msDelta, CancellationToken);
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
            catch (OperationCanceledException)
            {
                StopTimers();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Exception caught in {nameof(OffTimer_Elapsed)}");
            }
		}
    }
}
