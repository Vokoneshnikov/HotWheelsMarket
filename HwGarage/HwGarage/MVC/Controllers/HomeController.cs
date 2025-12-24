using System.Collections.Generic;
using System.Threading.Tasks;
using HwGarage.Core.Http;
using HwGarage.Core.Orm.Models;
using HwGarage.Core.Auth;

namespace HwGarage.MVC.Controllers
{
    public class HomeController : BaseController
    {
        public HomeController(ViewRenderer renderer) : base(renderer) { }

        public async Task Index(HttpContext context)
        {
            var user = context.User as User;

            string moderatorNavItem = "";

            if (user != null && user.HasRole("moderator"))
            {
                moderatorNavItem =
                    "<a href=\"/admin/moderation\" class=\"page-header__nav-item\">" +
                    "<span class=\"page-header__nav-icon\">🛡</span>" +
                    "<span class=\"page-header__nav-label\">Модерация</span>" +
                    "</a>";
            }

            var model = new Dictionary<string, object>
            {
                ["title"] = "HwGarage",
                ["moderatorNavItem"] = moderatorNavItem
            };

            await RenderView(context, "home/index.html", model);
        }
    }
}