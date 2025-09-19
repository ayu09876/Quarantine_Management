using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Routing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Quarantine_Management.Function
{
    public static class TagHelper
    {
        //public static string GenerateId()
        //{
        //    Random rnd = new Random();
        //    rnd.Next(10000, 99999);
        //    return "SKILL-ISSUE-" + rnd.Next();
        //}
        public static string IsActive(this IHtmlHelper helper, string controller, string action)
        {
            ViewContext context = helper.ViewContext;
            RouteValueDictionary values = context.RouteData.Values;

            string _controller = Convert.ToString(values["controller"]) ?? string.Empty;
            string _action = Convert.ToString(values["action"]) ?? string.Empty;

            return (controller == _controller && action == _action) ? "active" : "";
        }

        public static string IsMenuopen(this IHtmlHelper helper, string controller, string action)
        {
            ViewContext context = helper.ViewContext;
            RouteValueDictionary values = context.RouteData.Values;

            string _controller = Convert.ToString(values["controller"]) ?? string.Empty;
            string _action = Convert.ToString(values["action"]) ?? string.Empty;

            return (controller == _controller && action == _action) ? "menu-open" : "";
        }
    }
}