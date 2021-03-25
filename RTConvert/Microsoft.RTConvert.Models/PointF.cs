// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;

namespace Microsoft.RTConvert.Models
{
    /// <summary>
    /// A simple replacement for System.Drawing.PointF to avoid dependency on System.Drawing.
    /// </summary>
    /// <remarks>System.Drawing on Linux requires an additional dependency of libgdiplus.</remarks>
    public struct PointF : IEquatable<PointF>
    {
        /// <summary>
        /// Initializes a new instance of the PointF class with the specified coordinates.
        /// </summary>
        /// <param name="x">The horizontal position of the point.</param>
        /// <param name="y">The vertical position of the point.</param>
        public PointF(float x, float y)
        {
            X = x;
            Y = y;
        }

        /// <summary>
        /// Gets the x-coordinate of this PointF.
        /// </summary>
        public float X { get; }

        /// <summary>
        /// Gets the y-coordinate of this PointF.
        /// </summary>
        public float Y { get; }

        /// <summary>
        /// Specifies whether this PointF contains the same coordinates as the specified Object.
        /// </summary>
        /// <param name="obj">The Object to test.</param>
        /// <returns>This method returns true if obj is a PointF and has the same coordinates as this Point.</returns>
        public override bool Equals(object obj)
        {
            return obj is PointF f && Equals(f);
        }

        /// <summary>
        /// Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <param name="other">An object to compare with this object.</param>
        /// <returns>true if the current object is equal to other; otherwise, false.</returns>
        public bool Equals(PointF other)
        {
            return X == other.X &&
                   Y == other.Y;
        }

        /// <summary>
        /// Returns a hash code for this PointF structure.
        /// </summary>
        /// <returns>An integer value that specifies a hash value for this PointF structure.</returns>
        public override int GetHashCode()
        {
            return HashCode.Combine(X, Y);
        }

        /// <summary>
        /// Compares two PointF structures.
        /// The result specifies whether the values of the X and Y properties of the two PointF structures are equal.
        /// </summary>
        /// <param name="left">A PointF to compare.</param>
        /// <param name="right">A PointF to compare.</param>
        /// <returns>true if the X and Y values of the left and right PointF structures are equal; otherwise, false.</returns>
        public static bool operator ==(PointF left, PointF right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Determines whether the coordinates of the specified points are not equal.
        /// </summary>
        /// <param name="left">A PointF to compare.</param>
        /// <param name="right">A PointF to compare.</param>
        /// <returns>true to indicate the X and Y values of left and right are not equal; otherwise, false.</returns>
        public static bool operator !=(PointF left, PointF right)
        {
            return !(left == right);
        }
    }
}
