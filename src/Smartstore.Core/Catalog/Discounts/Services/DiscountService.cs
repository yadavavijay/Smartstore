﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Smartstore.Caching;
using Smartstore.Collections;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Checkout.Rules;
using Smartstore.Core.Data;
using Smartstore.Core.Identity;
using Smartstore.Core.Stores;
using Smartstore.Data.Hooks;

namespace Smartstore.Core.Catalog.Discounts
{
    public partial class DiscountService : AsyncDbSaveHook<Discount>, IDiscountService
    {
        // {0} = includeHidden, {1} = couponCode.
        private const string DISCOUNTS_ALL_KEY = "discount.all-{0}-{1}";
        internal const string DISCOUNTS_PATTERN_KEY = "discount.*";

        private readonly SmartDbContext _db;
        private readonly IRequestCache _requestCache;
        private readonly IStoreContext _storeContext;
        private readonly ICartRuleProvider _cartRuleProvider;
        private readonly Lazy<IShoppingCartService> _cartService;
        private readonly Dictionary<DiscountKey, bool> _discountValidityCache = new();
        private readonly Multimap<string, int> _relatedEntityIds = new(items => new HashSet<int>(items));

        public DiscountService(
            SmartDbContext db,
            IRequestCache requestCache,
            IStoreContext storeContext,
            ICartRuleProvider cartRuleProvider,
            Lazy<IShoppingCartService> cartService)
        {
            _db = db;
            _requestCache = requestCache;
            _storeContext = storeContext;
            _cartRuleProvider = cartRuleProvider;
            _cartService = cartService;
        }

        #region Hook

        protected override Task<HookResult> OnInsertingAsync(Discount entity, IHookedEntity entry, CancellationToken cancelToken)
            => Task.FromResult(HookResult.Ok);

        protected override Task<HookResult> OnUpdatingAsync(Discount entity, IHookedEntity entry, CancellationToken cancelToken)
        {
            if (entry.IsPropertyModified(nameof(Discount.DiscountType)))
            {
                KeepRelatedEntityIds(entity);
            }

            return Task.FromResult(HookResult.Ok);
        }

        protected override Task<HookResult> OnDeletingAsync(Discount entity, IHookedEntity entry, CancellationToken cancelToken)
        {
            KeepRelatedEntityIds(entity);
            return Task.FromResult(HookResult.Ok);
        }

        public override async Task OnAfterSaveCompletedAsync(IEnumerable<IHookedEntity> entries, CancellationToken cancelToken)
        {
            if (_relatedEntityIds.Any())
            {
                await UpdateHasDiscountsAppliedProperty(_db.Products, _relatedEntityIds["product"], cancelToken);
                await UpdateHasDiscountsAppliedProperty(_db.Categories, _relatedEntityIds["category"], cancelToken);
                await UpdateHasDiscountsAppliedProperty(_db.Manufacturers, _relatedEntityIds["manufactuter"], cancelToken);

                _relatedEntityIds.Clear();
            }

            _requestCache.RemoveByPattern(DISCOUNTS_PATTERN_KEY);
        }

        private void KeepRelatedEntityIds(Discount entity)
        {
            _relatedEntityIds.AddRange("product", entity.AppliedToProducts.Select(x => x.Id));
            _relatedEntityIds.AddRange("category", entity.AppliedToCategories.Select(x => x.Id));
            _relatedEntityIds.AddRange("manufacturer", entity.AppliedToManufacturers.Select(x => x.Id));
        }

        private async Task UpdateHasDiscountsAppliedProperty<TEntity>(DbSet<TEntity> dbSet, IEnumerable<int> ids, CancellationToken cancelToken = default)
            where TEntity : EntityWithDiscounts
        {
            var allIds = ids.ToArray();

            foreach (var idsChunk in allIds.Slice(100))
            {
                var entities = await dbSet
                    .Where(x => idsChunk.Contains(x.Id))
                    .ToListAsync(cancelToken);

                foreach (var entity in entities.OfType<EntityWithDiscounts>())
                {
                    var isLoaded = _db.IsCollectionLoaded(entity, x => x.AppliedDiscounts, out var collectionEntry);
                    var hasDiscounts = isLoaded
                        ? entity.AppliedDiscounts.Any()
                        : await collectionEntry.Query().AnyAsync(cancelToken);

                    entity.HasDiscountsApplied = hasDiscounts;
                }

                await _db.SaveChangesAsync(cancelToken);
            }
        }

        #endregion

        public virtual async Task<IEnumerable<Discount>> GetAllDiscountsAsync(DiscountType? discountType, string couponCode = null, bool includeHidden = false)
        {
            couponCode = couponCode.EmptyNull();

            var discountTypeId = discountType.HasValue ? (int)discountType.Value : 0;

            // We load all discounts and filter them by passed "discountType" parameter later because
            // this method is invoked several times per HTTP request with distinct "discountType" parameter.
            var cacheKey = DISCOUNTS_ALL_KEY.FormatInvariant(includeHidden, couponCode);

            var result = await _requestCache.GetAsync(cacheKey, async () =>
            {
                var query = _db.Discounts.AsNoTracking();

                if (!includeHidden)
                {
                    var utcNow = DateTime.UtcNow;

                    query = query.Where(x =>
                        (!x.StartDateUtc.HasValue || x.StartDateUtc <= utcNow) &&
                        (!x.EndDateUtc.HasValue || x.EndDateUtc >= utcNow));
                }

                if (couponCode.HasValue())
                {
                    query = query.Where(x => x.CouponCode == couponCode);
                }

                var discounts = await query
                    .OrderByDescending(d => d.Id)
                    .ToListAsync();

                var map = discounts.ToMultimap(x => x.DiscountTypeId, x => x);
                return map;
            });

            if (discountTypeId > 0)
            {
                return result[discountTypeId];
            }

            return result.SelectMany(x => x.Value);
        }

        public virtual async Task<bool> IsDiscountValidAsync(Discount discount, Customer customer, string couponCodeToValidate, Store store = null)
        {
            Guard.NotNull(discount, nameof(discount));

            store ??= _storeContext.CurrentStore;

            var cacheKey = new DiscountKey(discount, customer, couponCodeToValidate, store);
            if (_discountValidityCache.TryGetValue(cacheKey, out var result))
            {
                return result;
            }

            // Check coupon code.
            if (discount.RequiresCouponCode)
            {
                if (discount.CouponCode.IsEmpty() || !discount.CouponCode.EqualsNoCase(couponCodeToValidate))
                {
                    return Cached(false);
                }
            }

            // Check date range.
            var now = DateTime.UtcNow;
            if (discount.StartDateUtc.HasValue)
            {
                var startDate = DateTime.SpecifyKind(discount.StartDateUtc.Value, DateTimeKind.Utc);
                if (startDate.CompareTo(now) > 0)
                {
                    return Cached(false);
                }
            }
            if (discount.EndDateUtc.HasValue)
            {
                var endDate = DateTime.SpecifyKind(discount.EndDateUtc.Value, DateTimeKind.Utc);
                if (endDate.CompareTo(now) < 0)
                {
                    return Cached(false);
                }
            }

            if (!await CheckDiscountLimitationsAsync(discount, customer))
            {
                return Cached(false);
            }

            // Better not to apply discounts if there are gift cards in the cart cause the customer could "earn" money through that.
            if (discount.DiscountType == DiscountType.AssignedToOrderTotal || discount.DiscountType == DiscountType.AssignedToOrderSubTotal)
            {
                var cart =  await _cartService.Value.GetCartItemsAsync(customer, ShoppingCartType.ShoppingCart, store.Id);
                if (cart.Any(x => x.Item?.Product != null && x.Item.Product.IsGiftCard))
                {
                    return Cached(false);
                }
            }

            // Rulesets.
            if (!await _cartRuleProvider.RuleMatchesAsync(discount))
            {
                return Cached(false);
            }

            return Cached(true);

            bool Cached(bool value)
            {
                _discountValidityCache[cacheKey] = value;
                return value;
            }
        }

        protected virtual async Task<bool> CheckDiscountLimitationsAsync(Discount discount, Customer customer)
        {
            Guard.NotNull(discount, nameof(discount));

            switch (discount.DiscountLimitation)
            {
                case DiscountLimitationType.NTimesOnly:
                    {
                        var count = await _db.DiscountUsageHistory
                            .ApplyStandardFilter(discount.Id)
                            .CountAsync();
                        return count < discount.LimitationTimes;
                    }

                case DiscountLimitationType.NTimesPerCustomer:
                    if (customer != null && !customer.IsGuest())
                    {
                        // Registered customer.
                        var count = await _db.DiscountUsageHistory
                            .Include(x => x.Order)
                            .ApplyStandardFilter(discount.Id, customer.Id)
                            .CountAsync();
                        return count < discount.LimitationTimes;
                    }
                    else
                    {
                        // Guest.
                        return true;
                    }

                case DiscountLimitationType.Unlimited:
                    return true;

                default:
                    return false;
            }
        }

        class DiscountKey : Tuple<Discount, Customer, string, Store>
        {
            public DiscountKey(Discount discount, Customer customer, string customerCouponCode, Store store)
                : base(discount, customer, customerCouponCode, store)
            {
            }
        }
    }
}
