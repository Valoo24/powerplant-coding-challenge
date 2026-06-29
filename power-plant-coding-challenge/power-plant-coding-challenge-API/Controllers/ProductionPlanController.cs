using MediatR;
using Microsoft.AspNetCore.Mvc;
using power_plant_coding_challenge_core.features;

namespace power_plant_coding_challenge_API.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class ProductionPlanController(IMediator mediator) : ControllerBase
    {
        [HttpPost]
        public async Task<IActionResult> CalculateProductionPlan([FromBody] CalculateProductionPlan.Command command, CancellationToken cancellationToken)
        {
            var response = await mediator.Send(command, cancellationToken);

            return Ok(response.Results);
        }
    }
}
