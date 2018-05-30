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

                    var id = content.Value<Guid>("id");
                    var cameraName = content.Value<string>("cameraName");
                    var imageUri = content.Value<string>("imageUri");

                    await _subscriptionClient.CompleteAsync(m.SystemProperties.LockToken);
                },
                new Func<ExceptionReceivedEventArgs, Task>(async (e) => {
                }));
            return Task.CompletedTask;
        }

        Task IHostedService.StopAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
