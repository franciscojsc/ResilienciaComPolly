using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;

namespace PollyWebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PollyController : ControllerBase
    {
        [HttpGet("retry")]
        public ActionResult<IEnumerable<string>> Retry()
        {
            return new string[] { "valor 1", "valor 2", "valor 3" };
        }

        [HttpGet("admin")]
        public ActionResult<IEnumerable<string>> Admin()
        {
            return StatusCode(401);
            // return new string[] { "valor 1", "valor 2", "valor 3" };
        }

        [HttpGet("timeout")]
        public async Task<ActionResult<IEnumerable<string>>> Timeout()
        {
            await Task.Delay(10 * 1000);
            return new string[] { "valor 1", "valor 2", "valor 3" };
        }

        [HttpGet("circuitBreaker")]
        public async Task<ActionResult<IEnumerable<string>>> CircuitBreaker()
        {
            throw new Exception();
            // return new string[] { "valor 1", "valor 2", "valor 3" };
        }
    }
}
