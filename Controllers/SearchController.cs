using LeakOsintApi.Models;
using LeakOsintApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace LeakOsintApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SearchController : ControllerBase
{
    private readonly LeakOsintService _service;

    public SearchController(LeakOsintService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<IActionResult> Search(SearchRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Query))
            return BadRequest("Query is required.");

        var result = await _service.SearchAsync(
            request.Query,
            request.Limit,
            request.Lang);

        return Content(result, "application/json");
    }
}