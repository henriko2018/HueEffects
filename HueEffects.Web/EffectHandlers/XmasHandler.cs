using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HueEffects.Web.Models;
using Microsoft.Extensions.Logging;
using Q42.HueApi;
using Q42.HueApi.Interfaces;

namespace HueEffects.Web.EffectHandlers
{
    public class XmasHandler : EffectHandler
    {
        private readonly XmasEffectConfig _configuration;
        private readonly ILogger<XmasHandler> _logger;


        public XmasHandler(XmasEffectConfig configuration, ILoggerFactory loggerFactory, ILocalHueClient hueClient)  : base(hueClient, loggerFactory)
        {
            _configuration = configuration;
            _logger = loggerFactory.CreateLogger<XmasHandler>();
        }

        protected override async Task DoWork()
        {
            try
            {
                var lights = await GetLights(_configuration.LightGroup);
                var colorLights = lights.Where(light => light.Capabilities.Control.ColorGamut != null).ToList();
                var ambianceLights = lights.Where(light => light.Capabilities.Control.ColorTemperature != null).Except(colorLights).ToList();
                var colorLightIds = colorLights.Select(l => l.Id).ToList();
                var ambianceLightIds = ambianceLights.Select(l => l.Id).ToList();

                await SwitchOn(colorLightIds);
                await SwitchOn(ambianceLightIds);
                await SetColorRed(colorLightIds);

                // Documentation says "sat:25 always gives the most saturated colors and reducing it to sat:200 makes them less intense and more white"
                // but it is the other way around.
                const float maxSat = 200;
                const float minSat = 25;
                const float maxCt = 454;
                const float minCt = 153;
                const float timeInterval = 1; // seconds

                // Calculate step if we update once per time-interval.
                var noSteps = _configuration.CycleLength / timeInterval / 2; // Number of steps up or down
                var satStep = (maxSat - minSat) / noSteps;
                var ctStep = (maxCt - minCt) / noSteps;

                try
                {
                    while (!CancellationToken.IsCancellationRequested)
                    {
                        var sat = minSat;
                        var ct = minCt;

                        // Start at minSat, i.e. white, and go towards maxSat, i.e. red (higher value).
                        // For ambiance lights, we go from minCt, i.e. cold, to maxCt, i.e. warm (higher value).
                        for (var i = 0; i < noSteps && !CancellationToken.IsCancellationRequested; i++)
                        {
                            await UpdateAndWait(colorLightIds, ambianceLightIds, sat, ct, timeInterval,
                                CancellationToken);
                            sat += satStep;
                            ct += ctStep;
                        }

                        // Go the opposite direction, i.e. from red (maxSat) to white (minSat) (lower value).
                        for (var i = 0; i < noSteps && !CancellationToken.IsCancellationRequested; i++)
                        {
                            await UpdateAndWait(colorLightIds, ambianceLightIds, sat, ct, timeInterval,
                                CancellationToken);
                            sat -= satStep;
                            ct -= ctStep;
                        }
                    }
                    // Here's where we end up if the check CancellationToken.IsCancellationRequested returns true (less likely).
                    await RestoreOriginalValues(colorLights, ambianceLights);
                }
                catch (TaskCanceledException)
                {
                    _logger.LogInformation("Canceled. Restoring original values.");
                    // Here's where we end up if cancellation is requested during Task.Delay() in UpdateAndWait() (more likely).
                    await RestoreOriginalValues(colorLights, ambianceLights);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception caught in " + nameof(DoWork));
            }
        }

        private async Task RestoreOriginalValues(IEnumerable<Light> colorLights, IEnumerable<Light> ambianceLights)
        {
            var colorTasks = colorLights.Select(light => HueClient.SendCommandAsync(new LightCommand
                    {On = light.State.On, ColorCoordinates = light.State.ColorCoordinates, Hue = light.State.Hue},
                new[] {light.Id}));
            var ambianceTasks = ambianceLights.Select(light => HueClient.SendCommandAsync(new LightCommand
                    {On = light.State.On, ColorTemperature = light.State.ColorTemperature},
                new[] {light.Id}));
            var tasks = Enumerable.Union(colorTasks, ambianceTasks).ToList();
            await Task.WhenAll(tasks);
        }

        private async Task UpdateAndWait(IReadOnlyCollection<string> colorLights, IReadOnlyCollection<string> ambianceLights, float sat, float ct, float timeInterval, CancellationToken cancellationToken)
		{
			var startTime = DateTime.Now;

#pragma warning disable 4014
			UpdateSaturation((int) sat, colorLights);
			UpdateColorTemp((int) ct, ambianceLights);
#pragma warning restore 4014

			var timeToWait = startTime.AddSeconds(timeInterval) - DateTime.Now;
            await Task.Delay(timeToWait, cancellationToken);
        }

		private async Task SetColorRed(IReadOnlyCollection<string> lightIds)
        {
            _logger.LogDebug("Setting color to red for light(s) {lights}...", string.Join(',', lightIds));
            // "xy":[0.675,0.322]is red.
            var command = new LightCommand { ColorCoordinates = new[] { 0.675, 0.322 } };
            await HueClient.SendCommandAsync(command, lightIds);
        }

        private async Task UpdateSaturation(int sat, IReadOnlyCollection<string> lightIds)
        {
            _logger.LogDebug("Setting saturation to {sat} for light(s) {lights}...", sat, string.Join(',', lightIds));
            var command = new LightCommand {Saturation = sat};
            await HueClient.SendCommandAsync(command, lightIds);
        }
    }
}
