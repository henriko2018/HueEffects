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
            var bridges = JsonSerializer.Deserialize<IEnumerable<Bridge>>(response, new JsonSerializerOptions {PropertyNameCaseInsensitive = true}).ToList();
            if (!bridges.Any())
                throw new ApplicationException("No Hue bridge discovered");
            // Assume the first bridge is the one we want.
            return bridges.First().InternalIpAddress;
        }

        internal async Task<LightGroup> GetLightGroup(string lightGroupId)
        {
            var response = await _httpClient.GetStringAsync(await GetApiUri($"groups/{lightGroupId}"));
            var group = JsonSerializer.Deserialize<LightGroup>(response, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            group.Id = lightGroupId;
            return group;
        }

        internal async Task<Light> GetLight(string lightId)
        {
            var response = await _httpClient.GetStringAsync(await GetApiUri("lights/" + lightId));
            var result = JsonSerializer.Deserialize<Light>(response, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            result.Id = lightId;
            return result;
        }

        internal async Task SetLightState(string lightId, object state)
        {
            var content = JsonSerializer.Serialize(state,
                new JsonSerializerOptions {PropertyNamingPolicy = new LowerCaseJsonNamingPolicy()});
            Console.Out.WriteLine($"Setting light {lightId} state to {content}...");
            var response = await _httpClient.PutAsync(await GetApiUri($"lights/{lightId}/state"),
                new StringContent(content, Encoding.UTF8, "application/json"));
            if (!response.IsSuccessStatusCode)
                throw new ApplicationException($"Failed to set light state. Code: {response.StatusCode}, content:{await response.Content.ReadAsStringAsync()}");
        }
    }

    internal class LowerCaseJsonNamingPolicy : JsonNamingPolicy
    {
        public override string ConvertName(string name)
        {
            return name.ToLower();
        }
    }
}
