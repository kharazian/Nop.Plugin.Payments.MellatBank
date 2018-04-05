using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using Nop.Plugin.Payments.MellatBank.wsMellatBank;
using Nop.Core.Domain.Payments;

namespace Nop.Plugin.Payments.MellatBank
{
    public class MellatPeyment
    {

        #region Base Variable Definition

        public static readonly string requestSite = "https://bpm.shaparak.ir/pgwchannel/services/pgw";
        public static readonly string PgwSite = "https://bpm.shaparak.ir/pgwchannel/startpay.mellat";

        static readonly string callBackUrl = "http://www.Ably.ir/Payment/BankCallback";
        static readonly long terminalId = long.Parse("111111");
        static readonly string userName = "Ably.ir";
        static readonly string password = "11111111";

        string localDate = string.Empty;
        string localTime = string.Empty;
        #endregion

        public MellatPeyment()
        {
            try
            {
                localDate = DateTime.Now.ToString("yyyyMMdd");
                localTime = DateTime.Now.ToString("HHMMSS");
            }
            catch (Exception error)
            {
                throw new Exception(error.Message);
            }
        }

        public string bpPayRequest(long orderId, long priceAmount, string additionalText, string callBackUrl)
        {
            string StatusRe = string.Empty;
            try
            {
                StatusRe = GetMellatBankService().bpPayRequest(terminalId, userName, password, orderId, priceAmount, localDate, localTime, additionalText, callBackUrl, 0);
            }
            catch (Exception error)
            {
                throw new Exception(error.Message);
            }
            return StatusRe;
        }

        public string VerifyRequest(long orderId, long saleOrderId, long saleReferenceId)
        {
            string StatusRe = string.Empty;
            try
            {
                StatusRe = GetMellatBankService().bpVerifyRequest(terminalId, userName, password, orderId, saleOrderId, saleReferenceId);
            }
            catch (Exception Error)
            {
                throw new Exception(Error.Message);
            }
            return StatusRe;
        }

        public string InquiryRequest(long orderId, long saleOrderId, long saleReferenceId)
        {
            string StatusRe = string.Empty;
            try
            {
                StatusRe = GetMellatBankService().bpInquiryRequest(terminalId, userName, password, orderId, saleOrderId, saleReferenceId);
            }
            catch (Exception Error)
            {
                throw new Exception(Error.Message);
            }
            return StatusRe;
        }


        public string SettleRequest(long orderId, long saleOrderId, long saleReferenceId)
        {
            string StatusRe = string.Empty;
            try
            {
                StatusRe = GetMellatBankService().bpSettleRequest(terminalId, userName, password, orderId, saleOrderId, saleReferenceId);
            }
            catch (Exception Error)
            {
                throw new Exception(Error.Message);
            }
            return StatusRe;
        }

        public string bpReversalRequest(long orderId, long saleOrderId, long saleReferenceId)
        {
            string StatusRe = string.Empty;
            try
            {
                StatusRe = GetMellatBankService().bpReversalRequest(terminalId, userName, password, orderId, saleOrderId, saleReferenceId);
            }
            catch (Exception error)
            {
                throw new Exception(error.Message); ;
            }
            return StatusRe;
        }

        private PaymentGatewayClient GetMellatBankService()
        {
            System.Net.ServicePointManager.Expect100Continue = false;
            BasicHttpsBinding binding = new BasicHttpsBinding();
            EndpointAddress endpoint = new EndpointAddress(new Uri(requestSite));

            return new wsMellatBank.PaymentGatewayClient(binding, endpoint);
        }

        public PaymentStatus GetPaymentStatus(string Status, out string Msg)
        {
            var result = PaymentStatus.Pending;
            Msg = string.Empty;
            if (Status == null)
                Status = string.Empty;

            switch (Status.ToLowerInvariant())
            {
                case "0":
                    result = PaymentStatus.Authorized;
                    Msg = "ﺗﺮاﻛﻨﺶ_ﺑﺎ_ﻣﻮﻓﻘﻴﺖ_اﻧﺠﺎم_ﺷﺪ";
                    break;
                case "11":
                    Msg = "ﺷﻤﺎره_ﻛﺎرت_ﻧﺎﻣﻌﺘﺒﺮ_اﺳﺖ";
                    break;
                case "12":
                    Msg = "ﻣﻮﺟﻮدی_ﻛﺎﻓﻲ_ﻧﻴﺴﺖ";
                    break;
                case "13":
                    Msg = "رﻣﺰ_ﻧﺎدرﺳﺖ_اﺳﺖ";
                    break;
                case "14":
                    Msg = "ﺗﻌﺪاد_دﻓﻌﺎت_وارد_ﻛﺮدن_رﻣﺰ_ﺑﻴﺶ_از_ﺣﺪ_ﻣﺠﺎز_اﺳﺖ";
                    break;
                case "15":
                    Msg = "ﻛﺎرت_ﻧﺎﻣﻌﺘﺒﺮ_اﺳﺖ";
                    break;
                case "16":
                    Msg = "دﻓﻌﺎت_ﺑﺮداﺷﺖ_وﺟﻪ_ﺑﻴﺶ_از_ﺣﺪ_ﻣﺠﺎز_اﺳﺖ";
                    break;
                case "17":
                    Msg = "ﻛﺎرﺑﺮ_از_اﻧﺠﺎم_ﺗﺮاﻛﻨﺶ_ﻣﻨﺼﺮف_ﺷﺪه_اﺳﺖ";
                    break;
                case "18":
                    Msg = "ﺗﺎرﻳﺦ_اﻧﻘﻀﺎی_ﻛﺎرت_ﮔﺬﺷﺘﻪ_اﺳﺖ";
                    break;
                case "19":
                    Msg = "ﻣﺒﻠﻎ_ﺑﺮداﺷﺖ_وﺟﻪ_ﺑﻴﺶ_از_ﺣﺪ_ﻣﺠﺎز_اﺳﺖ";
                    break;
                case "111":
                    Msg = "ﺻﺎدر_ﻛﻨﻨﺪه_ﻛﺎرت_ﻧﺎﻣﻌﺘﺒﺮ_اﺳﺖ";
                    break;
                case "112":
                    Msg = "ﺧﻄﺎی_ﺳﻮﻳﻴﭻ_ﺻﺎدر_ﻛﻨﻨﺪه_ﻛﺎرت";
                    break;
                case "113":
                    Msg = "ﭘﺎﺳﺨﻲ_از_ﺻﺎدر_ﻛﻨﻨﺪه_ﻛﺎرت_درﻳﺎﻓﺖ_ﻧﺸﺪ";
                    break;
                case "114":
                    Msg = "دارﻧﺪه_ﻛﺎرت_ﻣﺠﺎز_ﺑﻪ_اﻧﺠﺎم_اﻳﻦ_ﺗﺮاﻛﻨﺶ_ﻧﻴﺴﺖ";
                    break;
                case "21":
                    Msg = "ﭘﺬﻳﺮﻧﺪه_ﻧﺎﻣﻌﺘﺒﺮ_اﺳﺖ";
                    break;
                case "23":
                    Msg = "ﺧﻄﺎی_اﻣﻨﻴﺘﻲ_رخ_داده_اﺳﺖ";
                    break;
                case "24":
                    Msg = "اﻃﻼﻋﺎت_ﻛﺎرﺑﺮی_ﭘﺬﻳﺮﻧﺪه_ﻧﺎﻣﻌﺘﺒﺮ_اﺳﺖ";
                    break;
                case "25":
                    Msg = "ﻣﺒﻠﻎ_ﻧﺎﻣﻌﺘﺒﺮ_اﺳﺖ";
                    break;
                case "31":
                    Msg = "ﭘﺎﺳﺦ_ﻧﺎﻣﻌﺘﺒﺮ_اﺳﺖ";
                    break;
                case "32":
                    Msg = "ﻓﺮﻣﺖ_اﻃﻼﻋﺎت_وارد_ﺷﺪه_ﺻﺤﻴﺢ_ﻧﻤﻲ_ﺑﺎﺷﺪ";
                    break;
                case "33":
                    Msg = "ﺣﺴﺎب_ﻧﺎﻣﻌﺘﺒﺮ_اﺳﺖ";
                    break;
                case "34":
                    Msg = "ﺧﻄﺎی_ﺳﻴﺴﺘﻤﻲ";
                    break;
                case "35":
                    Msg = "ﺗﺎرﻳﺦ_ﻧﺎﻣﻌﺘﺒﺮ_اﺳﺖ";
                    break;
                case "41":
                    Msg = "ﺷﻤﺎره_درﺧﻮاﺳﺖ_ﺗﻜﺮاری_اﺳﺖ";
                    break;
                case "42":
                    Msg = "ﺗﺮاﻛﻨﺶ_Sale_یافت_نشد_";
                    break;
                case "43":
                    Msg = "ﻗﺒﻼ_Verify_درﺧﻮاﺳﺖ_داده_ﺷﺪه_اﺳﺖ";
                    break;
                case "44":
                    Msg = "درخواست_verify_یافت_نشد";
                    break;
                case "45":
                    Msg = "ﺗﺮاﻛﻨﺶ_Settle_ﺷﺪه_اﺳﺖ";
                    break;
                case "46":
                    Msg = "ﺗﺮاﻛﻨﺶ_Settle_نشده_اﺳﺖ";
                    break;
                case "47":
                    Msg = "ﺗﺮاﻛﻨﺶ_Settle_یافت_نشد";
                    break;
                case "48":
                    Msg = "تراکنش_Reverse_شده_است";
                    break;
                case "49":
                    Msg = "تراکنش_Refund_یافت_نشد";
                    break;
                case "412":
                    Msg = "شناسه_قبض_نادرست_است";
                    break;
                case "413":
                    Msg = "ﺷﻨﺎﺳﻪ_ﭘﺮداﺧﺖ_ﻧﺎدرﺳﺖ_اﺳﺖ";
                    break;
                case "414":
                    Msg = "سازﻣﺎن_ﺻﺎدر_ﻛﻨﻨﺪه_ﻗﺒﺾ_ﻧﺎﻣﻌﺘﺒﺮ_اﺳﺖ";
                    break;
                case "415":
                    Msg = "زﻣﺎن_ﺟﻠﺴﻪ_ﻛﺎری_ﺑﻪ_ﭘﺎﻳﺎن_رسیده_است";
                    break;
                case "416":
                    Msg = "ﺧﻄﺎ_در_ﺛﺒﺖ_اﻃﻼﻋﺎت";
                    break;
                case "417":
                    Msg = "ﺷﻨﺎﺳﻪ_ﭘﺮداﺧﺖ_ﻛﻨﻨﺪه_ﻧﺎﻣﻌﺘﺒﺮ_اﺳﺖ";
                    break;
                case "418":
                    Msg = "اﺷﻜﺎل_در_ﺗﻌﺮﻳﻒ_اﻃﻼﻋﺎت_ﻣﺸﺘﺮی";
                    break;
                case "419":
                    Msg = "ﺗﻌﺪاد_دﻓﻌﺎت_ورود_اﻃﻼﻋﺎت_از_ﺣﺪ_ﻣﺠﺎز_ﮔﺬﺷﺘﻪ_اﺳﺖ";
                    break;
                case "421":
                    Msg = "IP_نامعتبر_است";
                    break;
                case "51":
                    Msg = "ﺗﺮاﻛﻨﺶ_ﺗﻜﺮاری_اﺳﺖ";
                    break;
                case "54":
                    Msg = "ﺗﺮاﻛﻨﺶ_ﻣﺮﺟﻊ_ﻣﻮﺟﻮد_ﻧﻴﺴﺖ";
                    break;
                case "55":
                    Msg = "ﺗﺮاﻛﻨﺶ_ﻧﺎﻣﻌﺘﺒﺮ_اﺳﺖ";
                    break;
                case "61":
                    Msg = "ﺧﻄﺎ_در_واریز";
                    break;
                default:
                    Msg = "خطای ناشناخته رخ داده است";
                    break;
            }
            return result;
        }

    }

}
