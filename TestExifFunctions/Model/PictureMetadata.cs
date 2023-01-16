﻿using Newtonsoft.Json;

namespace TestExifFunctions.Model
{
    public class PictureMetadata
    {
        private const string GoogleMapsMask = "https://www.google.ch/maps/@{0},{1}";

        [JsonProperty("id")] 
        public string Id => Name;

        public string PartitionKey { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public string Artist { get; set; }

        public double Latitude { get; set; }

        public double Longitude { get; set; }

        public string TakenDateTime { get; set; }

        public string CameraMake { get; set; }

        public string CameraModel { get; set; }
    }
}