using Microsoft.AspNetCore.Mvc;
using WebAnalytics.Infrastructure.Data;
using WebAnalytics.Infrastructure.Services;

namespace WebAnalytics.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HealthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IReportService _reportService;
        private readonly ILogger<HealthController> _logger;

        public HealthController(ApplicationDbContext context, IReportService reportService, ILogger<HealthController> logger)
        {
            _context = context;
            _reportService = reportService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetHealth()
        {
            try
            {
                var dbHealthy = await _context.Database.CanConnectAsync();

                var healthReport = new
                {
                    Status = dbHealthy ? "Healthy" : "Unhealthy",
                    Timestamp = DateTime.UtcNow,
                    Services = new
                    {
                        Database = dbHealthy ? "Connected" : "Disconnected",
                        API = "Healthy",
                        MessageBroker = "Healthy" // Can add RabbitMQ check here
                    },
                    Uptime = DateTime.UtcNow - System.Diagnostics.Process.GetCurrentProcess().StartTime.ToUniversalTime()
                };

                return Ok(healthReport);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Health check failed");
                return StatusCode(503, new { Status = "Unhealthy", Error = ex.Message });
            }
        }

        [HttpGet("metrics")]
        public async Task<IActionResult> GetMetrics()
        {
            try
            {
                var metrics = await _reportService.GetHealthMetricsAsync();
                return Ok(metrics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to fetch metrics");
                return StatusCode(500, new { Error = "Failed to fetch metrics" });
            }
        }
    }
}