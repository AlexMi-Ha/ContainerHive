using ContainerHive.Core.Common.Exceptions;
using ContainerHive.Core.Common.Interfaces;
using ContainerHive.Core.Models;
using ContainerHive.Filters;
using ContainerHive.Workers;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace ContainerHive.Controllers {
    [ApiController]
    [Route("projects")]
    [ServiceFilter(typeof(ApiKeyAuthFilter))]
    public class ProjectController : Controller {

        private readonly IProjectService _projectService;
        private readonly BackgroundWorkerQueue _backgroundWorkerQueue;
        private readonly IDeploymentService _deploymentService;

        public ProjectController(IProjectService projectService, BackgroundWorkerQueue backgroundWorkerQueue, IDeploymentService deploymentService) {
            _projectService = projectService!;
            _backgroundWorkerQueue = backgroundWorkerQueue!;
            _deploymentService = deploymentService!;
        }

        [HttpPost]
        [Route("")]
        public async Task<IActionResult> AddProject([FromBody] Project project) {
            var res = await _projectService.AddProjectAsync(project);
            return res.Match<IActionResult>(
                Ok,
                err => {
                    if(err is ValidationException) {
                        return BadRequest(err.Message);
                    }
                    return StatusCode(500, err.Message);
                }
                );
        }

        [HttpPut]
        [Route("")]
        public async Task<IActionResult> UpdateProject([FromBody]Project project) {
            var res = await _projectService.UpdateProjectAsync(project);
            return res.Match<IActionResult>(
                Ok,
                err => {
                    if (err is ValidationException) {
                        return BadRequest(err.Message);
                    }
                    return StatusCode(500, err.Message);
                }
                );
        }

        [HttpDelete]
        [Route("{id}")]
        public async Task<IActionResult> DeleteProject([FromRoute]string id, CancellationToken cancelToken) {
            var res = await _projectService.DeleteProjectAsync(id, cancelToken);
            return res.Match<IActionResult>(
                succ => succ ? Ok(id) : UnprocessableEntity(),
                err => {
                    if(err is RecordNotFoundException) {
                        return NotFound(err.Message);
                    }
                    return StatusCode(500, err.Message);
                }
                );
        }

        [HttpGet]
        [Route("{id}")]
        public async Task<IActionResult> GetProjectById([FromRoute]string id) {
            var res = await _projectService.GetProjectAsync(id);
            return res.Match<IActionResult>(
                Ok,
                err => NotFound(err.Message)
                );
        }

        [HttpGet]
        [Route("")]
        public async Task<IActionResult> GetProjects() {
            return Ok((await _projectService.GetProjectsAsync()).ToArray());
        }

        [HttpGet]
        [Route("{id}/deployments")]
        public async Task<IActionResult> GetDeployments([FromRoute]string projId) {
            return Ok((await _deploymentService.GetDeploymentsByProjectIdAsync(projId)).ToArray());
        }

        // TODO: custom Networks

        [HttpPost]
        [Route("{id}/webhook/regenerate")]
        public async Task<IActionResult> RegenerateWebhookToken([FromRoute]string id) {
            var res = await _projectService.RegenerateTokenAsync(id);
            return res.Match<IActionResult>(
                    Ok,
                    err => err is ArgumentException ? 
                        BadRequest(err.Message) 
                        : StatusCode(500,err.Message)
                ) ;
        }

        [HttpPost]
        [Route("{id}/deploy")]
        public IActionResult StartDeployAllTask([FromRoute]string id) {
            _backgroundWorkerQueue.QueueBackgroundItem(async token => await _projectService.DeployAllAsync(id, token));
            return Accepted();
        }

        [HttpPost]
        [Route("{id}/kill")]
        public IActionResult StartKillAllTask([FromRoute] string id) {
            _backgroundWorkerQueue.QueueBackgroundItem(async token => await _projectService.KillAllContainersAsync(id, token));
            return Accepted();
        }
    }
}
