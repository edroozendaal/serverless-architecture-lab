﻿using Microsoft.Extensions.Logging;

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

using TollBooth.Models;

namespace TollBooth
{
    public class SendToEventGrid
    {
        public SendToEventGrid(ILogger log, HttpClient client)
        {
            _log = log;
            _client = client;
        }

        public async Task SendLicensePlateData(LicensePlateData data)
        {
            // Will send to one of two routes, depending on success. Event listeners will filter and
            // act on events they need to process (save to database, move to manual checkup queue, etc.)
            if (data.LicensePlateFound)
            {
                await Send("savePlateData", "TollBooth/CustomerService", data);
            }
            else
            {
                await Send("queuePlateForManualCheckup", "TollBooth/CustomerService", data);
            }
        }

        private readonly HttpClient _client;
        private readonly ILogger _log;

        private async Task Send(string eventType, string subject, LicensePlateData data)
        {
            // Get the API URL and the API key from settings.
            var uri = Environment.GetEnvironmentVariable("eventGridTopicEndpoint");
            var key = Environment.GetEnvironmentVariable("eventGridTopicKey");

            _log.LogInformation($"Sending license plate data to the {eventType} Event Grid type");

            var events = new List<Event<LicensePlateData>>
            {
                new Event<LicensePlateData>()
                {
                    Data = data,
                    EventTime = DateTime.UtcNow,
                    EventType = eventType,
                    Id = Guid.NewGuid().ToString(),
                    Subject = subject
                }
            };

            _client.DefaultRequestHeaders.Clear();
            _client.DefaultRequestHeaders.Add("aeg-sas-key", key);
            await _client.PostAsJsonAsync(uri, events);

            _log.LogInformation($"Sent the following to the Event Grid topic: {events[0]}");
        }
    }
}