// Default URL for triggering event grid function in the local environment.
// http://localhost:7071/runtime/webhooks/EventGrid?functionName={functionname}
using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.EventGrid.Models;
using Microsoft.Azure.WebJobs.Extensions.EventGrid;
using Microsoft.Extensions.Logging;
using Azure.DigitalTwins.Core;
using Azure.Identity;
using System.Net.Http;
using Azure.Core.Pipeline;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Azure;
using System.Threading.Tasks;

namespace IoTHubtoTwins
{
    public static class IoTHubtoTwins
    {
        private static readonly string adtInstanceUrl = Environment.GetEnvironmentVariable("ADT_SERVICE_URL");
        private static readonly HttpClient singletonHttpClientInstance = new HttpClient();

        [FunctionName("IOTHubtoTwins")]
        public async static Task Run([EventGridTrigger] EventGridEvent eventGridEvent, ILogger log)
        {
            if (adtInstanceUrl == null) log.LogError("Application setting \"ADT_SERVICE_URL\" not set");
            try
            {
                var cred = new ManagedIdentityCredential("https://digitaltwins.azure.net");

                var client = new DigitalTwinsClient(
                new Uri(adtInstanceUrl),
                cred,
                new DigitalTwinsClientOptions
                {
                    Transport = new HttpClientTransport(singletonHttpClientInstance)
                });

                log.LogInformation($"ADT service client connection created.");

                if (eventGridEvent != null && eventGridEvent.Data != null)
                {
                    log.LogInformation(eventGridEvent.Data.ToString());

                    // convert the message into a json object
                    JObject deviceMessage = (JObject)JsonConvert.DeserializeObject(eventGridEvent.Data.ToString());

                    // get our device id, temp and humidity from the object
                    string deviceId = (string)deviceMessage["systemProperties"]["iothub-connection-device-id"];
                    var temperature = deviceMessage["body"]["Temperature"];
                    var humidity = deviceMessage["body"]["Humidity"];

                    //log the temperature and humidity
                    log.LogInformation($"Device:{deviceId} Temperature is:{temperature} and Humidity is:{humidity}");

                    // Update twin with temperature and humidity fro our raspberry pi>
                    var updateTwinData = new JsonPatchDocument();
                    updateTwinData.AppendReplace("/Temperature", temperature.Value<double>());
                    updateTwinData.AppendReplace("/Humidity", humidity.Value<double>());
                    await client.UpdateDigitalTwinAsync(deviceId, updateTwinData);
                }
            }

            catch (Exception ex)
            {
                log.LogError($"Error in ingest function: {ex.Message}");
            }

        }
    }
}
