using ContainerHive.Core.Common.Exceptions;
using ContainerHive.Core.Common.Interfaces;
using ContainerHive.Mvc.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ContainerHive.Mvc.Controllers {
    [Route("project")]
    [Authorize]
    public class ProjectController : Controller {

        private readonly IDeploymentService _deploymentService;
        private readonly IProjectService _projectService;
        private readonly IDockerService _dockerService;

        public ProjectController(IProjectService projectService, IDeploymentService deploymentService, IDockerService dockerService) {
            _projectService = projectService!;
            _deploymentService = deploymentService!;
            _dockerService = dockerService;
        }

        [Route("{id}")]
        [HttpGet]
        public async Task<IActionResult> Index([FromRoute] string id) {
            ViewData["id"] = id;
            var proj = await _projectService.GetProjectAsync(id);
            return proj.Match<IActionResult>(
                succ => View(succ),
                err => err is RecordNotFoundException ? NotFound() : StatusCode(500, err)
            );
        }

        [Route("{id}/deployments")]
        [HttpGet]
        public async Task<IActionResult> Deployments([FromRoute]string id) {
            ViewData["id"] = id;
            var res = await _deploymentService.GetDeploymentsByProjectIdAsync(id);
            return View(res);
        }

        [Route("{id}/builds")]
        [HttpGet]
        public async Task<IActionResult> Builds([FromRoute]string id, CancellationToken cancelToken) {
            ViewData["id"] = id;
            BuildsViewModel vm = new();
            var containerRes = await _dockerService.GetAllContainersForProjectAsync(id, cancelToken);
            containerRes.IfSucc(vm.ContainerList.AddRange);

            if(containerRes.IsFaulted) {
                return NotFound(((Exception)containerRes).Message);
            }

            var imageRes = await _dockerService.GetAllImagesForProjectAsync(id);
            if(imageRes != null) {
                vm.Builds.AddRange(imageRes);
            }

            return View(vm);
        }


        [Route("{id}/api/resetToken")]
        [HttpPost]
        public async Task<IActionResult> ResetToken([FromRoute] string id) { 
            var res = await _projectService.RegenerateTokenAsync(id);
            return res.Match<IActionResult>(
                Ok,
                err => {
                    if(err is RecordNotFoundException) {
                        return NotFound();
                    }
                    if(err is ArgumentException) {
                        return BadRequest();
                    }
                    return StatusCode(500, err);
                }
            );
        }

        [HttpPost]
        [Route("{id}/api/togglewebhook")]
        public async Task<IActionResult> ToggleWebhook([FromRoute]string id, [FromForm]bool active) {
            var res = await _projectService.SetWebhookActiveAsync(id, active);
            return res.Match<IActionResult>(
                succ => Ok(),
                err => err is RecordNotFoundException ? NotFound() : StatusCode(500, err)
            );
        }
    }
}
