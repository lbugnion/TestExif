using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SixLabors.ImageSharp;
using TestExifFunctions.Model;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using System.Linq;

namespace TestExifFunctions
{
    public static class GetImageMetadata
    {
        [FunctionName("GetImageMetadata")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(
                AuthorizationLevel.Anonymous, 
                "get", 
                Route = "metadata/{blobName}")]
            HttpRequest req,
            string blobName,
            ILogger log)
        {
            var connection = Environment.GetEnvironmentVariable(Constants.CosmosDBConnectionVariableName);
            var client = new CosmosClient(connection);

            var container = client.GetContainer(Constants.DatabaseName, Constants.ContainerName);

            var q = container.GetItemLinqQueryable<PictureMetadata>();
            var iterator = q.Where(p => p.Name == blobName).ToFeedIterator();
            var results = await iterator.ReadNextAsync();
            var existingMedata = results.FirstOrDefault();

            if (existingMedata == null)
            {
                var message = $"Not found: {blobName}";
                log.LogError(message);
                return new BadRequestObjectResult(message);
            };

            return new OkObjectResult(existingMedata);
        }
    }
}