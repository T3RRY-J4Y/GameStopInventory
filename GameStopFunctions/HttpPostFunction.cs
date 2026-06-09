using Azure.Storage.Queues;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text;
using System.Text.Json;

namespace GameStopFunctions
{
    public class HttpPostFunction
    {
        private readonly ILogger<HttpPostFunction> _logger;

        public HttpPostFunction(ILogger<HttpPostFunction> logger)
        {
            _logger = logger;
        }

        [Function("PlaceOrder")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "orders")] HttpRequestData req)
        {
            _logger.LogInformation("PlaceOrder HTTP POST function triggered.");

            var response = req.CreateResponse();

            try
            {
                var body = await new StreamReader(req.Body).ReadToEndAsync();
                var order = JsonSerializer.Deserialize<OrderRequest>(body,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (order == null || order.GameId <= 0 || order.Quantity <= 0)
                {
                    response.StatusCode = HttpStatusCode.BadRequest;
                    await response.WriteStringAsync("{\"error\": \"Invalid order data\"}");
                    return response;
                }

                // Place order message into Azure Storage Queue
                var queueClient = new QueueClient(
                    Environment.GetEnvironmentVariable("StorageConnection"),
                    "orders-queue");

                await queueClient.CreateIfNotExistsAsync();

                var message = JsonSerializer.Serialize(new
                {
                    order.GameId,
                    order.Quantity,
                    order.Username,
                    OrderedAt = DateTime.UtcNow
                });

                // Queue messages must be base64 encoded
                var encodedMessage = Convert.ToBase64String(
                    Encoding.UTF8.GetBytes(message));

                await queueClient.SendMessageAsync(encodedMessage);

                _logger.LogInformation($"Order queued for GameId: {order.GameId}");

                response.StatusCode = HttpStatusCode.OK;
                response.Headers.Add("Content-Type", "application/json");
                await response.WriteStringAsync("{\"message\": \"Order placed successfully\"}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error placing order: {ex.Message}");
                response.StatusCode = HttpStatusCode.InternalServerError;
                await response.WriteStringAsync("{\"error\": \"Internal server error\"}");
            }

            return response;
        }
    }

    public class OrderRequest
    {
        public int GameId { get; set; }
        public int Quantity { get; set; }
        public string Username { get; set; } = string.Empty;
    }
}