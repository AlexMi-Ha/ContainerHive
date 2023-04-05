using ContainerHive.Core.Common.Exceptions;
using ContainerHive.Core.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ContainerHive.Mvc.Controllers {
    [Route("project")]
    [Authorize]
    public class ProjectController : Controller {

        private readonly IDeploymentService _deploymentService;
        private readonly IProjectService _projectService;

        public ProjectController(IProjectService projectService, IDeploymentService deploymentService) {
            _projectService = projectService!;
            _deploymentService = deploymentService!;
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
