﻿using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Localization;

namespace Smartstore.Web.Models.Catalog
{
    public interface IQuantityInput
    {
        int EnteredQuantity { get; }
        int MinOrderAmount { get; }
        int MaxOrderAmount { get; }
        int QuantityStep { get; }
        LocalizedValue<string> QuantityUnitName { get; }
        List<SelectListItem> AllowedQuantities { get; }
        QuantityControlType QuantiyControlType { get; }
    }
}
