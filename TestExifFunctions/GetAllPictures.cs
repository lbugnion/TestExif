using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TestExifFunctions.Model;

namespace TestExifFunctions
{
    public static class GetAllPictures
    {
        [FunctionName(nameof(GetAllPictures))]
        public static async Task<IActionResult> Run(
            [HttpTrigger(
                AuthorizationLevel.Anonymous,
                "get",
                Route = "pictures")]
            HttpRequest req,
            ILogger log)
        {
            try
            {
                var connection = Environment.GetEnvironmentVariable(Constants.CosmosDBConnectionVariableName);
                var client = new CosmosClient(connection);

                var container = client.GetContainer(Constants.DatabaseName, Constants.ContainerName);

                var q = container.GetItemLinqQueryable<PictureMetadata>();
                var iterator = q.ToFeedIterator();
                var results = await iterator.ReadNextAsync();

                return new OkObjectResult(results);
            }
            catch (Exception ex)
            {
                return new UnprocessableEntityObjectResult(ex.Message);
            }
        }
    }
}