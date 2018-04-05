using System.Collections.Generic;
using AutoMapper;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Orders;
using System.Linq;
using System.Web;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Directory;
using Nop.Core.Domain.Localization;
using Nop.Core.Domain.Stores;

using Nop.Plugin.Payments.MellatBank.Domain;
using Nop.Plugin.Payments.MellatBank.Models;
using Nop.Plugin.Payments.MellatBank.AutoMapper;

namespace Nop.Plugin.Payments.MellatBank.MappingExtensions
{
    public static class Maps
    {
        public static IMappingExpression<TSource, TDestination> CreateMap<TSource, TDestination>()
        {
            return AutoMapperPeymentConfiguration.MapperConfigurationExpression.CreateMap<TSource, TDestination>().IgnoreAllNonExisting();
        }

        public static void CreateOrderEntityToOrderDtoMap()
        {
            AutoMapperPeymentConfiguration.MapperConfigurationExpression.CreateMap<Transaction, TransactionModel>()
                .IgnoreAllNonExisting();
        }
        

        public static void CreateAllMappings()
        {
            CreateMap<TransactionModel, Transaction>();

            CreateOrderEntityToOrderDtoMap();
        }
    }
}