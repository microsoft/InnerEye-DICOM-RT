// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.RTConvert.Common.Models
{
    using System;
    using System.Collections.Generic;
    using Microsoft.RTConvert.Models;

    /// <summary>
    /// Lookup table for structure name to RGB color
    /// </summary>
    [Serializable]
    public class StructureColor
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="structureToColor"></param>
        public StructureColor(Dictionary<string, RGBColor> structureToColor)
        {
            StructureToColor = structureToColor ?? throw new ArgumentNullException(nameof(structureToColor));
        }

        /// <summary>
        /// Gets the dictionary map
        /// </summary>
        public Dictionary<string, RGBColor> StructureToColor { get; }

        /// <summary>
        /// Extract a colour from the map by structure name
        /// </summary>
        /// <param name="key"></param>
        /// <param name="color"></param>
        /// <returns></returns>
        public bool TryGetValue(string key, out RGBColor color)
        {
            return StructureToColor.TryGetValue(key, out color);
        }
    }
}