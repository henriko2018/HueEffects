using System;
using System.Collections.Generic;
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
        private readonly XmasEffect _configuration;
        private readonly ILogger<XmasHandler> _logger;

        public XmasHandler(XmasEffect configuration, ILoggerFactory loggerFactory, ILocalHueClient hueClient)  : base(hueClient)
        {
            _configuration = configuration;
            _logger = loggerFactory.CreateLogger<XmasHandler>();
        }

        protected override async Task DoWork()
        {
            try
            {
                var colorLights = await GetLightsWithCapability(_configuration.LightGroup,
                    capabilities => capabilities.Control.ColorGamut != null);
                var ambianceLights = await GetLightsWithCapability(_configuration.LightGroup,
                    capabilities =>
                        capabilities.Control.ColorGamut == null && capabilities.Control.ColorTemperature != null);

                await SwitchOn(colorLights);
                await SwitchOn(ambianceLights);
                await SetColorRed(colorLights); // TODO: Add cancellation token?

                // Documentation says "sat:25 always gives the most saturated colors and reducing it to sat:200 makes them less intense and more white"
                // but it is the other way around.
                const float maxSat = 200;
                const float minSat = 25;
                const float maxCt = 454;
                const float minCt = 153;
                const float timeInterval = 1; // seconds

                // Calculate time between each step. We want to go from min to max in half the cycle length.
                //var msDelta = 1000 * _configuration.CycleLength / 2 / (minSat - maxSat);

                // Calculate step if we update once per time-interval.
                var noSteps = _configuration.CycleLength / timeInterval / 2; // Number of steps up or down
                var satStep = (maxSat - minSat) / noSteps;
                var ctStep = (maxCt - minCt) / noSteps;

                while (true)
                {
                    var sat = minSat;
                    var ct = minCt;
                    var intSat = 0; // This is the one we'll send to Hue later.
                    var intCt = 0;

                    void UpdateAndWait()
                    {
                        var startTime = DateTime.Now;

                        // Send new value only if it has changed
                        if ((int) sat != intSat)
                        {
                            intSat = (int) sat;
#pragma warning disable 4014
                            UpdateSaturation(intSat, colorLights);
#pragma warning restore 4014
                        }

                        if ((int) ct != intCt)
                        {
                            intCt = (int) ct;
#pragma warning disable 4014
                            UpdateColorTemp(intCt, ambianceLights);
#pragma warning restore 4014
                        }

                        var timeToWait = startTime.AddSeconds(timeInterval) - DateTime.Now;
                        Thread.Sleep(timeToWait); // TODO: Cancel while sleeping?
                    }


                    // Start at minSat, i.e. white, and go towards maxSat, i.e. red (higher value).
                    // For ambiance lights, we go from minCt, i.e. cold, to maxCt, i.e. warm (higher value).
                    for (var i = 0; i < noSteps; i++)
                    {
                        if (StopFlag) return;
                        UpdateAndWait();
                        sat += satStep;
                        ct += ctStep;
                    }

                    // Go the opposite direction, i.e. from red (maxSat) to white (minSat) (lower value).
                    for (var i = 0; i < noSteps; i++)
                    {
                        if (StopFlag) return;
                        UpdateAndWait();
                        sat -= satStep;
                        ct -= ctStep;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception caught in " + nameof(DoWork));
            }
        }

        private async Task SwitchOn(IReadOnlyCollection<string> lightIds)
        {
            _logger.LogDebug("Switching on light(s) {lights}...", string.Join(',', lightIds));
            var command = new LightCommand { On = true };
            await HueClient.SendCommandAsync(command, lightIds);
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

        private async Task UpdateColorTemp(int ct, IReadOnlyCollection<string> lightIds)
        {
            _logger.LogDebug("Setting color temp to {ct} for lights {lights}...", ct, string.Join(',', lightIds));
            var command = new LightCommand { ColorTemperature = ct};
            await HueClient.SendCommandAsync(command, lightIds);
        }

        private async Task<List<string>> GetLightsWithCapability(string groupId, Func<LightCapabilities, bool> condition)
        {
            var lights = new List<string>();
            var group = await HueClient.GetGroupAsync(_configuration.LightGroup);
            foreach (var lightId in group.Lights)
            {
                var light = await HueClient.GetLightAsync(lightId);
                if (condition(light.Capabilities))
                    lights.Add(light.Id);
            }

            return lights;
        }
    }
}
