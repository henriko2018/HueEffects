﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace XmasEffect
{
    internal class XmasEffect
    {
        public bool Continue { get; set; } = true;
        private readonly HueClient _hueClient;
        private readonly Options _options;

        public XmasEffect(Options options)
        {
            _options = options;
            _hueClient = new HueClient(options.User);    
        }

        internal async Task Start()
        {
            // Always use the special group "0", which contains all lights. This can of course be changed.
            var lightGroup = await _hueClient.GetLightGroup("0");
            var capableLights = await GetCapableLights(lightGroup);
            await InitLights(capableLights);
            await RunEffect(capableLights);
        }

        private async Task<(List<Light> colorLights, List<Light> ambianceLights)> GetCapableLights(LightGroup lightGroup)
        {
            var lights = new List<Light>();
            foreach (var lightId in lightGroup.Lights)
            {
                var light = await _hueClient.GetLight(lightId);
                light.Id = lightId;
                lights.Add(light);
            }
            var colorLights = lights.Where(light => light.Capabilities.Control.ColorGamut != null).ToList();
            var ambianceLights = lights.Where(light => light.Capabilities.Control.Ct != null).Except(colorLights).ToList();
            Console.Out.WriteLine($"Color light(s): {string.Join(',', colorLights.Select(l => l.Id))}");
            Console.Out.WriteLine($"Ambiance light(s): {string.Join(',', ambianceLights.Select(l => l.Id))}");
            return (colorLights, ambianceLights);
        }

        private async Task InitLights((List<Light> colorLights, List<Light> ambianceLights) capableLights)
        {
            Console.Out.WriteLine("Turning on light(s)...");
            foreach (var colorLight in capableLights.colorLights)
                await _hueClient.SetLightState(colorLight.Id, new State {On = true, Xy = new[] { 0.675f, 0.322f } }); // Red
            foreach (var ambianceLight in capableLights.ambianceLights)
                await _hueClient.SetLightState(ambianceLight.Id, new State {On = true});
        }

        private async Task RunEffect((List<Light> colorLights, List<Light> ambianceLights) capableLights)
        {
            Console.Out.WriteLine("Running effect...");

            // Documentation says "sat:25 always gives the most saturated colors and reducing it to sat:200 makes them less intense and more white"
            // but it is the other way around.
            const float maxSat = 200;
            const float minSat = 25;
            const float maxCt = 454;
            const float minCt = 153;
            const float timeInterval = 1; // seconds

            // Calculate step if we update once per time-interval.
            var noSteps = _options.CycleLength / timeInterval / 2; // Number of steps up or down
            var satStep = (maxSat - minSat) / noSteps;
            var ctStep = (maxCt - minCt) / noSteps;

            while (Continue)
            {
                var sat = minSat;
                var ct = minCt;

                // Start at minSat, i.e. white, and go towards maxSat, i.e. red (higher value).
                // For ambiance lights, we go from minCt, i.e. cold, to maxCt, i.e. warm (higher value).
                for (var i = 0; i < noSteps && Continue; i++)
                {
                    await UpdateAndWait(capableLights, sat, ct, timeInterval);
                    sat += satStep;
                    ct += ctStep;
                }

                // Go the opposite direction, i.e. from red (maxSat) to white (minSat) (lower value).
                for (var i = 0; i < noSteps && Continue; i++)
                {
                    await UpdateAndWait(capableLights, sat, ct, timeInterval);
                    sat -= satStep;
                    ct -= ctStep;
                }
            }
            
            Console.Out.WriteLine("Canceled. Restoring original values.");
            await RestoreOriginalValues(capableLights);
        }

        private async Task UpdateAndWait((List<Light> colorLights, List<Light> ambianceLights) capableLights, float sat, float ct, float timeInterval)
        {
            var startTime = DateTime.Now;

            // We don't await _hueClient below, since we're not interested in the result of the operation.
            // If it fails, it will probably work next time.
#pragma warning disable 4014
            foreach (var light in capableLights.colorLights)
                _hueClient.SetLightState(light.Id, new {sat = (int)sat});
            foreach (var light in capableLights.ambianceLights)
                _hueClient.SetLightState(light.Id, new {ct = (int)ct});
#pragma warning restore 4014

            var timeToWait = startTime.AddSeconds(timeInterval) - DateTime.Now;
            await Task.Delay(timeToWait);
        }

        private async Task RestoreOriginalValues((List<Light> colorLights, List<Light> ambianceLights) capableLights)
        {
            foreach (var light in capableLights.colorLights)
                await _hueClient.SetLightState(light.Id, new {light.State.On, light.State.Xy});
            foreach (var light in capableLights.ambianceLights)
                await _hueClient.SetLightState(light.Id, new {light.State.On, light.State.Ct});
        }
    }
}
