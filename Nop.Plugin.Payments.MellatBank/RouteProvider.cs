using System.Web.Mvc;
using System.Web.Routing;
using Nop.Web.Framework.Mvc.Routes;

namespace Nop.Plugin.Payments.MellatBank
{
    public partial class RouteProvider : IRouteProvider
    {
        public void RegisterRoutes(RouteCollection routes)
        {
            routes.MapRoute("Plugin.PaymentIR.Settings",
                 "Plugins/PaymentIR/Settings",
                 new { controller = "PaymentMellatBank", action = "Configure", },
                 new[] { "Nop.Plugin.Payments.MellatBank.Controllers" }
            );

            routes.MapRoute("Plugin.PaymentIR.ManageTransactions.List",
                 "Plugins/PaymentIR/List",
                 new { controller = "ManageTransactionsAdmin", action = "List" },
                 new[] { "Nop.Plugin.Payments.MellatBank.Controllers.Admin" }
            );
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
            routes.MapRoute("Plugin.Payments.MellatBank.RedirectVPOS",
                 "Plugins/PaymentMellatBank/RedirectVPOS",
                 new { controller = "PaymentMellatBank", action = "RedirectVPOS" },
                 new[] { "Nop.Plugin.Payments.MellatBank.Controllers" }
            );
            routes.MapRoute("Plugin.Payments.MellatBank.ShowError",
                 "Plugins/PaymentMellatBank/ShowError",
                 new { controller = "PaymentMellatBank", action = "ShowError" },
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
