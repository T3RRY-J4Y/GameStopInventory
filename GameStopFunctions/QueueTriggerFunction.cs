using Microsoft.Azure.Functions.Worker;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace GameStopFunctions
{
    public class QueueTriggerFunction
    {
        private readonly ILogger<QueueTriggerFunction> _logger;

        public QueueTriggerFunction(ILogger<QueueTriggerFunction> logger)
        {
            _logger = logger;
        }

        [Function("ProcessOrder")]
        public async Task Run(
            [QueueTrigger("orders-queue", Connection = "StorageConnection")] string message)
        {
            _logger.LogInformation($"Queue trigger fired. Message: {message}");

            try
            {
                var order = JsonSerializer.Deserialize<OrderMessage>(message,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (order == null) return;

                var connectionString = Environment.GetEnvironmentVariable("SqlConnection");
                using var conn = new SqlConnection(connectionString);
                await conn.OpenAsync();

                // Update stock level in Azure SQL
                using var cmd = new SqlCommand(@"
                    UPDATE Games
                    SET StockLevel = StockLevel - @quantity
                    WHERE Id = @gameId AND StockLevel >= @quantity", conn);

                cmd.Parameters.AddWithValue("@quantity", order.Quantity);
                cmd.Parameters.AddWithValue("@gameId", order.GameId);

                var rowsAffected = await cmd.ExecuteNonQueryAsync();

                if (rowsAffected > 0)
                    _logger.LogInformation($"Stock updated for GameId: {order.GameId}, Qty: {order.Quantity}");
                else
                    _logger.LogWarning($"Stock update failed for GameId: {order.GameId} — insufficient stock.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error processing order: {ex.Message}");
                throw;
            }
        }
    }

    public class OrderMessage
    {
        public int GameId { get; set; }
        public int Quantity { get; set; }
        public string Username { get; set; } = string.Empty;
        public DateTime OrderedAt { get; set; }
    }
}