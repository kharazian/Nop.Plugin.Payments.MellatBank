using Nop.Plugin.Payments.MellatBank.AutoMapper;
using Nop.Plugin.Payments.MellatBank.Domain;
using Nop.Plugin.Payments.MellatBank.Models;

namespace Nop.Plugin.Payments.MellatBank.MappingExtensions
{
    public static class TransactionMappings
    {
        public static TransactionModel ToModel(this Transaction transaction)
        {
            return transaction.MapTo<Transaction, TransactionModel>();
        }

        public static Transaction ToEntity(this TransactionModel transactionModel)
        {
            return transactionModel.MapTo<TransactionModel, Transaction>();
        }

        public static Transaction ToEntity(this TransactionModel model, Transaction destination)
        {
            return model.MapTo<TransactionModel, Transaction>(destination);
        }
    }
}