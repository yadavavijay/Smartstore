﻿using System.ComponentModel.DataAnnotations;
using Smartstore.Domain;

namespace Smartstore.Core.Media
{
    /// <summary>
    /// Represents the raw media data.
    /// </summary>
    public partial class MediaStorage : BaseEntity
    {
        /// <summary>
        /// Gets or sets the media binary data.
        /// </summary>
        [Required, MaxLength]
        public byte[] Data { get; set; }
    }
}