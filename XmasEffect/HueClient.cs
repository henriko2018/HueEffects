using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace XmasEffect
{
    public class HueClient
    {
        private readonly string _user;
        private readonly HttpClient _httpClient;
        private string _localIp;

        internal HueClient(string user)
        {
            _user = user;
            _httpClient = new HttpClient();
        }

        private async Task<string> GetApiUri(string resource)
        {
            if (_localIp == null)
                _localIp = await DiscoverLocalIp();
            return $"http://{_localIp}/api/{_user}/{resource}";
        }
        
        private async Task<string> DiscoverLocalIp()
        {
            var response = await _httpClient.GetStringAsync("https://discovery.meethue.com");
            var bridges = JsonSerializer.Deserialize<IEnumerable<Bridge>>(response).ToList();
            if (!bridges.Any())
                throw new ApplicationException("No Hue bridge discovered");
            // Assume the first bridge is the one we want.
            return bridges.First().InternalIpAddress;
        }

        internal async Task<IEnumerable<LightGroup>> GetLightGroups()
        {
            var response = await _httpClient.GetStringAsync(await GetApiUri("groups"));
            // Surprisingly, we don't get an array of groups but an object with properties called "1", "2" and so on.
            // Therefore, we cannot use JsonSerializer.Deserialize. 
            var jsonDoc = JsonDocument.Parse(response);
            var lightGroups = jsonDoc.RootElement.EnumerateObject().Select(prop => Map(prop.Name, prop.Value));
            return lightGroups;
        }

        internal async Task<LightGroup> GetLightGroup(string lightGroupId)
        {
            var response = await _httpClient.GetStringAsync(await GetApiUri($"groups/{lightGroupId}"));
            var jsonDoc = JsonDocument.Parse(response);
            return Map(lightGroupId, jsonDoc.RootElement);
        }

        internal async Task<Light> GetLight(string lightId)
        {
            var response = await _httpClient.GetStringAsync(await GetApiUri("lights/" + lightId));
            var result = JsonSerializer.Deserialize<Light>(response);
            result.Id = lightId;
            return result;
        }

        internal async Task SetLightState(string lightId, State state)
        {
            var response = await _httpClient.PutAsync(await GetApiUri($"lights/{lightId}/state"),
                new StringContent(JsonSerializer.Serialize(state), Encoding.UTF8, "application/json"));
            var responseBody = await response.Content.ReadAsStringAsync();
        }

        private LightGroup Map(string groupId, JsonElement groupValue)
        {
            var name = groupValue.GetProperty("name").GetString();
            var lightArray = groupValue.GetProperty("lights").EnumerateArray();
            var lightIds = lightArray.Select(l => l.GetString());
            return new LightGroup { Id = groupId, Name = name, LightIds = lightIds };
        }
    }
}
