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

    // POST: api/search/search
    [HttpPost("search")]
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

    // GET: api/search/coupons
    [HttpGet("getcoupons")]
    public IActionResult GetCoupons()
    {
        var coupons = new string[]
    {
    "LEAK2026",
    "OSINTPRO",
    "VAULTACCESS",
    "CYBERSCAN",
    "FULLREPORT",
    "PREMIUMLOOKUP",
    "DATADUMP",
    "BREACHFINDER",
    "TRACEACCESS",
    "ROOTACCESS",
    "BLACKARCH",
    "HUNTERMODE",
    "INFOLEAK",
    "SEARCHPLUS",
    "DEEPSEARCH",
    "INTELPASS",
    "DARKVAULT",
    "OPENSOURCE",
    "BREACH2026",
    "RECONX",
    "LEAKHUNTER",
    "FULLACCESS",
    "DATABASEX",
    "LEAKMASTER",
    "OSINTELITE",
    "PREMIUMDATA",
    "CYBERVAULT",
    "UNLOCKDATA",
    "DECRYPTX",
    "ACCESSGRANTED",
    "INTELPRO",
    "TARGETSCAN",
    "RECONPRO",
    "TRACE2026",
    "LEAKPASS",
    "HIDDENINFO",
    "LOOKUPMAX",
    "ULTIMATEOSINT",
    "DATABASEPRO",
    "SECRETACCESS",
    "ELITESEARCH",
    "MASTERLOOKUP",
    "INTELXPRO",
    "VAULTPRO",
    "CYBERELITE",
    "DARKSEARCH",
    "SCANMASTER",
    "OSINTACCESS",
    "RECONMASTER",
    "LEAKUNLOCK"
    };

        return Ok(coupons);
    }

    // POST: api/search/check-coupon
    [HttpPost("check-coupon")]
    public IActionResult CheckCoupon([FromBody] CouponRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Coupon))
            return BadRequest("Coupon is required.");

        var coupons = new string[]
    {
    "LEAK2026",
    "OSINTPRO",
    "VAULTACCESS",
    "CYBERSCAN",
    "FULLREPORT",
    "PREMIUMLOOKUP",
    "DATADUMP",
    "BREACHFINDER",
    "TRACEACCESS",
    "ROOTACCESS",
    "BLACKARCH",
    "HUNTERMODE",
    "INFOLEAK",
    "SEARCHPLUS",
    "DEEPSEARCH",
    "INTELPASS",
    "DARKVAULT",
    "OPENSOURCE",
    "BREACH2026",
    "RECONX",
    "LEAKHUNTER",
    "FULLACCESS",
    "DATABASEX",
    "LEAKMASTER",
    "OSINTELITE",
    "PREMIUMDATA",
    "CYBERVAULT",
    "UNLOCKDATA",
    "DECRYPTX",
    "ACCESSGRANTED",
    "INTELPRO",
    "TARGETSCAN",
    "RECONPRO",
    "TRACE2026",
    "LEAKPASS",
    "HIDDENINFO",
    "LOOKUPMAX",
    "ULTIMATEOSINT",
    "DATABASEPRO",
    "SECRETACCESS",
    "ELITESEARCH",
    "MASTERLOOKUP",
    "INTELXPRO",
    "VAULTPRO",
    "CYBERELITE",
    "DARKSEARCH",
    "SCANMASTER",
    "OSINTACCESS",
    "RECONMASTER",
    "LEAKUNLOCK"
    };

        bool isValid = coupons.Contains(request.Coupon, StringComparer.OrdinalIgnoreCase);

        return Ok(new
        {
            coupon = request.Coupon,
            valid = isValid
        });
    }
}