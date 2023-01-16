using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using System;
using System.IO;
using System.Threading.Tasks;
using TestExifFunctions.Model;

namespace TestExifFunctions
{
    public static class AddMetadata
    {
        [FunctionName(nameof(AddMetadata))]
        public static async Task<IActionResult> Run(
            [HttpTrigger(
                AuthorizationLevel.Anonymous,
                "get",
                Route = "add-metadata/{blobName}")]
            HttpRequest req,
            string blobName,
            [Blob(
                $"{Constants.UploadsFolderName}/{{blobName}}",
                FileAccess.Read,
                Connection = Constants.AzureWebJobsStorageVariableName)]
            Stream inputBlob,
            [Blob(
                "outputs/{blobName}",
                FileAccess.Write,
                Connection = Constants.AzureWebJobsStorageVariableName)]
            Stream outputBlob,
            [CosmosDB(
                databaseName: Constants.DatabaseName,
                containerName: Constants.ContainerName,
                Connection = Constants.CosmosDBConnectionVariableName,
                CreateIfNotExists = true,
                PartitionKey = Constants.UniquePartitionKey)]
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
                    Artist = Constants.ArtistName,
                    PartitionKey = Constants.UniquePartitionKey
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