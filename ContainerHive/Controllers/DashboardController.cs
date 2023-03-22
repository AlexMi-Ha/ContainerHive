using ContainerHive.Core.Common.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ContainerHive.Controllers {
    [Controller]
    public class DashboardController : Controller {

        private readonly IProjectService _projectService;

        public DashboardController(IProjectService projectService) {
            _projectService = projectService!;
        }

        public async IActionResult Index() {
            var res = await _projectService.GetProjectsAsync();
            return View(res);
        }
    }
}
