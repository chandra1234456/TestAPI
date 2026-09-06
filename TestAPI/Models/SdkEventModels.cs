using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TestAPI.Models
{
    [Table("sdk_events")]
    public class SdkEvent
    {
        [Key]
        [Column("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [Column("project_id")]
        public string ProjectId { get; set; } = string.Empty;

        [Required]
        [Column("session_id")]
        public string SessionId { get; set; } = string.Empty;

        [Required]
        [Column("type")]
        public string Type { get; set; } = string.Empty; // Log, Crash, Network, Breadcrumb, ANR, Performance, Device

        [Column("timestamp")]
        public long Timestamp { get; set; }

        [Column("payload", TypeName = "text")]
        public string Payload { get; set; } = "{}";

        [Column("device_info", TypeName = "text")]
        public string? DeviceInfo { get; set; }

        [Column("user_id")]
        public string? UserId { get; set; }

        [Column("created_utc")]
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    }

    [Table("sdk_sessions")]
    public class SdkSession
    {
        [Key]
        [Column("session_id")]
        public string SessionId { get; set; } = string.Empty;

        [Column("project_id")]
        public string ProjectId { get; set; } = string.Empty;

        [Column("user_id")]
        public string? UserId { get; set; }

        [Column("device_model")]
        public string? DeviceModel { get; set; }

        [Column("app_version")]
        public string? AppVersion { get; set; }

        [Column("start_time_utc")]
        public DateTime StartTimeUtc { get; set; } = DateTime.UtcNow;

        [Column("last_activity_utc")]
        public DateTime LastActivityUtc { get; set; } = DateTime.UtcNow;

        [Column("event_count")]
        public int EventCount { get; set; } = 0;

        [Column("has_crash")]
        public bool HasCrash { get; set; } = false;
    }

    [Table("sdk_crashes")]
    public class SdkCrash
    {
        [Key]
        [Column("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Column("project_id")]
        public string ProjectId { get; set; } = string.Empty;

        [Column("session_id")]
        public string SessionId { get; set; } = string.Empty;

        [Column("exception_name")]
        public string ExceptionName { get; set; } = string.Empty;

        [Column("exception_message")]
        public string ExceptionMessage { get; set; } = string.Empty;

        [Column("stack_trace")]
        public string StackTrace { get; set; } = string.Empty;

        [Column("breadcrumbs_json", TypeName = "text")]
        public string BreadcrumbsJson { get; set; } = "[]";

        [Column("device_info_json", TypeName = "text")]
        public string DeviceInfoJson { get; set; } = "{}";

        [Column("timestamp")]
        public long Timestamp { get; set; }

        [Column("created_utc")]
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    }

    [Table("sdk_logs")]
    public class SdkLog
    {
        [Key]
        [Column("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Column("project_id")]
        public string ProjectId { get; set; } = string.Empty;

        [Column("session_id")]
        public string SessionId { get; set; } = string.Empty;

        [Column("level")]
        public string Level { get; set; } = "INFO"; // DEBUG, INFO, WARNING, ERROR

        [Column("message")]
        public string Message { get; set; } = string.Empty;

        [Column("tag")]
        public string? Tag { get; set; }

        [Column("timestamp")]
        public long Timestamp { get; set; }

        [Column("created_utc")]
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    }

    [Table("sdk_networks")]
    public class SdkNetwork
    {
        [Key]
        [Column("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Column("project_id")]
        public string ProjectId { get; set; } = string.Empty;

        [Column("session_id")]
        public string SessionId { get; set; } = string.Empty;

        [Column("method")]
        public string Method { get; set; } = "GET";

        [Column("url")]
        public string Url { get; set; } = string.Empty;

        [Column("status_code")]
        public int? StatusCode { get; set; }

        [Column("duration_ms")]
        public long DurationMs { get; set; }

        [Column("error_message")]
        public string? ErrorMessage { get; set; }

        [Column("timestamp")]
        public long Timestamp { get; set; }

        [Column("created_utc")]
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    }

    // DTOs for API endpoints
    public class BatchEventRequestDto
    {
        public string ProjectId { get; set; } = "default_project";
        public string SessionId { get; set; } = string.Empty;
        public string? UserId { get; set; }
        public string? DeviceModel { get; set; }
        public string? AppVersion { get; set; }
        public List<EventDto> Events { get; set; } = new();
    }

    public class EventDto
    {
        public string? Id { get; set; }
        public string Type { get; set; } = "Log"; // Log, Crash, Network, Breadcrumb, ANR, Performance, Device
        public long Timestamp { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        public string? Message { get; set; }
        public string? Level { get; set; }
        public string? Exception { get; set; }
        public string? StackTrace { get; set; }
        public string? Method { get; set; }
        public string? Url { get; set; }
        public int? StatusCode { get; set; }
        public long? DurationMs { get; set; }
        public string? Error { get; set; }
        public List<string>? Breadcrumbs { get; set; }
        public Dictionary<string, string>? DeviceInfo { get; set; }
        public Dictionary<string, string>? Extra { get; set; }
    }

    public class AnalyticsSummaryDto
    {
        public int TotalEvents { get; set; }
        public int TotalSessions { get; set; }
        public int TotalCrashes { get; set; }
        public int TotalLogs { get; set; }
        public int TotalNetworkRequests { get; set; }
        public int NetworkErrors { get; set; }
        public double AverageLatencyMs { get; set; }
        public Dictionary<string, int> LogLevelBreakdown { get; set; } = new();
        public Dictionary<string, int> EventTypeBreakdown { get; set; } = new();
        public List<SdkCrash> RecentCrashes { get; set; } = new();
    }
}
