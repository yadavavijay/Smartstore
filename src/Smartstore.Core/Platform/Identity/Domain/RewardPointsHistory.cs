﻿using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Domain;

namespace Smartstore.Core.Identity
{
    internal class RewardPointsHistoryMap : IEntityTypeConfiguration<RewardPointsHistory>
    {
        public void Configure(EntityTypeBuilder<RewardPointsHistory> builder)
        {
            //builder.HasOne(c => c.Customer)
            //    .WithMany(c => c.RewardPointsHistory)
            //    .HasForeignKey(c => c.CustomerId)
            //    .IsRequired(false)
            //    .OnDelete(DeleteBehavior.Cascade);

            //builder.HasOne(c => c.UsedWithOrder)
            //    .WithOne(c => c.RedeemedRewardPointsEntry)
            //    .HasForeignKey<RewardPointsHistory>(c => c.UsedWithOrderId)
            //    .OnDelete(DeleteBehavior.SetNull);
        }
    }

    /// <summary>
    /// Represents a reward points history entry.
    /// </summary>
    public class RewardPointsHistory : BaseEntity
    {
        public RewardPointsHistory()
        {
        }

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private member.", Justification = "Required for EF lazy loading")]
        private RewardPointsHistory(ILazyLoader lazyLoader)
            : base(lazyLoader)
        {            
        }

        /// <summary>
        /// Gets or sets the customer identifier.
        /// </summary>
        public int CustomerId { get; set; }

        private Customer _customer;
        /// <summary>
        /// Gets or sets the customer.
        /// </summary>
        public Customer Customer
        {
            get => _customer ?? LazyLoader.Load(this, ref _customer);
            set => _customer = value;
        }

        /// <summary>
        /// Gets or sets the redeemed/added points.
        /// </summary>
        public int Points { get; set; }

        /// <summary>
        /// Gets or sets the points balance.
        /// </summary>
        public int PointsBalance { get; set; }

        /// <summary>
        /// Gets or sets the used amount.
        /// </summary>
        public decimal UsedAmount { get; set; }

        /// <summary>
        /// Gets or sets the message.
        /// </summary>
        [StringLength(4000)]
        public string Message { get; set; }

        /// <summary>
        /// Gets or sets the date of instance creation.
        /// </summary>
        public DateTime CreatedOnUtc { get; set; }

        [Column("UsedWithOrder_Id")]
        public int? UsedWithOrderId { get; set; }

        /// <summary>
        /// Gets or sets the order for which points were redeemed as a payment.
        /// </summary>
        public Order UsedWithOrder { get; set; }
    }
}
