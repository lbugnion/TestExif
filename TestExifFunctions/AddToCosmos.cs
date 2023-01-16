using System.IO;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using TestExifFunctions.Model;

namespace TestExifFunctions
{
    public static class AddToCosmos
    {
        private const string UniquePartitionKey = "partition";

        [FunctionName("AddToCosmos")]
        public static void Run(
            [HttpTrigger(
                AuthorizationLevel.Anonymous, 
                "post", 
                Route = "add-to-cosmos")]
            HttpRequest req,
            [CosmosDB(
                databaseName: "PicturesDb",
                containerName: "Pictures",
                Connection = "CosmosDBConnection",
                CreateIfNotExists = true,
                PartitionKey = "partition")]
            out PictureMetadata outputMetadata,
            ILogger log)
        {
            string requestBody = new StreamReader(req.Body).ReadToEnd();
            outputMetadata = JsonConvert.DeserializeObject<PictureMetadata>(requestBody);
            outputMetadata.PartitionKey = UniquePartitionKey;
        }
    }
}
