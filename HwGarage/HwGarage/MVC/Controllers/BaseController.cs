using System.Collections.Generic;
using System.Threading.Tasks;
using HwGarage.Core.Http;

namespace HwGarage.MVC.Controllers
{
    public abstract class BaseController
    {
        protected readonly ViewRenderer _renderer;

        protected BaseController(ViewRenderer renderer)
        {
            _renderer = renderer;
        }

        protected async Task RenderView(HttpContext context, string viewPath, Dictionary<string, object>? model = null)
        {
            var html = await _renderer.RenderAsync(viewPath, model);
            await context.WriteAsync(html);
        }
    }
}