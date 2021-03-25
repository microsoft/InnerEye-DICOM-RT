// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.RTConvert.Models
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// Represents an RGB color
    /// </summary>
    [Serializable]
    public class RGBColor : IEquatable<RGBColor>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RGBColor"/> class.
        /// </summary>
        /// <param name="r">The red value.</param>
        /// <param name="g">The green value.</param>
        /// <param name="b">The blue value.</param>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "r")]
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "g")]
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "b")]
        public RGBColor(byte r, byte g, byte b)
        {
            R = r;
            G = g;
            B = b;
        }

        /// <summary>
        /// The red component
        /// </summary>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "R")]
        public byte R { get; }

        /// <summary>
        /// The green componenet
        /// </summary>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "G")]
        public byte G { get; }

        /// <summary>
        /// The blue component
        /// </summary>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "B")]
        public byte B { get; }

        public override bool Equals(object obj)
        {
            return Equals(obj as RGBColor);
        }

        public bool Equals(RGBColor other)
        {
            return other != null &&
                   R == other.R &&
                   G == other.G &&
                   B == other.B;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(R, G, B);
        }

        /// <summary>
        /// Express value as string 
        /// </summary>
        public override string ToString() => $"{R},{G},{B}";

        public static bool operator ==(RGBColor left, RGBColor right)
        {
            return EqualityComparer<RGBColor>.Default.Equals(left, right);
        }

        public static bool operator !=(RGBColor left, RGBColor right)
        {
            return !(left == right);
        }
    }
}