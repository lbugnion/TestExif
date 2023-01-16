using Newtonsoft.Json;

namespace TestExifFunctions.Model
{
    public class PictureMetadata
    {
        private const string GoogleMapsMask = "https://www.google.com/maps/search/?api=1&query={0},{1}";

        public string Artist { get; set; }

        public string CameraMake { get; set; }

        public string CameraModel { get; set; }

        public string Description { get; set; }

        public string GoogleLocationUrl => string.Format(GoogleMapsMask, Latitude, Longitude);

        [JsonProperty("id")]
        public string Id => Name;

        public double Latitude { get; set; }

        public double Longitude { get; set; }

        public string Name { get; set; }

        public string PartitionKey { get; set; }

        public string TakenDateTime { get; set; }
    }
}