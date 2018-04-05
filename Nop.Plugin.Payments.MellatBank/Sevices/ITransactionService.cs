using System.Collections.Generic;
using Nop.Plugin.Payments.MellatBank.Domain;
using Nop.Core;

namespace Nop.Plugin.Payments.MellatBank.Services
{
    public interface ITransactionService
    {
        Transaction GetTransaction(long transactionId);
        bool ValidateTransactionById(long transactionId);
        IPagedList<Transaction> GetAllTransactions(int pageIndex = 0, int pageSize = int.MaxValue);
        void DeleteTransaction(Transaction transaction);
        Transaction GetTransactionById(int id);
        Transaction GetTransactionByTransactionId(long transactionId);

        void InsertTransaction(Transaction Transaction);
        void UpdateTransaction(Transaction Transaction);
    }
}