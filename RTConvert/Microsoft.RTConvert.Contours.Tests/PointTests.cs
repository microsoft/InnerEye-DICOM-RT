///  ------------------------------------------------------------------------------------------
///  Copyright (c) Microsoft Corporation. All rights reserved.
///  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
///  ------------------------------------------------------------------------------------------

namespace Microsoft.RTConvert.Contours.Tests
{
    using System;
    using Microsoft.RTConvert.Contours;
    using Microsoft.RTConvert.Models;
    using NUnit.Framework;

    [TestFixture]
    public class PointExtensionTests
    {
        private void AreEqual(PointF expected, PointF actual, string message)
        {
            Assert.AreEqual(expected.X, actual.X, 1e-5, $"X mismatch for {message}");
            Assert.AreEqual(expected.Y, actual.Y, 1e-5, $"Y mismatch for {message}");
        }

        [Test]
        public void PointExtensions()
        {
            var x1 = 1;
            var x2 = 2;
            var y1 = 3;
            var y2 = 4;
            var p1 = new PointF(x1, y1);
            var p2 = new PointF(x2, y2);
            var zero = new PointF(0, 0);
            var sum12 = p1.Add(p2);
            AreEqual(new PointF(x1 + x2, y1 + y2), sum12, "Addition");
            AreEqual(p1, sum12.Subtract(p2), "Subtraction");
            AreEqual(p1.Add(p1), p1.Multiply(2), "Multiply by 2");
            Assert.AreEqual(x1 * x1 + y1 * y1, p1.LengthSquared(), 1e-5, "Length");
            Assert.IsTrue(p1.LengthSquared() > 0, "non-zero length");
            var normed = p1.Normalize();
            Assert.AreEqual(1, normed.LengthSquared(), 1e-5, "length after normalization");
            Assert.Throws<ArgumentException>(() => zero.Normalize(), "Can't normalize a zero length vector");
            Assert.AreEqual(p1.LengthSquared(), p1.DotProduct(p1), "Length and dot product with itself should match");
        }
    }
}
