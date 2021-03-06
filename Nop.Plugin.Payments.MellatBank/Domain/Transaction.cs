﻿using Nop.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nop.Plugin.Payments.MellatBank.Domain
{
    public class Transaction : BaseEntity
    {
        public long TransactionId { get; set; }
        public string ReferenceNumber { get; set; }
        public long SaleReferenceId { get; set; }
        public string StatusPayment { get; set; }
        public bool TransactionFinished { get; set; }
        public long Amount { get; set; }
        public string BankName { get; set; }
        public int UserID { get; set; }
        public System.DateTime BuyDatetime { get; set; }
    }
}
