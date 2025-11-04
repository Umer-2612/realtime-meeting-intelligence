using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;

namespace PsiBot.Services.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    /// <summary>
    /// Minimal sample controller used for template validation.
    /// </summary>
    public class ValuesController : ControllerBase
    {
        /// <summary>
        /// Returns a hardcoded list of values for API verification.
        /// </summary>
        [HttpGet]
        public ActionResult<IEnumerable<string>> Get()
        {
            return new string[] { "value1", "value2" };
        }

        /// <summary>
        /// Returns a fixed value to demonstrate parameter binding.
        /// </summary>
        [HttpGet("{id}")]
        public ActionResult<string> Get(int id)
        {
            return "value";
        }

        /// <summary>
        /// Placeholder endpoint for POST operations.
        /// </summary>
        [HttpPost]
        public void Post([FromBody] string value)
        {
        }

        /// <summary>
        /// Placeholder endpoint for PUT operations.
        /// </summary>
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }

        /// <summary>
        /// Placeholder endpoint for DELETE operations.
        /// </summary>
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
