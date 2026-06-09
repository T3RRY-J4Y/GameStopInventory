using Azure;
using Azure.Data.Tables;

namespace GameStopInventory.Models
{
    public class AuditLog : ITableEntity
    {
        public string PartitionKey { get; set; } = "AuditLog";
        public string RowKey { get; set; } = Guid.NewGuid().ToString();
        public string Action { get; set; }
        public string Details { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
    }
}