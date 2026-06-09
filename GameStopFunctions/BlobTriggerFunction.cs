using Azure.Data.Tables;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace GameStopFunctions
{
    public class BlobTriggerFunction
    {
        private readonly ILogger<BlobTriggerFunction> _logger;

        public BlobTriggerFunction(ILogger<BlobTriggerFunction> logger)
        {
            _logger = logger;
        }

        [Function("BlobTriggerFunction")]
        public async Task Run(
            [BlobTrigger("game-images/{name}", Connection = "StorageConnection")] Stream stream,
            string name)
        {
            _logger.LogInformation($"Blob trigger fired for: {name}, Size: {stream.Length} bytes");

            // Write audit log to Azure Table Storage
            var tableClient = new TableServiceClient(
                Environment.GetEnvironmentVariable("StorageConnection"))
                .GetTableClient("AuditLogs");

            await tableClient.CreateIfNotExistsAsync();

            await tableClient.AddEntityAsync(new TableEntity
            {
                PartitionKey = "BlobUpload",
                RowKey = Guid.NewGuid().ToString(),
                ["Action"] = "ImageUploaded",
                ["Details"] = $"Image '{name}' was uploaded to Blob Storage. Size: {stream.Length} bytes.",
                ["Timestamp"] = DateTimeOffset.UtcNow
            });

            _logger.LogInformation($"Audit log written for blob: {name}");
        }
    }
}