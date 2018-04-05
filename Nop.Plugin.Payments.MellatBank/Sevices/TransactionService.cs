using System;
using System.Collections.Generic;
using System.Linq;
using Nop.Core.Data;
using Nop.Plugin.Payments.MellatBank.Domain;
using Nop.Core;

namespace Nop.Plugin.Payments.MellatBank.Services
{
    public class TransactionService : ITransactionService
    {
        private readonly IRepository<Transaction> _transactionRepository;
        public TransactionService(IRepository<Transaction> transactionRepository)
        {
            _transactionRepository = transactionRepository;
        }

        public bool ValidateTransaction(long transactionId, string transactionSecret, string authenticationCode)
        {
            return _transactionRepository.Table.Any(transaction => transaction.TransactionId == transactionId);
        }

        public Transaction GetTransaction(long transactionId)
        {
            return _transactionRepository.Table.FirstOrDefault(transaction => transaction.TransactionId == transactionId);
        }

        public bool ValidateTransactionById(long transactionId)
        {
            return _transactionRepository.Table.Any(transaction => transaction.TransactionId == transactionId);
        }

        public IPagedList<Transaction> GetAllTransactions(int pageIndex = 0, int pageSize = int.MaxValue)
        {
            var query = _transactionRepository.Table;

            query = query.OrderByDescending(al => al.BuyDatetime);
        
            var transaction = new PagedList<Transaction>(query, pageIndex, pageSize);
            return transaction;
        }

        public Transaction GetTransactionById(int id)
        {
            return _transactionRepository.GetById(id);
        }

        public Transaction GetTransactionByTransactionId(long transactionId)
        {
            return _transactionRepository.Table.FirstOrDefault(transaction => transaction.TransactionId == transactionId);
        }

        public void InsertTransaction(Transaction transaction)
        {
            if (transaction == null)
            {
                throw new ArgumentNullException("transaction");
            }

            _transactionRepository.Insert(transaction);
        }

        public void UpdateTransaction(Transaction transaction)
        {
            if (transaction == null)
            {
                throw new ArgumentNullException("transaction");
            }

            _transactionRepository.Update(transaction);
        }

        public void DeleteTransaction(Transaction transaction)
        {
            if (transaction == null)
                throw new ArgumentNullException("transaction");

            _transactionRepository.Delete(transaction);
        }
    }
}