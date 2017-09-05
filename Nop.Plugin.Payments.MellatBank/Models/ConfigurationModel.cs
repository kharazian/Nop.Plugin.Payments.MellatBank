using Nop.Web.Framework;
using Nop.Web.Framework.Mvc;

namespace Nop.Plugin.Payments.MellatBank.Models
{
    public class ConfigurationModel : BaseNopModel
    {
        public int ActiveStoreScopeConfiguration { get; set; }

        [NopResourceDisplayName("Plugins.Payments.MellatBank.Fields.TerminalId")]
        public bool TerminalId { get; set; }
        public bool TerminalId_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payments.MellatBank.Fields.UserName")]
        public string UserName { get; set; }
        public bool UserName_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payments.MellatBank.Fields.UserPassword")]
        public string UserPassword { get; set; }
        public bool UserPassword_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payments.MellatBank.Fields.BusinessPhoneNumber")]
        public string BusinessPhoneNumber { get; set; }
        public bool BusinessPhoneNumber_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payments.MellatBank.Fields.BusinessEmail")]
        public string BusinessEmail { get; set; }
        public bool BusinessEmail_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payments.MellatBank.Fields.PDTValidateOrderTotal")]
        public bool PdtValidateOrderTotal { get; set; }
        public bool PdtValidateOrderTotal_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payments.MellatBank.Fields.AdditionalFee")]
        public decimal AdditionalFee { get; set; }
        public bool AdditionalFee_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payments.MellatBank.Fields.AdditionalFeePercentage")]
        public bool AdditionalFeePercentage { get; set; }
        public bool AdditionalFeePercentage_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payments.MellatBank.Fields.PassProductNamesAndTotals")]
        public bool PassProductNamesAndTotals { get; set; }
        public bool PassProductNamesAndTotals_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payments.MellatBank.Fields.ReturnFromMellatBankWithoutPaymentRedirectsToOrderDetailsPage")]
        public bool ReturnFromMellatBankWithoutPaymentRedirectsToOrderDetailsPage { get; set; }
        public bool ReturnFromMellatBankWithoutPaymentRedirectsToOrderDetailsPage_OverrideForStore { get; set; }
    }
}