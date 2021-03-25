// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;

namespace Microsoft.RTConvert.Models
{
    /// <summary>
    /// A simple replacement for System.Drawing.RectangleF to avoid dependency on System.Drawing.
    /// </summary>
    /// <remarks>System.Drawing on Linux requires an additional dependency of libgdiplus.</remarks>
    public struct RectangleF : IEquatable<RectangleF>
    {
        /// <summary>
        /// Initializes a new instance of the RectangleF class with the specified location and size.
        /// </summary>
        /// <param name="x">The x-coordinate of the upper-left corner of the rectangle.</param>
        /// <param name="y">The y-coordinate of the upper-left corner of the rectangle.</param>
        /// <param name="width">The width of the rectangle.</param>
        /// <param name="height">The height of the rectangle.</param>
        public RectangleF(float x, float y, float width, float height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        /// <summary>
        /// Gets the x-coordinate of the upper-left corner of this RectangleF structure.
        /// </summary>
        public float X { get; }

        /// <summary>
        /// Gets the y-coordinate of the upper-left corner of this RectangleF structure.
        /// </summary>
        public float Y { get; }

        /// <summary>
        /// Gets the width of this RectangleF structure.
        /// </summary>
        public float Width { get; }

        /// <summary>
        /// Gets the height of this RectangleF structure.
        /// </summary>
        public float Height { get; }

        /// <summary>
        /// Gets the x-coordinate of the left edge of this RectangleF structure.
        /// </summary>
        public float Left => X;

        /// <summary>
        /// Gets the x-coordinate that is the sum of X and Width of this RectangleF structure.
        /// </summary>
        public float Right => X + Width;

        /// <summary>
        /// Gets the y-coordinate of the top edge of this RectangleF structure.
        /// </summary>
        public float Top => Y;

        /// <summary>
        /// Gets the y-coordinate that is the sum of Y and Height of this RectangleF structure.
        /// </summary>
        public float Bottom => Y + Height;

        /// <summary>
        /// Tests whether obj is a RectangleF with the same location and size of this RectangleF.
        /// </summary>
        /// <param name="obj">The Object to test.</param>
        /// <returns>true if obj is a RectangleF and its X, Y, Width, and Height properties are equal to the corresponding properties of this RectangleF; otherwise, false.</returns>
        public override bool Equals(object obj)
        {
            return obj is RectangleF f && Equals(f);
        }

        /// <summary>
        /// Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <param name="other">An object to compare with this object.</param>
        /// <returns>true if the current object is equal to other; otherwise, false.</returns>
        public bool Equals(RectangleF other)
        {
            return X == other.X &&
                   Y == other.Y &&
                   Width == other.Width &&
                   Height == other.Height;
        }

        /// <summary>
        /// Gets the hash code for this RectangleF structure.
        /// </summary>
        /// <returns>The hash code for this RectangleF.</returns>
        public override int GetHashCode()
        {
            return HashCode.Combine(X, Y, Width, Height);
        }

        /// <summary>
        /// Tests whether two RectangleF structures have equal location and size.
        /// </summary>
        /// <param name="left">The RectangleF structure that is to the left of the equality operator.</param>
        /// <param name="right">The RectangleF structure that is to the right of the equality operator.</param>
        /// <returns>true if the two specified RectangleF structures have equal X, Y, Width, and Height properties; otherwise, false.</returns>
        public static bool operator ==(RectangleF left, RectangleF right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Tests whether two RectangleF structures differ in location or size.
        /// </summary>
        /// <param name="left">The RectangleF structure that is to the left of the inequality operator.</param>
        /// <param name="right">The RectangleF structure that is to the right of the inequality operator.</param>
        /// <returns>true if any of the X , Y, Width, or Height properties of the two Rectangle structures are unequal; otherwise, false.</returns>
        public static bool operator !=(RectangleF left, RectangleF right)
        {
            return !(left == right);
        }
    }
}
