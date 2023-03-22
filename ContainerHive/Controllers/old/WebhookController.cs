using ContainerHive.Core.Common.Exceptions;
using ContainerHive.Core.Common.Interfaces;
using ContainerHive.Workers;
using Microsoft.AspNetCore.Mvc;

namespace ContainerHive.Controllers.old
{

    [Route("webhooks")]
    [ApiController]
    public class WebhookController : Controller
    {

        private readonly IProjectService _projectService;
        private readonly BackgroundWorkerQueue _backgroundWorkerQueue;

        public WebhookController(IProjectService projectService, BackgroundWorkerQueue backgroundWorkerQueue)
        {
            _projectService = projectService!;
            _backgroundWorkerQueue = backgroundWorkerQueue!;
        }

        [HttpPost]
        [Route("{id}/deploy")]
        public async Task<IActionResult> DeployAllTask([FromRoute] string id, [FromBody] string apiToken)
        {
            var cmpRes = await _projectService.CompareApiKeyAsync(id, apiToken);
            return cmpRes.Match<IActionResult>(
                succ =>
                {
                    _backgroundWorkerQueue.QueueBackgroundItem(async token => await _projectService.DeployAllAsync(id, token));
                    return Accepted();
                },
                err =>
                {
                    if (err is ArgumentException)
                    {
                        return BadRequest(err.Message);
                    }
                    if (err is RecordNotFoundException)
                    {
                        return NotFound(err.Message);
                    }
                    return StatusCode(500, err.Message);
                }
                );
        }

        [HttpPost]
        [Route("{id}/kill")]
        public async Task<IActionResult> KillAllTask([FromRoute] string id, [FromBody] string apiToken)
        {
            var cmpRes = await _projectService.CompareApiKeyAsync(id, apiToken);
            return cmpRes.Match<IActionResult>(
                succ =>
                {
                    _backgroundWorkerQueue.QueueBackgroundItem(async token => await _projectService.KillAllContainersAsync(id, token));
                    return Accepted();
                },
                err =>
                {
                    if (err is ArgumentException)
                    {
                        return BadRequest(err.Message);
                    }
                    if (err is RecordNotFoundException)
                    {
                        return NotFound(err.Message);
                    }
                    return StatusCode(500, err.Message);
                }
                );
        }
    }
}
