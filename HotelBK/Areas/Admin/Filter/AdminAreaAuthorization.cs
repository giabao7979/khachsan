using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace HotelBK.Areas.Admin.Filters
{
    public class AdminAreaAuthorization : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            base.OnActionExecuting(context);

            if (!context.HttpContext.User.Identity.IsAuthenticated)
            {
                context.Result = new RedirectToActionResult("Index", "Login", null);
                return;
            }

            if (!context.HttpContext.User.IsInRole("Admin"))
            {
                context.Result = new RedirectToActionResult("Index", "Home", null);
                return;
            }
        }
    }
}