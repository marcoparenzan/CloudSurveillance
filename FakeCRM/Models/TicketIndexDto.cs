using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FakeCRM.Models
{
    public class TicketIndexDto
    {
        [JsonProperty("id")]
        public Guid Id { get; set; }
        [JsonProperty("cameraName")]
        public string CameraName { get; set; }
        [JsonProperty("imageUri")]
        public string ImageUri { get; set; }
    }
}
