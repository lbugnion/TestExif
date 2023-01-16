using System.IO;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using TestExifFunctions.Model;
using System.Threading.Tasks;
using System;

namespace TestExifFunctions
{
    public static class AddToCosmos
    {
        private const string UniquePartitionKey = "partition";

        [FunctionName("AddToCosmos")]
        public static async Task Run(
            [HttpTrigger(
                AuthorizationLevel.Anonymous, 
                "post", 
                Route = "add-to-cosmos")]
            HttpRequest req,
            [CosmosDB(
                databaseName: "PicturesDb",
                containerName: "Pictures",
                Connection = "CosmosDBConnection",
                PartitionKey = "partition")]
            IAsyncCollector<PictureMetadata> output,
            ILogger log)
        {
            string requestBody = new StreamReader(req.Body).ReadToEnd();
            var outputMetadata = JsonConvert.DeserializeObject<PictureMetadata>(requestBody);
            outputMetadata.PartitionKey = UniquePartitionKey;

            try
            {
                await output.AddAsync(outputMetadata);
            }
            catch (Exception ex)
            {

            }
        }
    }
}
