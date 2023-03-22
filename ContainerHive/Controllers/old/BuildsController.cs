using ContainerHive.Core.Common.Exceptions;
using ContainerHive.Core.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ContainerHive.Controllers.old
{

    [ApiController]
    [Route("projects/{projId}/builds")]
    [Authorize]
    public class BuildsController : Controller
    {

        private readonly IDockerService _dockerService;

        public BuildsController(IDockerService dockerService)
        {
            _dockerService = dockerService!;
        }

        [HttpGet]
        [Route("images")]
        public async Task<IActionResult> GetBuilds([FromRoute] string projId)
        {
            return Ok((await _dockerService.GetAllImagesForProjectAsync(projId)).ToArray());
        }

        [HttpGet]
        [Route("images/{buildId}")]
        public async Task<IActionResult> GetBuildById([FromRoute] string buildId)
        {
            var res = await _dockerService.GetImageByIdAsync(buildId);
            return res.Match<IActionResult>(
                    Ok,
                    err =>
                    {
                        if (err is RecordNotFoundException)
                            return NotFound(err.Message);
                        return StatusCode(500, err.Message);
                    }
                );
        }

        [HttpGet]
        [Route("containers")]
        public async Task<IActionResult> GetContainers([FromRoute] string projId, CancellationToken cancelToken)
        {
            var res = await _dockerService.GetAllContainersForProjectAsync(projId, cancelToken);
            return res.Match<IActionResult>(
                    Ok,
                    err =>
                    {
                        if (err is OperationCanceledException)
                        {
                            return StatusCode(499, err.Message);
                        }
                        return StatusCode(500, err.Message);
                    }
                );
        }

        [HttpGet]
        [Route("containers/{buildId}")]
        public async Task<IActionResult> GetContainersById([FromRoute] string buildId, CancellationToken cancelToken)
        {
            var res = await _dockerService.GetContainerLogsAsync(buildId, cancelToken);
            return res.Match<IActionResult>(
                    Ok,
                    err =>
                    {
                        if (err is OperationCanceledException)
                        {
                            return StatusCode(499, err.Message);
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
