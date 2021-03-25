// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.RTConvert.Converters
{
    using Microsoft.RTConvert.Models;

    /// <summary>
    /// Colors may be partially specified on the command line, capture what information is supplied.
    /// </summary>
    public struct RGBColorOption
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="r">Red component.</param>
        /// <param name="g">Green component.</param>
        /// <param name="b">Blue component.</param>
        public RGBColorOption(byte? r, byte? g, byte? b)
        {
            R = r;
            G = g;
            B = b;
        }

        /// <summary>
        /// The red component
        /// </summary>
        public byte? R { get; }

        /// <summary>
        /// The green componenet
        /// </summary>
        public byte? G { get; }

        /// <summary>
        /// The blue component
        /// </summary>
        public byte? B { get; }

        public RGBColor ApplyDefault(RGBColor defaultColor) =>
            new RGBColor(
                R ?? defaultColor.R,
                G ?? defaultColor.G,
                B ?? defaultColor.B);
    }
}
