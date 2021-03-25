// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;

namespace Microsoft.RTConvert.Models
{
    /// <summary>
    /// A simple replacement for System.Drawing.Rectangle to avoid dependency on System.Drawing.
    /// </summary>
    /// <remarks>System.Drawing on Linux requires an additional dependency of libgdiplus.</remarks>
    public struct Rectangle : IEquatable<Rectangle>
    {
        /// <summary>
        /// Initializes a new instance of the Rectangle class with the specified location and size.
        /// </summary>
        /// <param name="x">The x-coordinate of the upper-left corner of the rectangle.</param>
        /// <param name="y">The y-coordinate of the upper-left corner of the rectangle.</param>
        /// <param name="width">The width of the rectangle.</param>
        /// <param name="height">The height of the rectangle.</param>
        public Rectangle(int x, int y, int width, int height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        /// <summary>
        /// Gets the x-coordinate of the upper-left corner of this Rectangle structure.
        /// </summary>
        public int X { get; }

        /// <summary>
        /// Gets the y-coordinate of the upper-left corner of this Rectangle structure.
        /// </summary>
        public int Y { get; }

        /// <summary>
        /// Gets the width of this Rectangle structure.
        /// </summary>
        public int Width { get; }

        /// <summary>
        /// Gets the height of this Rectangle structure.
        /// </summary>
        public int Height { get; }

        /// <summary>
        /// Tests whether obj is a Rectangle structure with the same location and size of this Rectangle structure.
        /// </summary>
        /// <param name="obj">The Object to test.</param>
        /// <returns>This method returns true if obj is a Rectangle structure and its
        /// X, Y, Width, and Height properties are equal
        /// to the corresponding properties of this Rectangle structure; otherwise, false.</returns>
        public override bool Equals(object obj)
        {
            return obj is Rectangle rectangle && Equals(rectangle);
        }

        /// <summary>
        /// Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <param name="other">An object to compare with this object.</param>
        /// <returns>true if the current object is equal to other; otherwise, false.</returns>
        public bool Equals(Rectangle other)
        {
            return X == other.X &&
                   Y == other.Y &&
                   Width == other.Width &&
                   Height == other.Height;
        }

        /// <summary>
        /// Returns the hash code for this Rectangle structure.
        /// </summary>
        /// <returns>An integer that represents the hash code for this rectangle.</returns>
        public override int GetHashCode()
        {
            return HashCode.Combine(X, Y, Width, Height);
        }

        /// <summary>
        /// Tests whether two Rectangle structures have equal location and size.
        /// </summary>
        /// <param name="left">The Rectangle structure that is to the left of the equality operator.</param>
        /// <param name="right">The Rectangle structure that is to the right of the equality operator.</param>
        /// <returns>This operator returns true if the two Rectangle structures have equal X, Y, Width, and Height properties.</returns>
        public static bool operator ==(Rectangle left, Rectangle right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Tests whether two Rectangle structures differ in location or size.
        /// </summary>
        /// <param name="left">The Rectangle structure that is to the left of the inequality operator.</param>
        /// <param name="right">The Rectangle structure that is to the right of the inequality operator.</param>
        /// <returns>This operator returns true if any of the
        /// X, Y, Width or Height properties of the two Rectangle structures are unequal; otherwise false.</returns>
        public static bool operator !=(Rectangle left, Rectangle right)
        {
            return !(left == right);
        }
    }
}
