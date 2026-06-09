using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;

namespace GameStopFunctions
{
    public class HttpGetFunction
    {
        private readonly ILogger<HttpGetFunction> _logger;

        public HttpGetFunction(ILogger<HttpGetFunction> logger)
        {
            _logger = logger;
        }

        [Function("GetGameStock")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "games/{id}/stock")] HttpRequestData req,
            int id)
        {
            _logger.LogInformation($"Fetching stock for game ID: {id}");

            var response = req.CreateResponse();

            try
            {
                var connectionString = Environment.GetEnvironmentVariable("SqlConnection");
                using var conn = new SqlConnection(connectionString);
                await conn.OpenAsync();

                using var cmd = new SqlCommand(
                    "SELECT Id, Title, StockLevel FROM Games WHERE Id = @id", conn);
                cmd.Parameters.AddWithValue("@id", id);

                using var reader = await cmd.ExecuteReaderAsync();

                if (reader.Read())
                {
                    var result = new
                    {
                        Id = reader.GetInt32(0),
                        Title = reader.GetString(1),
                        StockLevel = reader.GetInt32(2)
                    };

                    response.StatusCode = HttpStatusCode.OK;
                    response.Headers.Add("Content-Type", "application/json");
                    await response.WriteStringAsync(JsonSerializer.Serialize(result));
                }
                else
                {
                    response.StatusCode = HttpStatusCode.NotFound;
                    await response.WriteStringAsync("{\"error\": \"Game not found\"}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error fetching game stock: {ex.Message}");
                response.StatusCode = HttpStatusCode.InternalServerError;
                await response.WriteStringAsync("{\"error\": \"Internal server error\"}");
            }

            return response;
        }
    }
}