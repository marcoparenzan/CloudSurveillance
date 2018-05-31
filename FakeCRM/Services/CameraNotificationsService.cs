using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FakeCRM.Services
{
    public class CameraNotificationsService : IHostedService
    {
        private IConfiguration _configuration;
        private SubscriptionClient _subscriptionClient;

        public CameraNotificationsService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        Task IHostedService.StartAsync(CancellationToken cancellationToken)
        {
            _subscriptionClient = new SubscriptionClient(
                _configuration["ServiceBusConnectionString"],
                "camerastopic", "fakecrm"
                );
            _subscriptionClient.RegisterMessageHandler(
                async (m, c) => {

                    var bytes = m.Body;
                    var json = Encoding.UTF8.GetString(bytes);
                    var content = JsonConvert.DeserializeObject<JObject>(json);

                    var id = Guid.Parse(content.Value<string>("Id"));
                    var cameraName = content.Value<string>("CameraName");
                    var imageUri = content.Value<string>("ImageUri");

                    var documentClient =
                        new DocumentClient(
                            new Uri(_configuration["CosmosDbUri"]),
                            _configuration["CosmosDbKey"]
                        );

                    var db = documentClient
                        .CreateDatabaseQuery()
                        .ToList()
                        .SingleOrDefault(xx => xx.Id == "fakecrm");

                    var coll = documentClient
                        .CreateDocumentCollectionQuery(db.SelfLink)
                        .ToList()
                        .SingleOrDefault(xx => xx.Id == "docs");

                    var result = await documentClient.CreateDocumentAsync(
                        new Uri(coll.SelfLink, UriKind.Relative),
                        new {
                            id, imageUri, cameraName
                        }
                    );

                    await _subscriptionClient.CompleteAsync(m.SystemProperties.LockToken);
                },
                async (e) => { 

                });
            return Task.CompletedTask;
        }

        Task IHostedService.StopAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
