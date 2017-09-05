using Nop.Core.Configuration;

namespace Nop.Plugin.Payments.MellatBank
{
    public class MellatBankPaymentSettings : ISettings
    {
        public long TerminalId { get; set; }
        public string UserName { get; set; }
        public string UserPassword { get; set; }
        public string BusinessPhoneNumber { get; set; }
        public string BusinessEmail { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether to "additional fee" is specified as percentage. true - percentage, false - fixed value.
        /// </summary>
        public bool AdditionalFeePercentage { get; set; }
        /// <summary>
        /// Additional fee
        /// </summary>
        public decimal AdditionalFee { get; set; }
        public bool PassProductNamesAndTotals { get; set; }
        public bool PdtValidateOrderTotal { get; set; }
        public bool ReturnFromMellatBankWithoutPaymentRedirectsToOrderDetailsPage { get; set; }
    }
}
