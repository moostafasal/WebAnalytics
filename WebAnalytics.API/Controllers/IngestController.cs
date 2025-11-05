using Microsoft.AspNetCore.Mvc;
using WebAnalytics.Infrastructure.Services;

namespace WebAnalytics.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class IngestController : ControllerBase
    {
        private readonly IDataIngestionService _dataIngestionService;
        private readonly ILogger<IngestController> _logger;

        public IngestController(IDataIngestionService dataIngestionService, ILogger<IngestController> logger)
        {
            _dataIngestionService = dataIngestionService;
            _logger = logger;
        }

        [HttpPost("trigger")]
        public async Task<IActionResult> TriggerIngestion()
        {
            try
            {
                var gaFilePath = "MockData/google-analytics.json";
                var psiFilePath = "MockData/pagespeed.json";

                var result = await _dataIngestionService.IngestFromJsonFilesAsync(gaFilePath, psiFilePath);

                if (result)
                {
                    return Ok(new
                    {
                        Status = "Success",
                        Message = "Data received and processed successfully",
                        Timestamp = DateTime.UtcNow
                    });
                }
                else
                {
                    return StatusCode(500, new
                    {
                        Status = "Error",
                        Message = "Data processing failed"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Data ingestion endpoint failed");
                return StatusCode(500, new { Error = "Data processing failed" });
            }
        }
    }
}