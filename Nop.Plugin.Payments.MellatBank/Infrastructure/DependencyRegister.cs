using System;
using Autofac;
using Autofac.Core;
using Nop.Core.Configuration;
using Nop.Core.Data;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Orders;
using Nop.Core.Infrastructure;
using Nop.Core.Infrastructure.DependencyManagement;
using Nop.Data;

using Nop.Web.Framework.Mvc;
using Nop.Plugin.Payments.MellatBank.Domain;
using Nop.Plugin.Payments.MellatBank.Controllers;
using Nop.Plugin.Payments.MellatBank.Data;
using Nop.Plugin.Payments.MellatBank.Controllers.Admin;
using Nop.Plugin.Payments.MellatBank.Services;

namespace Nop.Plugin.Payments.MellatBank.Infrastructure
{
    public class DependencyRegister : IDependencyRegistrar
    {
		private const string ObjectContextName = "nop_object_context_Peyment_IR";

        public void Register(ContainerBuilder builder, ITypeFinder typeFinder, NopConfig config)
        {
            this.RegisterPluginDataContext<PeymentObjectContext>(builder, ObjectContextName);

            builder.RegisterType<EfRepository<Transaction>>()
               .As<IRepository<Transaction>>()
               .WithParameter(ResolvedParameter.ForNamed<IDbContext>(ObjectContextName))
               .InstancePerLifetimeScope();

            MappingExtensions.Maps.CreateAllMappings();

            RegisterPluginServices(builder);

            RegisterControllers(builder);

            RegisterModelBinders(builder);
        }

        private void RegisterControllers(ContainerBuilder builder)
        {
            builder.RegisterType<PaymentMellatBankController>().InstancePerLifetimeScope();
            builder.RegisterType<ManageTransactionsAdminController>().InstancePerLifetimeScope();
        }

        private void RegisterModelBinders(ContainerBuilder builder)
        {
            //builder.RegisterGeneric(typeof(ParametersModelBinder<>)).InstancePerLifetimeScope();
            //builder.RegisterGeneric(typeof(JsonModelBinder<>)).InstancePerLifetimeScope();
        }

        private void RegisterPluginServices(ContainerBuilder builder)
        {
            builder.RegisterType<TransactionService>().As<ITransactionService>().InstancePerLifetimeScope();
        }

        public int Order { get; }
    }
}