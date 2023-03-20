using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ContainerHive.Filters {
    public class ApiKeyAuthFilter : IAuthorizationFilter {

        private readonly IConfiguration _configuration;

        public ApiKeyAuthFilter(IConfiguration configuration) {
            _configuration = configuration!;
        }


        public void OnAuthorization(AuthorizationFilterContext context) {
            if(!context.HttpContext.Request.Headers.TryGetValue("x-api-public-token", out var token)) {
                context.Result = new UnauthorizedObjectResult("API Key missing");
                return;
            }

            var apiKey = _configuration.GetValue<string>("ApiPrivateKey")!;
            if(!apiKey.Equals(token)) {
                context.Result = new UnauthorizedObjectResult("Invalid API Key");
                return;
            }

        }
    }
}
