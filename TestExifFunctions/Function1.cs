using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using SixLabors.ImageSharp;
using System.Text;

namespace TestExifFunctions
{
    public static class Function1
    {
        private const string ArtistName = "Laurent Bugnion";

        [FunctionName("Function1")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(
                AuthorizationLevel.Anonymous,
                "get",
                Route = "add-metadata/{blobName}")]
            HttpRequest req,
            string blobName,
            [Blob($"uploads/{{blobName}}", FileAccess.Read, Connection = "AzureWebJobsStorage")]
            Stream inputBlob,
            [Blob($"outputs/{{blobName}}", FileAccess.Write, Connection = "AzureWebJobsStorage")]
            Stream outputBlob,
            ILogger log)
        {
            if (inputBlob == null)
            {
                var message = $"Not found: {blobName}";
                log.LogError(message);
                return new BadRequestObjectResult(message);
            }

            Image image = null;
            IImageFormat format = null;

            var tuple = await Image.LoadWithFormatAsync(inputBlob);
            image = tuple.Image;
            format = tuple.Format;

            var values = image.Metadata.ExifProfile.Values;

            foreach (var value in values)
            {
                if (value.IsArray)
                {
                    byte[]? array = value.GetValue() as byte[];

                    if (array != null)
                    {
                        var encoding = new ASCIIEncoding();
                        var text = encoding.GetString(
                            array,
                            0,
                            array.Length - 1);
                        log.LogInformation($"{value.Tag}: Byte array: {text}");
                    }
                    else
                    {
                        if (value.Tag == ExifTag.GPSLatitude
                            || value.Tag == ExifTag.GPSLongitude)
                        {
                            var latitudeArray = (Rational[])value.GetValue();
                            var latitude =
                                latitudeArray[0].Numerator
                                + (latitudeArray[1].Numerator / 60D)
                                + ((latitudeArray[2].Numerator / 1000000D) / 3600D);

                            log.LogInformation($"{value.Tag}: {latitude}");
                        }
                        else
                        {
                            log.LogInformation($"{value.Tag}: Not a byte array");
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

                    if (value.Tag == ExifTag.Artist
                        && valueString != ArtistName)
                    {
                        value.TrySetValue(ArtistName);
                    }
                }
            }

            log.LogInformation($"Saving {blobName} to outputs");
            await image.SaveAsync(outputBlob, format);
            return new OkObjectResult(blobName);
        }
    }
}
