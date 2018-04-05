using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Web;
using System.Web.Routing;
using Nop.Core;
using Nop.Core.Domain.Directory;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Core.Domain.Shipping;
using Nop.Core.Plugins;
using Nop.Plugin.Payments.MellatBank.Controllers;
using Nop.Services.Common;
using Nop.Services.Configuration;
using Nop.Services.Directory;
using Nop.Services.Localization;
using Nop.Services.Orders;
using Nop.Services.Payments;
using Nop.Services.Tax;
using System.ServiceModel;
using Nop.Plugin.Payments.MellatBank.wsMellatBank;
using Nop.Services.Events;
using Nop.Plugin.Payments.MellatBank.Models;
using Nop.Plugin.Payments.MellatBank.Domain;
using Nop.Web.Framework.Menu;
using Nop.Plugin.Payments.MellatBank.Services;

namespace Nop.Plugin.Payments.MellatBank
{
    /// <summary>
    /// MellatBank payment processor
    /// </summary>
    public class MellatBankPaymentProcessor : BasePlugin, IPaymentMethod, IAdminMenuPlugin
    {
        #region Constants

        /// <summary>
        /// nopCommerce partner code
        /// </summary>
        private const string BN_CODE = "nopCommerce_SP";

        #endregion

        #region Fields

        private readonly CurrencySettings _currencySettings;
        private readonly HttpContextBase _httpContext;
        private readonly ICheckoutAttributeParser _checkoutAttributeParser;
        private readonly ICurrencyService _currencyService;
        private readonly IGenericAttributeService _genericAttributeService;
        private readonly ILocalizationService _localizationService;
        private readonly IOrderTotalCalculationService _orderTotalCalculationService;
        private readonly ISettingService _settingService;
        private readonly ITaxService _taxService;
        private readonly IWebHelper _webHelper;
        private readonly MellatBankPaymentSettings _mellatBankPaymentSettings;
        private readonly IStoreContext _storeContext;
        private readonly IEventPublisher _eventPublisher;
        private readonly MellatPeyment _mellatPeyment;
        private readonly IWorkContext _workContext;
        private readonly ITransactionService _transactionService;


        #endregion

        #region Ctor

        public MellatBankPaymentProcessor(CurrencySettings currencySettings,
            HttpContextBase httpContext,
            ICheckoutAttributeParser checkoutAttributeParser,
            ICurrencyService currencyService,
            IGenericAttributeService genericAttributeService,
            ILocalizationService localizationService,
            IOrderTotalCalculationService orderTotalCalculationService,
            ISettingService settingService,
            ITaxService taxService,
            IWebHelper webHelper,
            MellatBankPaymentSettings MellatBankPaymentSettings,
            IStoreContext storeContext,
            IEventPublisher eventPublisher,
            IWorkContext workContext,
            ITransactionService transactionService)
        {
            this._currencySettings = currencySettings;
            this._httpContext = httpContext;
            this._checkoutAttributeParser = checkoutAttributeParser;
            this._currencyService = currencyService;
            this._genericAttributeService = genericAttributeService;
            this._localizationService = localizationService;
            this._orderTotalCalculationService = orderTotalCalculationService;
            this._settingService = settingService;
            this._taxService = taxService;
            this._webHelper = webHelper;
            this._mellatBankPaymentSettings = MellatBankPaymentSettings;
            this._storeContext = storeContext;
            this._eventPublisher = eventPublisher;
            _mellatPeyment = new MellatPeyment();
            this._workContext = workContext;
            _transactionService = transactionService;
        }

        #endregion

        #region Utilities




        /// <summary>
        /// Gets PDT details
        /// </summary>
        /// <param name="tx">TX</param>
        /// <param name="values">Values</param>
        /// <param name="response">Response</param>
        /// <returns>Result</returns>
        public Dictionary<string, string> GetPdtDetails(string response)
        {
            bool firstLine = true;

            Dictionary<string, string> values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (string l in response.Split('&'))
            {
                string line = l.Trim();
                if (firstLine)
                {
                    firstLine = false;
                }
                else
                {
                    int equalPox = line.IndexOf('=');
                    if (equalPox >= 0)
                        values.Add(line.Substring(0, equalPox), line.Substring(equalPox + 1));
                }
            }
            return values;
        }

        /// <summary>
        /// Generate string (URL) for redirection
        /// </summary>
        /// <param name="postProcessPaymentRequest">Payment info required for an order processing</param>
        /// <param name="passProductNamesAndTotals">A value indicating whether to pass product names and totals</param>
        private string GenerationCallBackUrl(PostProcessPaymentRequest postProcessPaymentRequest, bool passProductNamesAndTotals, long transactionid)
        {
            var builder = new StringBuilder();
            builder.Append(_webHelper.GetStoreLocation(false) + "Plugins/PaymentMellatBank/PDTHandler");
            var cmd = passProductNamesAndTotals
                ? "_cart"
                : "_xclick";
            builder.AppendFormat("?cmd={0}", cmd);
            builder.AppendFormat("&transactionid={0}", transactionid);
            builder.AppendFormat("&orderid={0}", postProcessPaymentRequest.Order.Id);
            if (passProductNamesAndTotals)
            {
                builder.AppendFormat("&upload=1");

                //get the items in the cart
                decimal cartTotal = decimal.Zero;
                var cartTotalRounded = decimal.Zero;
                var cartItems = postProcessPaymentRequest.Order.OrderItems;
                int x = 1;
                foreach (var item in cartItems)
                {
                    var unitPriceExclTax = item.UnitPriceExclTax;
                    var priceExclTax = item.PriceExclTax;
                    //round
                    var unitPriceExclTaxRounded = Math.Round(unitPriceExclTax, 2);
                    builder.AppendFormat("&item_name_" + x + "={0}", HttpUtility.UrlEncode(item.Product.Name));
                    builder.AppendFormat("&amount_" + x + "={0}", unitPriceExclTaxRounded.ToString("0.00", CultureInfo.InvariantCulture));
                    builder.AppendFormat("&quantity_" + x + "={0}", item.Quantity);
                    x++;
                    cartTotal += priceExclTax;
                    cartTotalRounded += unitPriceExclTaxRounded * item.Quantity;
                }

                //the checkout attributes that have a cost value and send them to MellatBank as items to be paid for
                var attributeValues = _checkoutAttributeParser.ParseCheckoutAttributeValues(postProcessPaymentRequest.Order.CheckoutAttributesXml);
                foreach (var val in attributeValues)
                {
                    var attPrice = _taxService.GetCheckoutAttributePrice(val, false, postProcessPaymentRequest.Order.Customer);
                    //round
                    var attPriceRounded = Math.Round(attPrice, 2);
                    if (attPrice > decimal.Zero) //if it has a price
                    {
                        var attribute = val.CheckoutAttribute;
                        if (attribute != null)
                        {
                            var attName = attribute.Name; //set the name
                            builder.AppendFormat("&item_name_" + x + "={0}", HttpUtility.UrlEncode(attName)); //name
                            builder.AppendFormat("&amount_" + x + "={0}", attPriceRounded.ToString("0.00", CultureInfo.InvariantCulture)); //amount
                            builder.AppendFormat("&quantity_" + x + "={0}", 1); //quantity
                            x++;
                            cartTotal += attPrice;
                            cartTotalRounded += attPriceRounded;
                        }
                    }
                }

                //order totals

                //shipping
                var orderShippingExclTax = postProcessPaymentRequest.Order.OrderShippingExclTax;
                var orderShippingExclTaxRounded = Math.Round(orderShippingExclTax, 2);
                if (orderShippingExclTax > decimal.Zero)
                {
                    builder.AppendFormat("&item_name_" + x + "={0}", "Shipping fee");
                    builder.AppendFormat("&amount_" + x + "={0}", orderShippingExclTaxRounded.ToString("0.00", CultureInfo.InvariantCulture));
                    builder.AppendFormat("&quantity_" + x + "={0}", 1);
                    x++;
                    cartTotal += orderShippingExclTax;
                    cartTotalRounded += orderShippingExclTaxRounded;
                }

                //payment method additional fee
                var paymentMethodAdditionalFeeExclTax = postProcessPaymentRequest.Order.PaymentMethodAdditionalFeeExclTax;
                var paymentMethodAdditionalFeeExclTaxRounded = Math.Round(paymentMethodAdditionalFeeExclTax, 2);
                if (paymentMethodAdditionalFeeExclTax > decimal.Zero)
                {
                    builder.AppendFormat("&item_name_" + x + "={0}", "Payment method fee");
                    builder.AppendFormat("&amount_" + x + "={0}", paymentMethodAdditionalFeeExclTaxRounded.ToString("0.00", CultureInfo.InvariantCulture));
                    builder.AppendFormat("&quantity_" + x + "={0}", 1);
                    x++;
                    cartTotal += paymentMethodAdditionalFeeExclTax;
                    cartTotalRounded += paymentMethodAdditionalFeeExclTaxRounded;
                }

                //tax
                var orderTax = postProcessPaymentRequest.Order.OrderTax;
                var orderTaxRounded = Math.Round(orderTax, 2);
                if (orderTax > decimal.Zero)
                {
                    //builder.AppendFormat("&tax_1={0}", orderTax.ToString("0.00", CultureInfo.InvariantCulture));

                    //add tax as item
                    builder.AppendFormat("&item_name_" + x + "={0}", HttpUtility.UrlEncode("Sales Tax")); //name
                    builder.AppendFormat("&amount_" + x + "={0}", orderTaxRounded.ToString("0.00", CultureInfo.InvariantCulture)); //amount
                    builder.AppendFormat("&quantity_" + x + "={0}", 1); //quantity

                    cartTotal += orderTax;
                    cartTotalRounded += orderTaxRounded;
                    x++;
                }

                if (cartTotal > postProcessPaymentRequest.Order.OrderTotal)
                {
                    /* Take the difference between what the order total is and what it should be and use that as the "discount".
                     * The difference equals the amount of the gift card and/or reward points used. 
                     */
                    decimal discountTotal = cartTotal - postProcessPaymentRequest.Order.OrderTotal;
                    discountTotal = Math.Round(discountTotal, 2);
                    cartTotalRounded -= discountTotal;
                    //gift card or rewared point amount applied to cart in nopCommerce - shows in MellatBank as "discount"
                    builder.AppendFormat("&discount_amount_cart={0}", discountTotal.ToString("0.00", CultureInfo.InvariantCulture));
                }

                //save order total that actually sent to MellatBank (used for PDT order total validation)
                _genericAttributeService.SaveAttribute(postProcessPaymentRequest.Order, "OrderMellatBankSaleReferenceId", cartTotalRounded);
            }
            else
            {
                //pass order total
                builder.AppendFormat("&item_name=Order Number {0}", postProcessPaymentRequest.Order.Id);
                var orderTotal = Math.Round(postProcessPaymentRequest.Order.OrderTotal, 2);
                builder.AppendFormat("&amount={0}", orderTotal.ToString("0.00", CultureInfo.InvariantCulture));

                //save order total that actually sent to MellatBank (used for PDT order total validation)
                _genericAttributeService.SaveAttribute(postProcessPaymentRequest.Order, "OrderMellatBankSaleReferenceId", orderTotal);
            }

            builder.AppendFormat("&custom={0}", postProcessPaymentRequest.Order.OrderGuid);
            builder.AppendFormat("&charset={0}", "utf-8");
            builder.AppendFormat("&bn={0}", BN_CODE);
            builder.Append(string.Format("&no_note=1&currency_code={0}", HttpUtility.UrlEncode(_currencyService.GetCurrencyById(_currencySettings.PrimaryStoreCurrencyId).CurrencyCode)));
            builder.AppendFormat("&invoice={0}", postProcessPaymentRequest.Order.Id);
            builder.AppendFormat("&rm=2", new object[0]);
            if (postProcessPaymentRequest.Order.ShippingStatus != ShippingStatus.ShippingNotRequired)
                builder.AppendFormat("&no_shipping=2", new object[0]);
            else
                builder.AppendFormat("&no_shipping=1", new object[0]);

            return builder.ToString();
        }

        #endregion

        #region Methods

        /// <summary>
        /// Process a payment
        /// </summary>
        /// <param name="processPaymentRequest">Payment info required for an order processing</param>
        /// <returns>Process payment result</returns>
        public ProcessPaymentResult ProcessPayment(ProcessPaymentRequest processPaymentRequest)
        {
            var result = new ProcessPaymentResult();
            result.NewPaymentStatus = PaymentStatus.Pending;
            return result;
        }

        /// <summary>
        /// Post process payment (used by payment gateways that require redirecting to a third-party URL)
        /// </summary>
        /// <param name="postProcessPaymentRequest">Payment info required for an order processing</param>
        public void PostProcessPayment(PostProcessPaymentRequest postProcessPaymentRequest)
        {
            Transaction t = new Transaction
            {
                TransactionId = Int64.Parse(DateTime.UtcNow.ToString("yyyyMMddhhmmssfff")),
                Amount = Convert.ToInt64(postProcessPaymentRequest.Order.OrderTotal),
                StatusPayment = "-100",
                BankName = "Mellat",
                SaleReferenceId = 0,
                BuyDatetime = System.DateTime.Now,
                UserID = 1,
                TransactionFinished = false,
            };
            _transactionService.InsertTransaction(t);
            var callBackRedirect = GenerationCallBackUrl(postProcessPaymentRequest, _mellatBankPaymentSettings.PassProductNamesAndTotals, t.TransactionId);
            if (callBackRedirect == null)
                throw new Exception("MellatBank URL cannot be generated");
            //ensure URL doesn't exceed 2K chars. Otherwise, customers can get "too long URL" exception
            if (callBackRedirect.Length > 2048)
                callBackRedirect = GenerationCallBackUrl(postProcessPaymentRequest, false, t.TransactionId);


            string StatusSendRequest = string.Empty;
            string Status = string.Empty;
            string RefID = string.Empty;
            StatusSendRequest = _mellatPeyment.bpPayRequest(t.TransactionId, t.Amount, _storeContext.CurrentStore.Name, callBackRedirect);
            var returnValues = StatusSendRequest.Split(',');
            if (returnValues.Length > 0)
            {
                Status = returnValues[0];
            }
            if (returnValues.Length > 1)
            {
                RefID = returnValues[1];
            }
            string msg = string.Empty;
            var reStatus = _mellatPeyment.GetPaymentStatus(Status, out msg);
            if (reStatus == PaymentStatus.Authorized)
            {
                t.StatusPayment = Status;
                t.ReferenceNumber = RefID;
                _transactionService.UpdateTransaction(t);
                _httpContext.Response.RedirectToRoute("Plugin.Payments.MellatBank.RedirectVPOS", new { refID = RefID });
            }
            else
            {
                t.StatusPayment = msg;
                _transactionService.UpdateTransaction(t);
                TransactionError tErr = new TransactionError { OrderId = postProcessPaymentRequest.Order.Id, ErrorId = Status, ErrorMessage = msg};
                _httpContext.Response.RedirectToRoute("Plugin.Payments.MellatBank.ShowError", tErr);
            }

        }

        /// <summary>
        /// Returns a value indicating whether payment method should be hidden during checkout
        /// </summary>
        /// <param name="cart">Shoping cart</param>
        /// <returns>true - hide; false - display.</returns>
        public bool HidePaymentMethod(IList<ShoppingCartItem> cart)
        {
            //you can put any logic here
            //for example, hide this payment method if all products in the cart are downloadable
            //or hide this payment method if current customer is from certain country
            return false;
        }

        /// <summary>
        /// Gets additional handling fee
        /// </summary>
        /// <param name="cart">Shoping cart</param>
        /// <returns>Additional handling fee</returns>
        public decimal GetAdditionalHandlingFee(IList<ShoppingCartItem> cart)
        {
            var result = this.CalculateAdditionalFee(_orderTotalCalculationService, cart,
                _mellatBankPaymentSettings.AdditionalFee, _mellatBankPaymentSettings.AdditionalFeePercentage);
            return result;
        }

        /// <summary>
        /// Captures payment
        /// </summary>
        /// <param name="capturePaymentRequest">Capture payment request</param>
        /// <returns>Capture payment result</returns>
        public CapturePaymentResult Capture(CapturePaymentRequest capturePaymentRequest)
        {
            var result = new CapturePaymentResult();
            result.AddError("Capture method not supported");
            return result;
        }

        /// <summary>
        /// Refunds a payment
        /// </summary>
        /// <param name="refundPaymentRequest">Request</param>
        /// <returns>Result</returns>
        public RefundPaymentResult Refund(RefundPaymentRequest refundPaymentRequest)
        {
            var result = new RefundPaymentResult();
            result.AddError("Refund method not supported");
            return result;
        }

        /// <summary>
        /// Voids a payment
        /// </summary>
        /// <param name="voidPaymentRequest">Request</param>
        /// <returns>Result</returns>
        public VoidPaymentResult Void(VoidPaymentRequest voidPaymentRequest)
        {
            var result = new VoidPaymentResult();
            result.AddError("Void method not supported");
            return result;
        }

        /// <summary>
        /// Process recurring payment
        /// </summary>
        /// <param name="processPaymentRequest">Payment info required for an order processing</param>
        /// <returns>Process payment result</returns>
        public ProcessPaymentResult ProcessRecurringPayment(ProcessPaymentRequest processPaymentRequest)
        {
            var result = new ProcessPaymentResult();
            result.AddError("Recurring payment not supported");
            return result;
        }

        /// <summary>
        /// Cancels a recurring payment
        /// </summary>
        /// <param name="cancelPaymentRequest">Request</param>
        /// <returns>Result</returns>
        public CancelRecurringPaymentResult CancelRecurringPayment(CancelRecurringPaymentRequest cancelPaymentRequest)
        {
            var result = new CancelRecurringPaymentResult();
            result.AddError("Recurring payment not supported");
            return result;
        }

        /// <summary>
        /// Gets a value indicating whether customers can complete a payment after order is placed but not completed (for redirection payment methods)
        /// </summary>
        /// <param name="order">Order</param>
        /// <returns>Result</returns>
        public bool CanRePostProcessPayment(Order order)
        {
            if (order == null)
                throw new ArgumentNullException("order");
            
            //let's ensure that at least 5 seconds passed after order is placed
            //P.S. there's no any particular reason for that. we just do it
            if ((DateTime.UtcNow - order.CreatedOnUtc).TotalSeconds < 5)
                return false;

            return true;
        }

        /// <summary>
        /// Gets a route for provider configuration
        /// </summary>
        /// <param name="actionName">Action name</param>
        /// <param name="controllerName">Controller name</param>
        /// <param name="routeValues">Route values</param>
        public void GetConfigurationRoute(out string actionName, out string controllerName, out RouteValueDictionary routeValues)
        {
            actionName = "Configure";
            controllerName = "PaymentMellatBank";
            routeValues = new RouteValueDictionary { { "Namespaces", "Nop.Plugin.Payments.MellatBank.Controllers" }, { "area", null } };
        }

        /// <summary>
        /// Gets a route for payment info
        /// </summary>
        /// <param name="actionName">Action name</param>
        /// <param name="controllerName">Controller name</param>
        /// <param name="routeValues">Route values</param>
        public void GetPaymentInfoRoute(out string actionName, out string controllerName, out RouteValueDictionary routeValues)
        {
            actionName = "PaymentInfo";
            controllerName = "PaymentMellatBank";
            routeValues = new RouteValueDictionary { { "Namespaces", "Nop.Plugin.Payments.MellatBank.Controllers" }, { "area", null } };
        }

        /// <summary>
        /// Get type of controller
        /// </summary>
        /// <returns>Type</returns>
        public Type GetControllerType()
        {
            return typeof(PaymentMellatBankController);
        }

        /// <summary>
        /// Install the plugin
        /// </summary>
        public override void Install()
        {
            //settings
            var settings = new MellatBankPaymentSettings
            {
                TerminalId = 0,
                UserName="UserName",
                UserPassword = "Password",
                BusinessEmail = "test@test.com",
                PdtValidateOrderTotal = true,
            };
            _settingService.SaveSetting(settings);

            //locales
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.MellatBank.Fields.AdditionalFee", "هزینه های مازاد");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.MellatBank.Fields.AdditionalFee.Hint", "مبلغ هزینه مازاد جهت درج در فاکتور مشتری.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.MellatBank.Fields.TerminalId", "شماره درگاه بانک ملت");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.MellatBank.Fields.TerminalId.Hint", "فعال کردن شماره درگاه بانک ملت");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.MellatBank.Fields.UserName", "نام کاربری درگاه بانک ملت");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.MellatBank.Fields.UserName.Hint", "فعال کردن نام کاربری درگاه بانک ملت");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.MellatBank.Fields.UserPassword", "رمز درگاه بانک ملت");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.MellatBank.Fields.UserPassword.Hint", "فعال کردن رمز درگاه بانک ملت");

            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.MellatBank.Fields.BusinessPhoneNumber", "شماره تلفن فروشگاه");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.MellatBank.Fields.BusinessPhoneNumber.Hint", "فعال کردن شماره تلفن فروشگاه");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.MellatBank.Fields.AdditionalFeePercentage", "هزینه مازاد بر اساس درصد");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.MellatBank.Fields.AdditionalFeePercentage.Hint", "آیا درصد هزینه مازاد برای کل فاکتور حساب شود؟ اگر این گزینه تیک نخورد هزینه مازاد بر اساس مقدار ثابت محاسبه خواهد شد.");

            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.MellatBank.Fields.BusinessEmail", "پست الکترونیک");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.MellatBank.Fields.BusinessEmail.Hint", "استفاده از پست الکترونیک اختصاصی.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.MellatBank.Fields.PassProductNamesAndTotals", "ارسال نام و مبلغ کالا برای بانک ملت");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.MellatBank.Fields.PassProductNamesAndTotals.Hint", "فعال کردن ارسال نام و مبلغ کالا برای بانک ملت");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.MellatBank.Fields.PDTValidateOrderTotal", "بررسی کالاها در زمان تایید پرداخت");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.MellatBank.Fields.PDTValidateOrderTotal.Hint", "فعال کردن بررسی کالاها در زمان تایید پرداخت");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.MellatBank.Fields.RedirectionTip", "شما برای نهایی کردن خرید و پرداخت فاکتور خود به سایت بانک ملت منتقل خواهید شد.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.MellatBank.Fields.ReturnFromMellatBankWithoutPaymentRedirectsToOrderDetailsPage", "برگشت به صفحه خرید");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.MellatBank.Fields.ReturnFromMellatBankWithoutPaymentRedirectsToOrderDetailsPage.Hint", "فعال کردن برگشت به صفحه خرید در صورت کلیک بر روی لینگ \"برگشت به صفحه خرید\"");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.MellatBank.Instructions", "<p><b>در صورت استفاده از این افزونه خواهشمند است شرایط استفاده از بانک ملت را مطالعه فرمایید.</b><br /><br />برای استفاده از این افزونه باید شماره حساب بانک ملت دریافت نمایید:<br /><br />1. وارد اکانت بانک ملت شوید (اینجا <a href=\"https://www.MellatBank.com/us/webapps/mpp/referral/MellatBank-business-account2?partner_id=9JJPJNNPQ7PZ8\" target=\"_blank\">ثبت نام</a> ).<br /></p>");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.MellatBank.PaymentMethodDescription", "برای نهایی کردن خرید و پرداخت فاکتور به سایت بانک ملت منتقل خواهید شد.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.MellatBank.RoundingWarning", "گرد کردن مبلغ فاکتور.");

            base.Install();
        }

        /// <summary>
        /// Uninstall the plugin
        /// </summary>
        public override void Uninstall()
        {
            //settings
            _settingService.DeleteSetting<MellatBankPaymentSettings>();

            //locales
            this.DeletePluginLocaleResource("Plugins.Payments.MellatBank.Fields.AdditionalFee");
            this.DeletePluginLocaleResource("Plugins.Payments.MellatBank.Fields.AdditionalFee.Hint");
            this.DeletePluginLocaleResource("Plugins.Payments.MellatBank.Fields.TerminalId");
            this.DeletePluginLocaleResource("Plugins.Payments.MellatBank.Fields.TerminalId.Hint");
            this.DeletePluginLocaleResource("Plugins.Payments.MellatBank.Fields.UserName");
            this.DeletePluginLocaleResource("Plugins.Payments.MellatBank.Fields.UserName.Hint");
            this.DeletePluginLocaleResource("Plugins.Payments.MellatBank.Fields.UserPassword");
            this.DeletePluginLocaleResource("Plugins.Payments.MellatBank.Fields.UserPassword.Hint");
            this.DeletePluginLocaleResource("Plugins.Payments.MellatBank.Fields.BusinessPhoneNumber");
            this.DeletePluginLocaleResource("Plugins.Payments.MellatBank.Fields.BusinessPhoneNumber.Hint");
            this.DeletePluginLocaleResource("Plugins.Payments.MellatBank.Fields.AdditionalFeePercentage");
            this.DeletePluginLocaleResource("Plugins.Payments.MellatBank.Fields.AdditionalFeePercentage.Hint");
            this.DeletePluginLocaleResource("Plugins.Payments.MellatBank.Fields.BusinessEmail");
            this.DeletePluginLocaleResource("Plugins.Payments.MellatBank.Fields.BusinessEmail.Hint");
            this.DeletePluginLocaleResource("Plugins.Payments.MellatBank.Fields.PassProductNamesAndTotals");
            this.DeletePluginLocaleResource("Plugins.Payments.MellatBank.Fields.PassProductNamesAndTotals.Hint");
            this.DeletePluginLocaleResource("Plugins.Payments.MellatBank.Fields.PDTValidateOrderTotal");
            this.DeletePluginLocaleResource("Plugins.Payments.MellatBank.Fields.PDTValidateOrderTotal.Hint");
            this.DeletePluginLocaleResource("Plugins.Payments.MellatBank.Fields.RedirectionTip");
            this.DeletePluginLocaleResource("Plugins.Payments.MellatBank.Fields.ReturnFromMellatBankWithoutPaymentRedirectsToOrderDetailsPage");
            this.DeletePluginLocaleResource("Plugins.Payments.MellatBank.Fields.ReturnFromMellatBankWithoutPaymentRedirectsToOrderDetailsPage.Hint");
            this.DeletePluginLocaleResource("Plugins.Payments.MellatBank.Instructions");
            this.DeletePluginLocaleResource("Plugins.Payments.MellatBank.PaymentMethodDescription");
            this.DeletePluginLocaleResource("Plugins.Payments.MellatBank.RoundingWarning");

            base.Uninstall();
        }

        public void ManageSiteMap(Web.Framework.Menu.SiteMapNode rootNode)
        {
            string pluginMenuName = _localizationService.GetResource("Plugin.Payments.MellatBank.Admin.Menu.Title", languageId: _workContext.WorkingLanguage.Id, defaultValue: "Payment IR");

            string settingsMenuName = _localizationService.GetResource("Plugin.Payments.MellatBank.Admin.Menu.Settings.Title", languageId: _workContext.WorkingLanguage.Id, defaultValue: "Settings");

            string manageTransactionMenuName = _localizationService.GetResource("Plugin.Payments.MellatBank.Admin.Menu.Trabsaction.Title", languageId: _workContext.WorkingLanguage.Id, defaultValue: "Transaction");

            const string adminUrlPart = "Plugins/";

            var pluginMainMenu = new Web.Framework.Menu.SiteMapNode
            {
                Title = pluginMenuName,
                Visible = true,
                SystemName = "Payments.MellatBank-Main-Menu",
                IconClass = "fa-genderless"
            };

            //pluginMainMenu.ChildNodes.Add(new Web.Framework.Menu.SiteMapNode
            //{
            //    Title = settingsMenuName,
            //    Url = _webHelper.GetStoreLocation() + adminUrlPart + "PaymentIR/Settings",
            //    Visible = true,
            //    SystemName = "Payments.MellatBank-Settings-Menu",
            //    IconClass = "fa-genderless"
            //});

            pluginMainMenu.ChildNodes.Add(new Web.Framework.Menu.SiteMapNode
            {
                Title = manageTransactionMenuName,
                Url = _webHelper.GetStoreLocation() + adminUrlPart + "PaymentIR/List",
                Visible = true,
                SystemName = "Payments.MellatBank-Transaction-Menu",
                IconClass = "fa-genderless"
            });


            //string pluginDocumentationUrl = "https://github.com/SevenSpikes/api-plugin-for-nopcommerce";

            //pluginMainMenu.ChildNodes.Add(new Web.Framework.Menu.SiteMapNode
            //{
            //    Title = _localizationService.GetResource("Plugins.Api.Admin.Menu.Docs.Title"),
            //    Url = pluginDocumentationUrl,
            //    Visible = true,
            //    SystemName = "Api-Docs-Menu",
            //    IconClass = "fa-genderless"
            //});//TODO: target="_blank"


            rootNode.ChildNodes.Add(pluginMainMenu);
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets a value indicating whether capture is supported
        /// </summary>
        public bool SupportCapture
        {
            get { return false; }
        }

        /// <summary>
        /// Gets a value indicating whether partial refund is supported
        /// </summary>
        public bool SupportPartiallyRefund
        {
            get { return false; }
        }

        /// <summary>
        /// Gets a value indicating whether refund is supported
        /// </summary>
        public bool SupportRefund
        {
            get { return false; }
        }

        /// <summary>
        /// Gets a value indicating whether void is supported
        /// </summary>
        public bool SupportVoid
        {
            get { return false; }
        }

        /// <summary>
        /// Gets a recurring payment type of payment method
        /// </summary>
        public RecurringPaymentType RecurringPaymentType
        {
            get { return RecurringPaymentType.NotSupported; }
        }

        /// <summary>
        /// Gets a payment method type
        /// </summary>
        public PaymentMethodType PaymentMethodType
        {
            get { return PaymentMethodType.Redirection; }
        }

        /// <summary>
        /// Gets a value indicating whether we should display a payment information page for this plugin
        /// </summary>
        public bool SkipPaymentInfo
        {
            get { return false; }
        }

        /// <summary>
        /// Gets a payment method description that will be displayed on checkout pages in the public store
        /// </summary>
        public string PaymentMethodDescription
        {
            //return description of this payment method to be display on "payment method" checkout step. good practice is to make it localizable
            //for example, for a redirection payment method, description may be like this: "You will be redirected to MellatBank site to complete the payment"
            get { return _localizationService.GetResource("Plugins.Payments.MellatBank.PaymentMethodDescription"); }
        }

        #endregion
    }
}
