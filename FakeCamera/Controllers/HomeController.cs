using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using FakeCamera.Models;
using System.Net.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.WindowsAzure.Storage;
using System.IO;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using System.Text;
using Microsoft.Azure.ServiceBus;

namespace FakeCamera.Controllers
{
    public class HomeController : Controller
    {
        private IConfiguration _configuration;

        public HomeController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult About()
        {
            ViewData["Message"] = "Your application description page.";

            return View();
        }

        public IActionResult Contact()
        {
            ViewData["Message"] = "Your contact page.";

            return View();
        }

        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [HttpGet]
        public IActionResult Camera()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Camera(List<IFormFile> camera)
        {
            var storageAccount = 
                CloudStorageAccount.Parse(
                    _configuration["StorageConnectionString"]);

            var blobClient = storageAccount.CreateCloudBlobClient();

            var camerasContainer = 
                blobClient.GetContainerReference("cameras");
            await camerasContainer.CreateIfNotExistsAsync();

            foreach (var cameraFile in camera)
            {
                var cameraName = _configuration["cameraName"];
                var id = Guid.NewGuid();
                var fileExtension = 
                    Path.GetExtension(cameraFile.FileName);
                var blobName = $"{cameraName}/{id}{fileExtension}";

                var blobRef = 
                    camerasContainer.GetBlockBlobReference(blobName);

                using (var stream = cameraFile.OpenReadStream())
                {
                    await blobRef.UploadFromStreamAsync(stream);
                    //var buffer = new byte[4096];
                    //while (true)
                    //{
                    //    var read = await stream.ReadAsync(buffer, 0, buffer.Length);
                    //    if (read == 0) break;
                    //    await blobRef.UploadFromByteArrayAsync(buffer, 0, read);
                    //    if (read < buffer.Length) break;
                    //}
                }

                var sas = blobRef.GetSharedAccessSignature(
                    new SharedAccessBlobPolicy()
                    {
                        Permissions = SharedAccessBlobPermissions.Read,
                        SharedAccessStartTime = DateTime.Now.AddMinutes(-5),
                        SharedAccessExpiryTime = DateTime.Now.AddMinutes(54)
                    });

                var blobUri = $"{blobRef.Uri.AbsoluteUri}{sas}";

                var notification = new
                {
                    CameraName = cameraName,
                    Id = id,
                    ImageUri = blobUri
                };

                var notificationJson = JsonConvert.SerializeObject(notification);
                var notificationBytes = Encoding.UTF8.GetBytes(notificationJson);

                //// SERVICEBUS QUEUE

                //var queueClient = new QueueClient(
                //    _configuration["ServiceBusQueueConnectionString"], "cameras");

                //var message = new Message(notificationBytes);

                //await queueClient.SendAsync(message);

                // SERVICE BUS TOPIC

                var topicClient = new TopicClient(
                    _configuration["ServiceBusQueueConnectionString"],
                    "camerastopic");

                var message = new Message(notificationBytes);

                await topicClient.SendAsync(message);

            }

            return View();
        }
    }
}
