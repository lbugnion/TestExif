using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using SixLabors.ImageSharp;
using TestExifFunctions.Model;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage;
using System;

namespace TestExifFunctions
{
    public static class AddMetadata
    {
        private const string ArtistName = "Laurent Bugnion";
        private const string UniquePartitionKey = "partition";

        private const string UploadsFolderName = "uploads";

        [FunctionName(nameof(AddMetadata))]
        public static async Task<IActionResult> Run(
            [HttpTrigger(
                AuthorizationLevel.Anonymous,
                "get",
                Route = "add-metadata/{blobName}")]
            HttpRequest req,
            string blobName,
            [Blob($"{UploadsFolderName}/{{blobName}}", FileAccess.Read, Connection = "AzureWebJobsStorage")]
            Stream inputBlob,
            [Blob("outputs/{blobName}", FileAccess.Write, Connection = "AzureWebJobsStorage")]
            Stream outputBlob,
            [CosmosDB(
                databaseName: "PicturesDb",
                containerName: "Pictures",
                Connection = "CosmosDBConnection",
                CreateIfNotExists = true,
                PartitionKey = "partition")]
            IAsyncCollector<PictureMetadata> collector,
            ILogger log)
        {
            if (inputBlob == null)
            {
                var message = $"Not found: {blobName}";
                log.LogError(message);
                return new BadRequestObjectResult(message);
            }

            try
            {
                var tuple = await Image.LoadWithFormatAsync(inputBlob);
                var image = tuple.Image;
                var format = tuple.Format;
                var values = image.Metadata.ExifProfile.Values;

                var existingMedata = new PictureMetadata
                {
                    Name = blobName,
                    Artist = ArtistName,
                    PartitionKey = UniquePartitionKey
                };

                foreach (var value in values)
                {
                    if (value.IsArray)
                    {
                        if (value.Tag == ExifTag.GPSLatitude
                            || value.Tag == ExifTag.GPSLongitude)
                        {
                            var coordinateArray = (Rational[])value.GetValue();
                            var coordinate =
                                coordinateArray[0].Numerator
                                + (coordinateArray[1].Numerator / 60D)
                                + ((coordinateArray[2].Numerator / coordinateArray[2].Denominator) / 3600D);

                            log.LogInformation($"{value.Tag}: {coordinate}");

                            if (value.Tag == ExifTag.GPSLatitude)
                            {
                                existingMedata.Latitude = coordinate;
                            }
                            else
                            {
                                existingMedata.Longitude = coordinate;
                            }
                        }
                    }
                    else
                    {
                        var valueString = value.GetValue().ToString();

                        log.LogInformation($"{value.Tag}: {valueString}");

                        if (valueString.Contains("LogoLicious"))
                        {
                            value.TrySetValue(string.Empty);
                        }

                        if (value.Tag == ExifTag.ImageDescription)
                        {
                            existingMedata.Description = valueString;
                        }
                        else if (value.Tag == ExifTag.ImageDescription)
                        {
                            existingMedata.Description = valueString;
                        }
                        else if (value.Tag == ExifTag.Make)
                        {
                            existingMedata.CameraMake = valueString;
                        }
                        else if (value.Tag == ExifTag.Model)
                        {
                            existingMedata.CameraModel = valueString;
                        }
                        else if (value.Tag == ExifTag.DateTimeOriginal)
                        {
                            existingMedata.TakenDateTime = valueString;
                        }
                    }
                }

                // Call synchronous function to save in Azure CosmosDB

                await collector.AddAsync(existingMedata);

                log.LogInformation($"Saving {blobName} to outputs");
                await image.SaveAsync(outputBlob, format);
                return new OkObjectResult(blobName);
            }
            catch (Exception ex)
            {
                log.LogError(ex.Message);
                return new UnprocessableEntityObjectResult(ex.Message);
            }
        }
    }
}
