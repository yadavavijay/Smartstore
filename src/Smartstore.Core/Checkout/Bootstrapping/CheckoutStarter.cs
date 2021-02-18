﻿using System.Linq;
using Autofac;
using Smartstore.Core.Checkout.Attributes;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Checkout.GiftCards;
using Smartstore.Core.Checkout.Payment;
using Smartstore.Core.Checkout.Payment.Rules;
using Smartstore.Core.Checkout.Rules;
using Smartstore.Core.Checkout.Shipping;
using Smartstore.Core.Checkout.Shipping.Rules;
using Smartstore.Core.Checkout.Tax;
using Smartstore.Core.Rules;
using Smartstore.Core.Rules.Rendering;
using Smartstore.Engine;
using Smartstore.Engine.Builders;

namespace Smartstore.Core.Bootstrapping
{
    public sealed class CheckoutStarter : StarterBase
    {
        public override void ConfigureContainer(ContainerBuilder builder, IApplicationContext appContext, bool isActiveModule)
        {
            builder.RegisterType<CheckoutAttributeMaterializer>().As<ICheckoutAttributeMaterializer>().InstancePerLifetimeScope();
            builder.RegisterType<CheckoutAttributeFormatter>().As<ICheckoutAttributeFormatter>().InstancePerLifetimeScope();
            builder.RegisterType<ShoppingCartValidator>().As<IShoppingCartValidator>().InstancePerLifetimeScope();
            builder.RegisterType<ShoppingCartService>().As<IShoppingCartService>().InstancePerLifetimeScope();
            builder.RegisterType<GiftCardService>().As<IGiftCardService>().InstancePerLifetimeScope();
            builder.RegisterType<ShippingService>().As<IShippingService>().InstancePerLifetimeScope();
            builder.RegisterType<TaxService>().As<ITaxService>().InstancePerLifetimeScope();

            // Cart rules.
            var cartRuleTypes = appContext.TypeScanner.FindTypes<IRule>(ignoreInactiveModules: true).ToList();
            foreach (var ruleType in cartRuleTypes)
            {
                builder.RegisterType(ruleType).Keyed<IRule>(ruleType).InstancePerLifetimeScope();
            }

            builder.RegisterType<CartRuleProvider>()
                .As<ICartRuleProvider>()
                .Keyed<IRuleProvider>(RuleScope.Cart)
                .InstancePerLifetimeScope();

            // Rule options provider.
            builder.RegisterType<PaymentMethodRuleOptionsProvider>().As<IRuleOptionsProvider>().InstancePerLifetimeScope();
            builder.RegisterType<ShippingRateComputationMethodRuleOptionsProvider>().As<IRuleOptionsProvider>().InstancePerLifetimeScope();
            builder.RegisterType<ShippingMethodRuleOptionsProvider>().As<IRuleOptionsProvider>().InstancePerLifetimeScope();
        }
    }
}