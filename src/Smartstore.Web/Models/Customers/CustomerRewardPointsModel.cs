﻿using System;
using System.Collections.Generic;
using Smartstore.Core.Common;
using Smartstore.Web.Modelling;

namespace Smartstore.Web.Models.Customers
{
    public partial class CustomerRewardPointsModel : ModelBase
    {
        public List<RewardPointsHistoryModel> RewardPoints { get; set; } = new();
        public Money RewardPointsBalance { get; set; } // TODO: (mh) (core) Put raw MONEY here
        public string RewardPointsBalanceFormatted { get; set; }

        [LocalizedDisplay("RewardPoints.Fields.")]
        public partial class RewardPointsHistoryModel : EntityModelBase
        {
            [LocalizedDisplay("*Points")]
            public int Points { get; set; }

            [LocalizedDisplay("*PointsBalance")]
            public int PointsBalance { get; set; }

            [LocalizedDisplay("*Message")]
            public string Message { get; set; }

            [LocalizedDisplay("Common.CreatedOn")]
            public DateTime CreatedOn { get; set; }
        }
    }
}
