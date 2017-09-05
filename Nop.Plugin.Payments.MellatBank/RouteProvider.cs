using System.Web.Mvc;
using System.Web.Routing;
using Nop.Web.Framework.Mvc.Routes;

namespace Nop.Plugin.Payments.MellatBank
{
    public partial class RouteProvider : IRouteProvider
    {
        public void RegisterRoutes(RouteCollection routes)
        {
            //PDT
            routes.MapRoute("Plugin.Payments.MellatBank.PDTHandler",
                 "Plugins/PaymentMellatBank/PDTHandler",
                 new { controller = "PaymentMellatBank", action = "PDTHandler" },
                 new[] { "Nop.Plugin.Payments.MellatBank.Controllers" }
            );
            //Cancel
            routes.MapRoute("Plugin.Payments.MellatBank.CancelOrder",
                 "Plugins/PaymentMellatBank/CancelOrder",
                 new { controller = "PaymentMellatBank", action = "CancelOrder" },
                 new[] { "Nop.Plugin.Payments.MellatBank.Controllers" }
            );
        }
        public int Priority
        {
            get
            {
                return 0;
            }
        }
    }
}
