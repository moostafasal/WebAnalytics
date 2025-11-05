using Microsoft.AspNetCore.Mvc;
using WebAnalytics.Infrastructure.IServices;
using WebAnalytics.Infrastructure.Services;

namespace WebAnalytics.API.Controllers
{
    /// <summary>
    /// Handles data ingestion operations from various sources including Google Analytics and PageSpeed Insights
    /// </summary>
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
        /// <summary>
        /// Triggers data ingestion process from Google Analytics and PageSpeed Insights JSON files
        /// </summary>
        /// <remarks>
        /// Sample request:
        /// 
        ///     POST /api/ingest/trigger
        ///     {
        ///         "note": "This endpoint automatically processes mock data files"
        ///     }
        /// 
        /// This endpoint:
        /// - Reads Google Analytics data from MockData/google-analytics.json
        /// - Reads PageSpeed Insights data from MockData/pagespeed.json
        /// - Processes and stores the data in the database
        /// - Returns processing status and timestamp
        /// </remarks>
        /// <response code="200">Data processed successfully</response>
        /// <response code="500">Internal server error or data processing failed</response>
        /// <returns>Ingestion process result with status and timestamp</returns>
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