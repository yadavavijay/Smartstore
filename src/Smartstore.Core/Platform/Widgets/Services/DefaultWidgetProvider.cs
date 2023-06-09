﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Smartstore.Collections;

namespace Smartstore.Core.Widgets
{
    public class DefaultWidgetProvider : IWidgetProvider
    {
        private readonly IHttpContextAccessor _accessor;

        private Multimap<string, WidgetInvoker> _zoneWidgetsMap;
        private Multimap<Regex, WidgetInvoker> _zoneExpressionWidgetsMap;

        public DefaultWidgetProvider(IHttpContextAccessor accessor)
        {
            _accessor = accessor;
        }

        public virtual void RegisterWidget(string[] zones, WidgetInvoker widget)
        {
            Guard.NotNull(zones, nameof(zones));
            Guard.NotNull(widget, nameof(widget));

            if (_accessor.HttpContext?.Request?.Query?.ContainsKey("nowidgets") == true)
            {
                return;
            }

            if (_zoneWidgetsMap == null)
            {
                _zoneWidgetsMap = new Multimap<string, WidgetInvoker>(StringComparer.OrdinalIgnoreCase, invokers => new HashSet<WidgetInvoker>());
            }

            foreach (var zone in zones)
            {
                _zoneWidgetsMap.Add(zone, widget);
            }
        }

        public virtual void RegisterWidget(Regex zonePattern, WidgetInvoker widget)
        {
            Guard.NotNull(zonePattern, nameof(zonePattern));
            Guard.NotNull(widget, nameof(widget));

            if (_accessor.HttpContext?.Request?.Query?.ContainsKey("nowidgets") == true)
            {
                return;
            }

            if (_zoneExpressionWidgetsMap == null)
            {
                _zoneExpressionWidgetsMap = new Multimap<Regex, WidgetInvoker>(invokers => new HashSet<WidgetInvoker>());
            }

            _zoneExpressionWidgetsMap.Add(zonePattern, widget);
        }

        public bool HasContent(string zone)
        {
            if (zone.IsEmpty())
            {
                return false;
            }

            if (_zoneWidgetsMap != null && _zoneWidgetsMap.ContainsKey(zone))
            {
                return true;
            }

            if (_zoneExpressionWidgetsMap != null)
            {
                foreach (var entry in _zoneExpressionWidgetsMap)
                {
                    var rg = entry.Key;
                    if (rg.IsMatch(zone))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public IEnumerable<WidgetInvoker> GetWidgets(string zone)
        {
            if (zone.IsEmpty())
            {
                return Enumerable.Empty<WidgetInvoker>();
            } 

            var result = new List<WidgetInvoker>();

            if (_zoneWidgetsMap != null && _zoneWidgetsMap.TryGetValues(zone, out var widgets))
            {
                result.AddRange(widgets);
            }

            if (_zoneExpressionWidgetsMap != null)
            {
                foreach (var entry in _zoneExpressionWidgetsMap)
                {
                    var rg = entry.Key;
                    if (rg.IsMatch(zone))
                    {
                        result.AddRange(entry.Value);
                    }
                }
            }

            return result;
        }

        public bool ContainsWidget(string zone, string widgetKey)
        {
            Guard.NotEmpty(zone, nameof(zone));
            Guard.NotEmpty(widgetKey, nameof(widgetKey));

            if (_zoneWidgetsMap != null && _zoneWidgetsMap.TryGetValues(zone, out var widgets))
            {
                // INFO: Hot path code
                foreach (var widget in widgets)
                {
                    if (widget.Key == widgetKey)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}