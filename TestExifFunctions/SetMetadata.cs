using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using TestExifFunctions.Model;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using SixLabors.ImageSharp;
using System.Linq;
using Microsoft.WindowsAzure.Storage;

namespace TestExifFunctions
{
    public static class SetMetadata
    {
        [FunctionName("SetMetadata")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(
                AuthorizationLevel.Anonymous, 
                "post", 
                Route = "set-metadata/{pictureName}")]
            HttpRequest req,
            string pictureName,
            [Blob(
                "outputs/{pictureName}",
                FileAccess.Read,
                Connection = Constants.AzureWebJobsStorageVariableName)]
            Stream inputBlob,
            ILogger log)
        {
            if (inputBlob == null)
            {
                var message = $"Not found: {pictureName}";
                log.LogError(message);
                return new BadRequestObjectResult(message);
            }

            try
            {
                var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                var data = JsonConvert.DeserializeObject<PictureMetadata>(requestBody);

                if (string.IsNullOrEmpty(data.Description))
                {
                    return new BadRequestObjectResult("No description found");
                }

                var tuple = await Image.LoadWithFormatAsync(inputBlob);
                var image = tuple.Image;
                var format = tuple.Format;
                var values = image.Metadata.ExifProfile.Values;

                var mustSave = false;

                var descriptionValue = values.FirstOrDefault(v => v.Tag == ExifTag.ImageDescription);

                if (descriptionValue == null)
                {
                    image.Metadata.ExifProfile.SetValue(ExifTag.ImageDescription, data.Description);
                    mustSave = true;
                }
                else
                {
                    if (descriptionValue.GetValue().ToString() != data.Description)
                    {
                        mustSave = descriptionValue.TrySetValue(data.Description);

                        if (!mustSave)
                        {
                            var message = $"Couldn't save {pictureName}";
                            log.LogError(message);
                            return new UnprocessableEntityObjectResult(message);
                        }
                    }
                }

                if (mustSave)
                {
                    var account = CloudStorageAccount.Parse(
                        Environment.GetEnvironmentVariable(Constants.AzureWebJobsStorageVariableName));

                    var client = account.CreateCloudBlobClient();

                    var targetContainer = client.GetContainerReference(Constants.OutputFolderName);

                    var outputBlob = targetContainer.GetBlockBlobReference(pictureName);
                    outputBlob.Properties.ContentType = "image/jpg";

                    using (var outputStream = new MemoryStream())
                    {
                        await image.SaveAsync(outputStream, format);
                        outputStream.Position = 0;
                        await outputBlob.UploadFromStreamAsync(outputStream);
                    }
                }

                return new OkObjectResult(pictureName);
            }
            catch (Exception ex)
            {
                log.LogError(ex.Message);
                return new UnprocessableEntityObjectResult(ex.Message);
            }
        }
    }
}
