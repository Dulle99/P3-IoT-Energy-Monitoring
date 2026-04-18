using SensorSimulator.DTOs;
using SensorSimulator.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace SensorSimulator.Utilities
{
    public static class HttpUtility
    {
        public static async Task SendingReadingsToEdgeXAsync(string baseUrl, HttpClient httpClient, PowerConsumptionReading reading)
        {
            await PostPlainTextAsync(httpClient, $"{baseUrl}/globalActivePower", reading.GlobalActivePower.ToString(CultureInfo.InvariantCulture));
            await PostPlainTextAsync(httpClient, $"{baseUrl}/voltage", reading.Voltage.ToString(CultureInfo.InvariantCulture));
            await PostPlainTextAsync(httpClient, $"{baseUrl}/globalIntensity", reading.GlobalIntensity.ToString(CultureInfo.InvariantCulture));

        }

        private static async Task PostPlainTextAsync(HttpClient client, string url, string value)
        {
            using var content = new StringContent(value);

            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/plain");

            var response = await client.PostAsync(url, content);

            if(!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new Exception($"POST {url} - Failed to post data. Status code: {response.StatusCode}, Response body: {errorBody}");
            }
        }
    }
}
