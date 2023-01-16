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
using System.Net.Http;
using ClientBackend.Model;

namespace ClientBackend
{
    public static class SetPicture
    {
        [FunctionName(nameof(SetPicture))]
        public static async Task<IActionResult> Run(
            [HttpTrigger(
                AuthorizationLevel.Anonymous,
                "post", 
                Route = "picture")] 
            HttpRequest req,
            [CosmosDB(
                databaseName: Constants.DatabaseName,
                containerName: Constants.ContainerName,
                Connection = Constants.CosmosDBConnectionVariableName,
                CreateIfNotExists = true,
                PartitionKey = Constants.UniquePartitionKey)]
            IAsyncCollector<PictureMetadata> collector,
            ILogger log)
        {
            try
            {
                var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                var data = JsonConvert.DeserializeObject<PictureMetadata>(requestBody);
                await collector.AddAsync(data);

                // Call the function to set the EXIF in the blob
                var client = new HttpClient();

                var baseUrl = Environment.GetEnvironmentVariable(Constants.MainFunctionsUrlVariableName);
                var url = $"{baseUrl}/set-metadata/{data.Name}";
                var response = await Proxy.Http.PostAsJsonAsync(url, data);

                if (response.IsSuccessStatusCode)
                {
                    return new OkObjectResult("OK");
                }

                return new UnprocessableEntityObjectResult(response.ReasonPhrase);
            }
            catch (Exception ex)
            {
                log.LogError(ex.Message);
                return new UnprocessableEntityObjectResult(ex.Message);
            }
        }
    }
}
