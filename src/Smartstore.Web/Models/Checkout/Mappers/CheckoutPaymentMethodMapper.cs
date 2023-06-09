﻿using Smartstore.ComponentModel;
using Smartstore.Core;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Checkout.Payment;
using Smartstore.Core.Checkout.Shipping;
using Smartstore.Core.Checkout.Tax;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Data;
using Smartstore.Core.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Smartstore.Web.Models.Checkout
{
    public static partial class CheckoutPaymentMethodMappingExtensions
    {
        public static async Task MapAsync(this IEnumerable<OrganizedShoppingCartItem> entity, CheckoutPaymentMethodModel model)
        {
            await MapperFactory.MapAsync(entity, model, null);
        }
    }

    public class CheckoutPaymentMethodMapper : Mapper<IEnumerable<OrganizedShoppingCartItem>, CheckoutPaymentMethodModel>
    {
        private readonly ICommonServices _services;
        private readonly ICurrencyService _currencyService;
        private readonly IPaymentService _paymentService;
        private readonly IShippingService _shippingService;
        private readonly ITaxCalculator _taxCalculator;
        private readonly ShippingSettings _shippingSettings;

        public CheckoutPaymentMethodMapper(
            ICommonServices services,
            IPaymentService paymentService,
            ICurrencyService currencyService,
            IShippingService shippingService,
            ITaxCalculator taxCalculator,
            ShippingSettings shippingSettings)
        {
            _services = services;
            _paymentService = paymentService;
            _currencyService = currencyService;
            _shippingService = shippingService;
            _taxCalculator = taxCalculator;
            _shippingSettings = shippingSettings;
        }

        protected override void Map(IEnumerable<OrganizedShoppingCartItem> from, CheckoutPaymentMethodModel to, dynamic parameters = null)
            => throw new NotImplementedException();

        public override async Task MapAsync(IEnumerable<OrganizedShoppingCartItem> from, CheckoutPaymentMethodModel to, dynamic parameters = null)
        {
            Guard.NotNull(from, nameof(from));
            Guard.NotNull(to, nameof(to));

            var storeId = _services.StoreContext.CurrentStore.Id;
            var customer = _services.WorkContext.CurrentCustomer;

            // Was shipping skipped.
            // TODO: (ms) (core) Wait with implementation until any provider for shipping rate computation has been implemented.
            //var shippingOptions = _shippingService.GetShippingOptions(from.ToList(), customer.ShippingAddress, storeId: storeId).ShippingOptions;

            //if (!from.IsShippingRequired() || shippingOptions.Count <= 1 && _shippingSettings.SkipShippingIfSingleOption)
            //{
            //    to.SkippedSelectShipping = true;
            //}

            //var paymentTypes = new PaymentMethodType[]
            //{
            //    PaymentMethodType.Standard,
            //    PaymentMethodType.Redirection,
            //    PaymentMethodType.StandardAndRedirection
            //};

            //var allPaymentMethods = await _paymentService.GetAllPaymentMethodsAsync(storeId);
            //var boundPaymentMethods = await _paymentService.LoadActivePaymentMethodsAsync(customer, from.ToList(), storeId, paymentTypes);

            //foreach (var boundPaymentMethod in boundPaymentMethods)
            //{
            //    if (from.IncludesMatchingItems(x => x.IsRecurring) && boundPaymentMethod.Value.RecurringPaymentType == RecurringPaymentType.NotSupported)
            //        continue;

            //    var model = new CheckoutPaymentMethodModel.PaymentMethodModel
            //    {
            //        // TODO: (ms) (core) Wait for plugin mediator implementation to retrieve localized payment method names.
            //        //Name = _pluginMediator.GetLocalizedFriendlyName(boundPaymentMethod.Metadata),
            //        //Description = _pluginMediator.GetLocalizedDescription(boundPaymentMethod.Metadata),
            //        PaymentWidgetInvoker = boundPaymentMethod.Value.GetPaymentInfoWidget(),
            //        PaymentMethodSystemName = boundPaymentMethod.Metadata.SystemName,
            //        RequiresInteraction = boundPaymentMethod.Value.RequiresInteraction
            //    };

            //    if (allPaymentMethods.TryGetValue(boundPaymentMethod.Metadata.SystemName, out var paymentMethod))
            //    {
            //        model.FullDescription = paymentMethod.GetLocalized(x => x.FullDescription, _services.WorkContext.WorkingLanguage);
            //    }

            //    // TODO: (ms) (core) Wait for PluginMediator implementation
            //    //model.BrandUrl = _pluginMediator.GetBrandImageUrl(boundPaymentMethod.Metadata);

            //    // Payment method additional fee.
            //    // TODO: (ms) (core) Wait for implementation of any payment service and the respective 'GetAdditionalHandlingFee' method.
            //    //decimal paymentMethodAdditionalFee = _paymentService.GetAdditionalHandlingFee(cart, boundPaymentMethod.Metadata.SystemName);
            //    decimal paymentMethodAdditionalFee = 0m;
            //    decimal rateBase = await _taxCalculator.CalculatePaymentFeeTaxAsync(paymentMethodAdditionalFee);
            //    var rate = _currencyService.ConvertFromPrimaryCurrency(rateBase, _services.WorkContext.WorkingCurrency);

            //    if (rate != decimal.Zero)
            //    {
            //        model.Fee = rate.ToString(true);
            //    }

            //    to.PaymentMethods.Add(model);
            //}

            // Find a (previously) selected payment method.
            var selected = false;
            var selectedPaymentMethodSystemName = customer.GenericAttributes.SelectedPaymentMethod;
            if (selectedPaymentMethodSystemName.HasValue())
            {
                var paymentMethodToSelect = to.PaymentMethods.Find(pm => pm.PaymentMethodSystemName.EqualsNoCase(selectedPaymentMethodSystemName));
                if (paymentMethodToSelect != null)
                {
                    paymentMethodToSelect.Selected = true;
                    selected = true;
                }
            }

            // If no option has been selected, just try selecting the first one.
            if (!selected)
            {
                var paymentMethodToSelect = to.PaymentMethods.FirstOrDefault();
                if (paymentMethodToSelect != null)
                {
                    paymentMethodToSelect.Selected = true;
                }
            }
        }
    }
}
