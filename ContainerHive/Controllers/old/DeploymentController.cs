using ContainerHive.Core.Common.Exceptions;
using ContainerHive.Core.Common.Interfaces;
using ContainerHive.Core.Models;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ContainerHive.Controllers.old
{

    [ApiController]
    [Route("deployments")]
    [Authorize]
    public class DeploymentController : Controller
    {

        private readonly IDeploymentService _deploymentService;

        public DeploymentController(IDeploymentService deploymentService)
        {
            _deploymentService = deploymentService!;
        }

        [HttpPost]
        [Route("")]
        public async Task<IActionResult> AddDeployment([FromBody] Deployment deployment)
        {
            var res = await _deploymentService.AddDeploymentAsync(deployment);
            return res.Match(
                Ok,
                err =>
                {
                    if (err is ValidationException)
                        return BadRequest(err.Message);
                    return StatusCode(500, err.Message);
                }
                );
        }

        [HttpPut]
        [Route("")]
        public async Task<IActionResult> UpdateDeployment([FromBody] Deployment deployment)
        {
            var res = await _deploymentService.UpdateDeploymentAsync(deployment);
            return res.Match(
                Ok,
                err =>
                {
                    if (err is ValidationException)
                        return BadRequest(err.Message);
                    return StatusCode(500, err.Message);
                }
                );
        }

        [HttpDelete]
        [Route("{id}")]
        public async Task<IActionResult> DeleteDeployment([FromRoute] string id, CancellationToken cancelToken)
        {
            var res = await _deploymentService.DeleteDeploymentAsync(id, cancelToken);
            return res.Match<IActionResult>(
                    succ => succ ? Ok() : UnprocessableEntity(),
                    err =>
                    {
                        if (err is RecordNotFoundException)
                        {
                            return NotFound(err.Message);
                        }
                        return StatusCode(500, err.Message);
                    }
                );
        }

        [HttpGet]
        [Route("{id}")]
        public async Task<IActionResult> GetDeploymentById([FromRoute] string id)
        {
            var res = await _deploymentService.GetDeploymentByIdAsync(id);
            return res.Match<IActionResult>(
                    Ok,
                    err => err is RecordNotFoundException ? NotFound(err.Message) : StatusCode(500, err.Message)
                );
        }

        [HttpGet]
        [Route("")]
        public async Task<IActionResult> GetDeployments()
        {
            return Ok((await _deploymentService.GetDeploymentsAsync()).ToArray());
        }
    }
}
