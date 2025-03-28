using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Collections.Generic;

namespace HotelBK.Areas.Admin.Filters
{
    public class AdminAreaAuthorization : ActionFilterAttribute
    {
        private readonly string[] _allowedRoles;

        // Static mapping of controllers to roles that can access them
        private static readonly Dictionary<string, string[]> ControllerRoleMap = new Dictionary<string, string[]>
        {
            // Admin has access to everything (not needed to specify explicitly)
            
            // Staff can access these controllers
            { "Rooms", new[] { "Admin", "Staff", "Reception" } },
            { "Bookings", new[] { "Admin", "Staff", "Reception" } },
            { "Payments", new[] { "Admin", "Staff", "Reception" } },
            { "Reports", new[] { "Admin", "Staff" } },
            { "ContactMessages", new[] { "Admin", "Staff" } },
            { "Reviews", new[] { "Admin", "Staff" } },
            { "AdminHome", new[] { "Admin", "Staff", "Reception" } },
            
            // Reception has limited access (already specified above)
            
            // Admin only controllers
            { "User", new[] { "Admin" } },
            { "Roles", new[] { "Admin" } },
            { "Notifications", new[] { "Admin", "Staff", "Reception" } }
            
            // AdminHome can be accessed by anyone who can log in to admin
            // Default behavior will allow any authenticated admin user to access
        };

        public AdminAreaAuthorization(params string[] roles)
        {
            _allowedRoles = roles;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            base.OnActionExecuting(context);

            // First check if user is authenticated
            if (!context.HttpContext.User.Identity.IsAuthenticated)
            {
                context.Result = new RedirectToActionResult("Index", "Login", null);
                return;
            }

            // All users must have at least one admin role to access the Admin area
            bool hasAdminAccess = context.HttpContext.User.IsInRole("Admin") ||
                                 context.HttpContext.User.IsInRole("Staff") ||
                                 context.HttpContext.User.IsInRole("Reception");

            if (!hasAdminAccess)
            {
                context.Result = new RedirectToActionResult("Index", "Home", null);
                return;
            }

            // If Admin, grant access to everything
            if (context.HttpContext.User.IsInRole("Admin"))
            {
                // Admin has full access
                return;
            }

            // For non-admin roles, check the controller-specific permissions
            string controllerName = context.RouteData.Values["controller"].ToString();

            // Check if we have defined specific role requirements for this controller
            if (ControllerRoleMap.TryGetValue(controllerName, out var allowedRoles))
            {
                bool hasAccess = false;

                // Check if the user has any of the allowed roles for this controller
                foreach (var role in allowedRoles)
                {
                    if (context.HttpContext.User.IsInRole(role))
                    {
                        hasAccess = true;
                        break;
                    }
                }

                if (!hasAccess)
                {
                    // User doesn't have the required role to access this controller
                    context.Result = new RedirectToActionResult("Index", "AdminHome", new { area = "Admin" });
                    return;
                }
            }
            else if (_allowedRoles.Length > 0)
            {
                // If specific roles were defined in the attribute itself, check those
                bool hasRequiredRole = false;

                foreach (var role in _allowedRoles)
                {
                    if (context.HttpContext.User.IsInRole(role))
                    {
                        hasRequiredRole = true;
                        break;
                    }
                }

                if (!hasRequiredRole)
                {
                    context.Result = new RedirectToActionResult("Index", "AdminHome", new { area = "Admin" });
                    return;
                }
            }

            // If no specific roles defined for this controller and no specific roles in attribute,
            // then we've already verified the user has some admin access role above
        }
    }
}