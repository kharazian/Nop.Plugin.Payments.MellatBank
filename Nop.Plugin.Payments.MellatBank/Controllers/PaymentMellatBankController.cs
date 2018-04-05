using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using Nop.Core;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Plugin.Payments.MellatBank.Models;
using Nop.Services.Common;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Orders;
using Nop.Services.Payments;
using Nop.Services.Stores;
using Nop.Web.Framework.Controllers;
using System.ServiceModel;
using Nop.Plugin.Payments.MellatBank.Constants;
using Nop.Plugin.Payments.MellatBank.Services;
using Nop.Plugin.Payments.MellatBank.Domain;

namespace Nop.Plugin.Payments.MellatBank.Controllers
{
    public class PaymentMellatBankController : BasePaymentController
    {
        private readonly IWorkContext _workContext;
        private readonly IStoreService _storeService;
        private readonly ISettingService _settingService;
        private readonly IPaymentService _paymentService;
        private readonly IOrderService _orderService;
        private readonly IOrderProcessingService _orderProcessingService;
        private readonly IGenericAttributeService _genericAttributeService;
        private readonly ILocalizationService _localizationService;
        private readonly IStoreContext _storeContext;
        private readonly ILogger _logger;
        private readonly IWebHelper _webHelper;
        private readonly PaymentSettings _paymentSettings;
        private readonly MellatBankPaymentSettings _mellatBankPaymentSettings;
        private readonly ITransactionService _transactionService;
        private readonly ShoppingCartSettings _shoppingCartSettings;

        public PaymentMellatBankController(IWorkContext workContext,
            IStoreService storeService, 
            ISettingService settingService, 
            IPaymentService paymentService, 
            IOrderService orderService, 
            IOrderProcessingService orderProcessingService,
            IGenericAttributeService genericAttributeService,
            ILocalizationService localizationService,
            IStoreContext storeContext,
            ILogger logger, 
            IWebHelper webHelper,
            PaymentSettings paymentSettings,
            MellatBankPaymentSettings MellatBankPaymentSettings,
            ShoppingCartSettings shoppingCartSettings,
            ITransactionService transactionService)
        {
            this._workContext = workContext;
            this._storeService = storeService;
            this._settingService = settingService;
            this._paymentService = paymentService;
            this._orderService = orderService;
            this._orderProcessingService = orderProcessingService;
            this._genericAttributeService = genericAttributeService;
            this._localizationService = localizationService;
            this._storeContext = storeContext;
            this._logger = logger;
            this._webHelper = webHelper;
            this._paymentSettings = paymentSettings;
            this._mellatBankPaymentSettings = MellatBankPaymentSettings;
            this._shoppingCartSettings = shoppingCartSettings;
            this._transactionService = transactionService;
        }

        [AdminAuthorize]
        [ChildActionOnly]
        public ActionResult Configure()
        {
            //load settings for a chosen store scope
            var storeScope = this.GetActiveStoreScopeConfiguration(_storeService, _workContext);
            var MellatBankPaymentSettings = _settingService.LoadSetting<MellatBankPaymentSettings>(storeScope);

            var model = new ConfigurationModel();
            model.TerminalId = MellatBankPaymentSettings.TerminalId;
            model.UserName = MellatBankPaymentSettings.UserName;
            model.UserPassword = MellatBankPaymentSettings.UserPassword;
            model.BusinessPhoneNumber = MellatBankPaymentSettings.BusinessPhoneNumber;
            model.BusinessEmail = MellatBankPaymentSettings.BusinessEmail;
            model.PdtValidateOrderTotal = MellatBankPaymentSettings.PdtValidateOrderTotal;
            model.AdditionalFee = MellatBankPaymentSettings.AdditionalFee;
            model.AdditionalFeePercentage = MellatBankPaymentSettings.AdditionalFeePercentage;
            model.PassProductNamesAndTotals = MellatBankPaymentSettings.PassProductNamesAndTotals;
            model.ReturnFromMellatBankWithoutPaymentRedirectsToOrderDetailsPage = MellatBankPaymentSettings.ReturnFromMellatBankWithoutPaymentRedirectsToOrderDetailsPage;

            model.ActiveStoreScopeConfiguration = storeScope;
            if (storeScope > 0)
            {
                model.BusinessEmail_OverrideForStore = _settingService.SettingExists(MellatBankPaymentSettings, x => x.BusinessEmail, storeScope);
                model.PdtValidateOrderTotal_OverrideForStore = _settingService.SettingExists(MellatBankPaymentSettings, x => x.PdtValidateOrderTotal, storeScope);
                model.AdditionalFee_OverrideForStore = _settingService.SettingExists(MellatBankPaymentSettings, x => x.AdditionalFee, storeScope);
                model.AdditionalFeePercentage_OverrideForStore = _settingService.SettingExists(MellatBankPaymentSettings, x => x.AdditionalFeePercentage, storeScope);
                model.PassProductNamesAndTotals_OverrideForStore = _settingService.SettingExists(MellatBankPaymentSettings, x => x.PassProductNamesAndTotals, storeScope);
                model.ReturnFromMellatBankWithoutPaymentRedirectsToOrderDetailsPage_OverrideForStore = _settingService.SettingExists(MellatBankPaymentSettings, x => x.ReturnFromMellatBankWithoutPaymentRedirectsToOrderDetailsPage, storeScope);
            }

            return View("~/Plugins/Payments.MellatBank/Views/Configure.cshtml", model);
        }

        [HttpPost]
        [AdminAuthorize]
        [ChildActionOnly]
        public ActionResult Configure(ConfigurationModel model)
        {
            if (!ModelState.IsValid)
                return Configure();

            //load settings for a chosen store scope
            var storeScope = this.GetActiveStoreScopeConfiguration(_storeService, _workContext);
            var MellatBankPaymentSettings = _settingService.LoadSetting<MellatBankPaymentSettings>(storeScope);

            //save settings
            MellatBankPaymentSettings.TerminalId = model.TerminalId;
            MellatBankPaymentSettings.UserName = model.UserName;
            MellatBankPaymentSettings.UserPassword = model.UserPassword;
            MellatBankPaymentSettings.BusinessPhoneNumber = model.BusinessPhoneNumber;
            MellatBankPaymentSettings.BusinessEmail = model.BusinessEmail;
            MellatBankPaymentSettings.PdtValidateOrderTotal = model.PdtValidateOrderTotal;
            MellatBankPaymentSettings.AdditionalFee = model.AdditionalFee;
            MellatBankPaymentSettings.AdditionalFeePercentage = model.AdditionalFeePercentage;
            MellatBankPaymentSettings.PassProductNamesAndTotals = model.PassProductNamesAndTotals;
            MellatBankPaymentSettings.ReturnFromMellatBankWithoutPaymentRedirectsToOrderDetailsPage = model.ReturnFromMellatBankWithoutPaymentRedirectsToOrderDetailsPage;

            /* We do not clear cache after each setting update.
             * This behavior can increase performance because cached settings will not be cleared 
             * and loaded from database after each update */


            _settingService.SaveSettingOverridablePerStore(MellatBankPaymentSettings, x => x.TerminalId, model.TerminalId_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(MellatBankPaymentSettings, x => x.UserName, model.UserName_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(MellatBankPaymentSettings, x => x.UserPassword, model.UserPassword_OverrideForStore, storeScope, false);

            _settingService.SaveSettingOverridablePerStore(MellatBankPaymentSettings, x => x.BusinessPhoneNumber, model.BusinessPhoneNumber_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(MellatBankPaymentSettings, x => x.BusinessEmail, model.BusinessEmail_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(MellatBankPaymentSettings, x => x.PdtValidateOrderTotal, model.PdtValidateOrderTotal_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(MellatBankPaymentSettings, x => x.AdditionalFee, model.AdditionalFee_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(MellatBankPaymentSettings, x => x.AdditionalFeePercentage, model.AdditionalFeePercentage_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(MellatBankPaymentSettings, x => x.PassProductNamesAndTotals, model.PassProductNamesAndTotals_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(MellatBankPaymentSettings, x => x.ReturnFromMellatBankWithoutPaymentRedirectsToOrderDetailsPage, model.ReturnFromMellatBankWithoutPaymentRedirectsToOrderDetailsPage_OverrideForStore, storeScope, false);
            
            //now clear settings cache
            _settingService.ClearCache();

            SuccessNotification(_localizationService.GetResource("Admin.Plugins.Saved"));

            return Configure();
        }

        //action displaying notification (warning) to a store owner about inaccurate MellatBank rounding
        [ValidateInput(false)]
        public ActionResult RoundingWarning(bool passProductNamesAndTotals)
        {
            //prices and total aren't rounded, so display warning
            if (passProductNamesAndTotals && !_shoppingCartSettings.RoundPricesDuringCalculation)
                return Json(new { Result = _localizationService.GetResource("Plugins.Payments.MellatBank.RoundingWarning") }, JsonRequestBehavior.AllowGet);

            return Json(new { Result = string.Empty }, JsonRequestBehavior.AllowGet);
        }

        [ChildActionOnly]
        public ActionResult PaymentInfo()
        {
            return View("~/Plugins/Payments.MellatBank/Views/PaymentInfo.cshtml");
        }

        [NonAction]
        public override IList<string> ValidatePaymentForm(FormCollection form)
        {
            var warnings = new List<string>();
            return warnings;
        }

        [NonAction]
        public override ProcessPaymentRequest GetPaymentInfo(FormCollection form)
        {
            var paymentInfo = new ProcessPaymentRequest();
            return paymentInfo;
        }

        //[ValidateInput(false)]
        //public ActionResult PDTHandler(FormCollection form)
        //{
        //    bool Run_bpReversalRequest = true;

        //    var SaleReferenceId = _webHelper.QueryString<long>("SaleReferenceId");
        //    var SaleOrderId = _webHelper.QueryString<long>("SaleOrderId");
        //    var ResCode = _webHelper.QueryString<string>("ResCode");

        //    //Result Code
        //    string resultCode_bpinquiryRequest = string.Empty;
        //    string resultCode_bpSettleRequest = string.Empty;
        //    string resultCode_bpVerifyRequest = string.Empty;

        //    string response = _webHelper.GetThisPageUrl(true);
            
        //    var processor = _paymentService.LoadPaymentMethodBySystemName("Payments.MellatBank") as MellatBankPaymentProcessor;
        //    if (processor == null ||
        //        !processor.IsPaymentMethodActive(_paymentSettings) || !processor.PluginDescriptor.Installed)
        //        throw new NopException("MellatBank module cannot be loaded");
        //    Dictionary<string, string> values = processor.GetPdtDetails(response);

        //    string orderNumber = string.Empty;
        //    values.TryGetValue("custom", out orderNumber);
        //    Guid orderNumberGuid = Guid.Empty;
        //    try
        //    {
        //        orderNumberGuid = new Guid(orderNumber);
        //    }
        //    catch { }
        //    Order order = _orderService.GetOrderByGuid(orderNumberGuid);
        //    var orderTotalSentToMellatBank = order.GetAttribute<decimal?>("OrderTotalSentToMellatBank");

        //    if (ResCode == "0" && SaleOrderId != 0 && SaleReferenceId != 0)
        //    {
        //        resultCode_bpVerifyRequest = processor.GetPdtVerify(order.Id, SaleOrderId, SaleReferenceId); 

        //        if (string.IsNullOrEmpty(resultCode_bpVerifyRequest))
        //        { 
        //            resultCode_bpinquiryRequest = processor.GetPdtInquiry(order.Id, SaleOrderId, SaleReferenceId); 
        //        }

        //        if (resultCode_bpVerifyRequest == "0" || resultCode_bpinquiryRequest == "0")
        //        {               
        //            resultCode_bpSettleRequest = processor.GetPdtSettle(order.Id, SaleOrderId, SaleReferenceId);
        //            if (resultCode_bpSettleRequest == "0" || resultCode_bpSettleRequest == "45")
        //            {
        //                Run_bpReversalRequest = false;
        //                //TempData["Message"] += " لطفا شماره پیگیری را یادداشت نمایید" + saleReferenceId;
        //            }
        //        }

        //    }
        //    if (Run_bpReversalRequest && order != null && SaleOrderId != 0 && SaleReferenceId != 0) //ReversalRequest
        //    {
        //        processor.GetPdtReversal(order.Id, SaleOrderId, SaleReferenceId);
        //        // Save information to Database...
        //    }

        //    if (Run_bpReversalRequest == false)
        //    {
        //        if (order != null)
        //        {
        //            var sb = new StringBuilder();
        //            sb.AppendLine("MellatBank PDT:");
        //            sb.AppendLine("payment_status: " + resultCode_bpSettleRequest);
        //            sb.AppendLine("payment_status_code: " + ResCode);
        //            var payment_status = "";
        //            var newPaymentStatus = MellatBankHelper.GetPaymentStatus(resultCode_bpSettleRequest, out payment_status);
        //            sb.AppendLine("New payment status: " + newPaymentStatus);

        //            //order note
        //            order.OrderNotes.Add(new OrderNote
        //            {
        //                Note = sb.ToString(),
        //                DisplayToCustomer = false,
        //                CreatedOnUtc = DateTime.UtcNow
        //            });
        //            _orderService.UpdateOrder(order);                    

        //            //validate order total
        //            if (orderTotalSentToMellatBank.HasValue && orderTotalSentToMellatBank != order.OrderTotal)
        //            {
        //                string errorStr = string.Format("MellatBank PDT. Returned order total doesn't equal order total {0}. Order# {1}.", order.OrderTotal, order.Id);
        //                //log
        //                _logger.Error(errorStr);
        //                //order note
        //                order.OrderNotes.Add(new OrderNote
        //                {
        //                    Note = errorStr,
        //                    DisplayToCustomer = false,
        //                    CreatedOnUtc = DateTime.UtcNow
        //                });
        //                _orderService.UpdateOrder(order);

        //                return RedirectToAction("Index", "Home", new { area = "" });
        //            }
        //            //clear attribute
        //            if (orderTotalSentToMellatBank.HasValue)
        //                _genericAttributeService.SaveAttribute<decimal?>(order, "OrderTotalSentToMellatBank", null);

        //            _genericAttributeService.SaveAttribute<long?>(order, "OrderMellatBankSaleReferenceId", SaleReferenceId);
        //            //mark order as paid
        //            if (newPaymentStatus == PaymentStatus.Paid)
        //            {
        //                if (_orderProcessingService.CanMarkOrderAsPaid(order))
        //                {
        //                    order.AuthorizationTransactionId = SaleReferenceId.ToString();
        //                    _orderService.UpdateOrder(order);

        //                    _orderProcessingService.MarkOrderAsPaid(order);
        //                }
        //            }                    
        //        }
        //        return RedirectToRoute("CheckoutCompleted", new { orderId = order.Id});
        //    }
        //    else
        //    {
        //        if (order != null)
        //        {
        //            //order note
        //            order.OrderNotes.Add(new OrderNote
        //            {
        //                Note = "MellatBank PDT failed. " + response,
        //                DisplayToCustomer = false,
        //                CreatedOnUtc = DateTime.UtcNow
        //            });
        //            _orderService.UpdateOrder(order);
        //        }
        //        return RedirectToRoute("Plugin.Payments.MellatBank.CancelOrder");
        //    }
        //}

        public ActionResult CancelOrder(FormCollection form)
        {
            if (_mellatBankPaymentSettings.ReturnFromMellatBankWithoutPaymentRedirectsToOrderDetailsPage)
            {
                var order = _orderService.SearchOrders(storeId: _storeContext.CurrentStore.Id,
                    customerId: _workContext.CurrentCustomer.Id, pageSize: 1)
                    .FirstOrDefault();
                if (order != null)
                {
                    return RedirectToRoute("OrderDetails", new { orderId = order.Id });
                }
            }

            return RedirectToAction("Index", "Home", new { area = "" });
        }

        public ActionResult ShowError(TransactionError tErrParam)
        {
            TransactionError tErr = (TransactionError)TempData["tErr"];
            if (tErr == null)
                tErr = tErrParam;
            return View(ViewNames.ShowError, tErr);
        }

        public ActionResult RedirectVPOS(string refId)
        {
            try
            {
                if (refId == null)
                {
                    TransactionError _tErr = new TransactionError { ErrorMessage = "هیچ شماره پیگیری برای پرداخت از سمت بانک ارسال نشده است!" };
                    TempData["tErr"] = _tErr;
                    return RedirectToAction("ShowError", "PaymentMellatBank");
                }
                else
                {
                    return View("~/Plugins/Payments.MellatBank/Views/RedirectVPOS.cshtml", null, refId);
                }
            }
            catch (Exception error)
            {
                TransactionError _tErr = new TransactionError { ErrorMessage = error + "متاسفانه خطایی رخ داده است، لطفا مجددا عملیات خود را انجام دهید در صورت تکرار این مشکل را به بخش پشتیبانی اطلاع دهید" };
                TempData["tErr"] = _tErr;
                return RedirectToAction("ShowError", "PaymentMellatBank");
            }
        }

        //[HttpPost]
        public ActionResult PDTHandler()
        {
            
            bool Run_bpReversalRequest = false;
            long saleReferenceId = -999;
            long saleOrderId = -999;
            long transactionId = -999;
            string resultCode_bpPayRequest;
            Transaction t;
            int _orderId = -999;
            Order _order = new Order();

            MellatPeyment _mellatPeyment = new MellatPeyment();
            TransactionError _tErr = new TransactionError();

            try
            {
                saleReferenceId = long.Parse(Request.Params["SaleReferenceId"].ToString());
                saleOrderId = long.Parse(Request.Params["SaleOrderId"].ToString());                
                resultCode_bpPayRequest = Request.Params["ResCode"].ToString();
                transactionId = long.Parse(Request.Params["transactionid"].ToString());
                _orderId = int.Parse(Request.Params["orderid"].ToString());
                _order = _orderService.GetOrderById(_orderId);
                //Result Code
                string resultCode_bpinquiryRequest = "-9999";
                string resultCode_bpSettleRequest = "-9999";
                string resultCode_bpVerifyRequest = "-9999";
                t = _transactionService.GetTransaction(transactionId);
                if (int.Parse(resultCode_bpPayRequest) == 0)
                {
                    #region Success

                    resultCode_bpVerifyRequest = _mellatPeyment.VerifyRequest(saleOrderId, saleOrderId, saleReferenceId);

                    if (string.IsNullOrEmpty(resultCode_bpVerifyRequest))
                    {
                        #region Inquiry Request

                        resultCode_bpinquiryRequest = _mellatPeyment.InquiryRequest(saleOrderId, saleOrderId, saleReferenceId);
                        if (int.Parse(resultCode_bpinquiryRequest) != 0)
                        {
                            string msg = string.Empty;
                            _mellatPeyment.GetPaymentStatus(resultCode_bpinquiryRequest, out msg);
                            _tErr.ErrorId = resultCode_bpinquiryRequest;
                            _tErr.ErrorMessage = msg;
                            t.StatusPayment = msg;
                            _transactionService.UpdateTransaction(t);
                            Run_bpReversalRequest = true;
                        }

                        #endregion
                    }

                    if ((int.Parse(resultCode_bpVerifyRequest) == 0)
                        ||
                        (int.Parse(resultCode_bpinquiryRequest) == 0))
                    {

                        #region SettleRequest

                        resultCode_bpSettleRequest = _mellatPeyment.SettleRequest(saleOrderId, saleOrderId, saleReferenceId);
                        if ((int.Parse(resultCode_bpSettleRequest) == 0)
                            || (int.Parse(resultCode_bpSettleRequest) == 0))
                        {
                            _order.PaymentStatus = PaymentStatus.Paid;
                            _order.OrderStatus = OrderStatus.Processing;
                            _orderService.UpdateOrder(_order);
                            string msg = "تراکنش شما با موفقیت انجام شد ";
                            msg += " لطفا شماره پیگیری را یادداشت نمایید" + saleReferenceId;
                            _tErr.ErrorId = "0";
                            _tErr.ErrorMessage = msg;
                            t.StatusPayment = msg;
                            t.TransactionFinished = true;
                            _transactionService.UpdateTransaction(t);
                        }
                        else
                        {
                            string msg = string.Empty;
                            _mellatPeyment.GetPaymentStatus(resultCode_bpinquiryRequest, out msg);
                            _tErr.ErrorId = resultCode_bpinquiryRequest;
                            _tErr.ErrorMessage = msg;
                            t.StatusPayment = msg;
                            _transactionService.UpdateTransaction(t);
                            Run_bpReversalRequest = true;
                        }
                        #endregion
                    }
                    else
                    {
                        string msg = string.Empty;
                        _mellatPeyment.GetPaymentStatus(resultCode_bpinquiryRequest, out msg);
                        _tErr.ErrorId = resultCode_bpinquiryRequest;
                        _tErr.ErrorMessage = msg;
                        t.StatusPayment = msg;
                        _transactionService.UpdateTransaction(t);
                        Run_bpReversalRequest = true;
                    }

                    #endregion
                }
                else
                {
                    string msg = string.Empty;
                    _mellatPeyment.GetPaymentStatus(resultCode_bpinquiryRequest, out msg);
                    _tErr.ErrorId = resultCode_bpinquiryRequest;
                    _tErr.ErrorMessage = msg;
                    t.StatusPayment = msg;
                    _transactionService.UpdateTransaction(t);
                    Run_bpReversalRequest = true;
                }
                TempData["tErr"] = _tErr;
                return RedirectToAction("ShowError", "PaymentMellatBank");
            }
            catch (Exception Error)
            {
                string msg = "متاسفانه خطایی رخ داده است، لطفا مجددا عملیات خود را انجام دهید در صورت تکرار این مشکل را به بخش پشتیبانی اطلاع دهید";
                _tErr.ErrorId = "-100";
                _tErr.ErrorMessage = msg;
                // Save and send Error for admin user
                Run_bpReversalRequest = true;
                TempData["tErr"] = _tErr;
                return RedirectToAction("ShowError", "PaymentMellatBank");
            }
            finally
            {
                if (Run_bpReversalRequest) //ReversalRequest
                {
                    if (saleOrderId != -999 && saleReferenceId != -999)
                        _mellatPeyment.bpReversalRequest(saleOrderId, saleOrderId, saleReferenceId);
                    // Save information to Database...
                }
            }

        }

    }
}