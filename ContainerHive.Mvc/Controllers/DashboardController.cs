using ContainerHive.Core.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ContainerHive.Mvc.Controllers {
    [Controller]
    [Authorize]
    public class DashboardController : Controller {

        private readonly IProjectService _projectService;

        public DashboardController(IProjectService projectService) {
            _projectService = projectService!;
        }


        public async Task<IActionResult> Index() {
            var res = await _projectService.GetProjectsAsync();
            return View(res);
        }
    }
}
