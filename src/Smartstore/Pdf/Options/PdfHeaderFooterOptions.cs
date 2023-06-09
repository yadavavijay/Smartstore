﻿using DinkToPdf;

namespace Smartstore.Pdf
{
    public class PdfHeaderFooterOptions : PdfOptions
    {
        /// <summary>
        /// Spacing between footer and content in mm. Default = 5.
        /// </summary>
        public float? Spacing { get; set; } = 5;

        /// <summary>
        /// Display line below the header / above the footer
        /// </summary>
        public bool ShowLine { get; set; }

        /// <summary>
        /// Set font name (default Arial)
        /// </summary>
        public string FontName { get; set; }

        /// <summary>
        /// Set font size. Default = 10.
        /// </summary>
        public float? FontSize { get; set; } = 10;

        /// <summary>
        /// Left aligned text
        /// </summary>
        public string TextLeft { get; set; }

        /// <summary>
        /// Centered text
        /// </summary>
        public string TextCenter { get; set; }

        /// <summary>
        /// Right aligned text
        /// </summary>
        public string TextRight { get; set; }

        public bool HasText => TextLeft.HasValue() || TextCenter.HasValue() || TextRight.HasValue();

        /// <inheritdoc/>
        protected internal override void Apply(string flag, HtmlToPdfDocument document)
        {
            // TODO: (core) Apply PdfHeaderFooterOptions
        }
    }
}