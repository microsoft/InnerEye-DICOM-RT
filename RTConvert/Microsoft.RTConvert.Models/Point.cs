// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;

namespace Microsoft.RTConvert.Models
{
    /// <summary>
    /// A simple replacement for System.Drawing.Point to avoid dependency on System.Drawing.
    /// </summary>
    /// <remarks>System.Drawing on Linux requires an additional dependency of libgdiplus.</remarks>
    public struct Point : IEquatable<Point>
    {
        /// <summary>
        /// Initializes a new instance of the Point struct with the specified coordinates.
        /// </summary>
        /// <param name="x">The horizontal position of the point.</param>
        /// <param name="y">The vertical position of the point.</param>
        public Point(int x, int y)
        {
            X = x;
            Y = y;
        }

        /// <summary>
        /// Gets the x-coordinate of this Point.
        /// </summary>
        public int X { get; }

        /// <summary>
        /// Gets the y-coordinate of this Point.
        /// </summary>
        public int Y { get; }

        /// <summary>
        /// Specifies whether this point instance contains the same coordinates as the specified object.
        /// </summary>
        /// <param name="obj">The Object to test for equality.</param>
        /// <returns>true if obj is a Point and has the same coordinates as this point instance.</returns>
        public override bool Equals(object obj)
        {
            return obj is Point point && Equals(point);
        }

        /// <summary>
        /// Specifies whether this point instance contains the same coordinates as another point.
        /// </summary>
        /// <param name="other">The point to test for equality.</param>
        /// <returns>true if other has the same coordinates as this point instance.</returns>
        public bool Equals(Point other)
        {
            return X == other.X &&
                   Y == other.Y;
        }

        /// <summary>
        /// Returns a hash code for this Point.
        /// </summary>
        /// <returns>An integer value that specifies a hash value for this Point.</returns>
        public override int GetHashCode()
        {
            return HashCode.Combine(X, Y);
        }

        /// <summary>
        /// Compares two Point objects.
        /// The result specifies whether the values of the X and Y properties of the two Point objects are equal.
        /// </summary>
        /// <param name="left">A Point to compare.</param>
        /// <param name="right">A Point to compare.</param>
        /// <returns>true if the X and Y values of left and right are equal; otherwise, false.</returns>
        public static bool operator ==(Point left, Point right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Compares two Point objects.
        /// The result specifies whether the values of the X or Y properties of the two Point objects are unequal.
        /// </summary>
        /// <param name="left">A Point to compare.</param>
        /// <param name="right">A Point to compare.</param>
        /// <returns>true if the values of either the X properties or the Y properties of left and right differ;
        /// otherwise, false.</returns>
        public static bool operator !=(Point left, Point right)
        {
            return !(left == right);
        }
    }
}
