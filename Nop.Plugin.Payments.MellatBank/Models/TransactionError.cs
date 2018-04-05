using Nop.Core;
using Nop.Web.Framework;
using Nop.Web.Framework.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nop.Plugin.Payments.MellatBank.Models
{
    public class TransactionError : BaseNopModel
    {
        [NopResourceDisplayName("Plugins.Payments.MellatBank.Fields.TerminalId")]
        public int OrderId { get; set; }
        public string ErrorId { get; set; }
        public string ErrorMessage { get; set; }
    }
}
