using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAnalytics.Core.DTOs;
using WebAnalytics.Infrastructure.Services;

namespace WebAnalytics.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ReportsController : ControllerBase
    {
        private readonly IReportService _reportService;
        private readonly ILogger<ReportsController> _logger;

        public ReportsController(IReportService reportService, ILogger<ReportsController> logger)
        {
            _reportService = reportService;
            _logger = logger;
        }

        [HttpGet("overview")]
        public async Task<ActionResult<OverviewReport>> GetOverview()
        {
            try
            {
                var report = await _reportService.GetOverviewReportAsync();
                return Ok(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to generate overview report");
                return StatusCode(500, new { Error = "Failed to generate report" });
            }
        }

        [HttpGet("pages")]
        public async Task<ActionResult<List<PageReport>>> GetPages()
        {
            try
            {
                var reports = await _reportService.GetPageReportsAsync();
                return Ok(reports);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to generate page reports");
                return StatusCode(500, new { Error = "Failed to generate reports" });
            }
        }
    }
}