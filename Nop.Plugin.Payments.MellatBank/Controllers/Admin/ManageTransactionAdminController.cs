using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Nop.Admin.Controllers;
using Nop.Plugin.Payments.MellatBank.Domain;
using Nop.Plugin.Payments.MellatBank.Models;
using Nop.Services.Localization;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Kendoui;
using Nop.Plugin.Payments.MellatBank.Services;
using Nop.Plugin.Payments.MellatBank.Constants;
using Nop.Plugin.Payments.MellatBank.MappingExtensions;
using Nop.Services.Helpers;

namespace Nop.Plugin.Payments.MellatBank.Controllers.Admin
{
    [AdminAuthorize]
    public class ManageTransactionsAdminController : BaseAdminController
    {
        private readonly ITransactionService _transactionService;
        private readonly ILocalizationService _localizationService;
        private readonly IDateTimeHelper _dateTimeHelper;

        public ManageTransactionsAdminController(ITransactionService transactionService,
            ILocalizationService localizationService, IDateTimeHelper dateTimeHelper)
        {
            _transactionService = transactionService;
            _localizationService = localizationService;
            _dateTimeHelper = dateTimeHelper;
        }

        [HttpGet]
        public ActionResult List()
        {
            return View(ViewNames.AdminTransactionList);
        }

        [HttpPost]
        public ActionResult List(DataSourceRequest command)
        {
            var transactions = _transactionService.GetAllTransactions(command.Page - 1, command.PageSize);
            var gridModel = new DataSourceResult
            {
                Data = transactions.Select(x =>
                {
                    var m = x.ToModel();
                    m.BuyDatetime = _dateTimeHelper.ConvertToUserTime(x.BuyDatetime, DateTimeKind.Utc);
                    return m;

                }),
                Total = transactions.TotalCount
            };
            return Json(gridModel);

        }

        public ActionResult Create()
        {
            TransactionModel transactionModel = PrepareTransactionModel();

            return View();
        }

        [HttpPost, ParameterBasedOnFormName("save-continue", "continueEditing")]
        public ActionResult Create(TransactionModel model, bool continueEditing)
        {
            if (ModelState.IsValid)
            {
                Transaction transaction = null;//;model.ToEntity();

                _transactionService.InsertTransaction(transaction);

                SuccessNotification(_localizationService.GetResource("Plugins.Api.Admin.Transaction.Created"));
                return continueEditing ? RedirectToAction("Edit", new { id = transaction.Id }) : RedirectToAction("List");
            }

            return RedirectToAction("List");
        }

        public ActionResult Edit(int id)
        {
            Transaction transaction = _transactionService.GetTransactionById(id);

            var transactionModel = new TransactionModel();

            //if (transaction != null)
            //{
            //    transactionModel = transaction.ToModel();
            //}

            return View();// ViewNames.AdminApiTransactionsEdit, transactionModel);
        }

        [HttpPost, ParameterBasedOnFormName("save-continue", "continueEditing")]
        public ActionResult Edit(TransactionModel model, bool continueEditing)
        {
            if (ModelState.IsValid)
            {
                Transaction editedTransaction = null;//_transactionService.GetTransactionById(model.Id);

                //editedTransaction = model.ToEntity(editedTransaction);

                _transactionService.UpdateTransaction(editedTransaction);

                SuccessNotification(_localizationService.GetResource("Plugins.Api.Admin.Transaction.Edit"));
                return continueEditing ? RedirectToAction("Edit", new { id = editedTransaction.Id }) : RedirectToAction("List");
            }

            return RedirectToAction("List");
        }

        public ActionResult DeleteTransaction(int id, DataSourceRequest command)
        {
            Transaction transaction = _transactionService.GetTransactionById(id);
            if (transaction == null)
                throw new ArgumentException("No transaction found with the specified id");

            _transactionService.DeleteTransaction(transaction);

            return List(command);
        }

        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {
            Transaction transaction = _transactionService.GetTransactionById(id);
            _transactionService.DeleteTransaction(transaction);

            SuccessNotification(_localizationService.GetResource("Plugins.Api.Admin.Transaction.Deleted"));
            return RedirectToAction("List");
        }
        
        private IList<TransactionModel> PrepareListModel()
        {
            IList<Transaction> transactions = _transactionService.GetAllTransactions();

            var transactionModels = new List<TransactionModel>();

            foreach (var transaction in transactions)
            {
                transactionModels.Add(transaction.ToModel());
            }

            return transactionModels;
        }

        private TransactionModel PrepareTransactionModel()
        {
            var transactionModel = new TransactionModel()
            {
                //TransactionSecret = Guid.NewGuid().ToString(),
                //IsActive = true
            };

            return transactionModel;
        }
    }
}