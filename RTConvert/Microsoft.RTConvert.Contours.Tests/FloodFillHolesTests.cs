///  ------------------------------------------------------------------------------------------
///  Copyright (c) Microsoft Corporation. All rights reserved.
///  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
///  ------------------------------------------------------------------------------------------

namespace Microsoft.RTConvert.Contours.Tests
{
    using System.Diagnostics;
    using System.Drawing;
    using System.Linq;
    using System.IO;
    using Microsoft.RTConvert.Contours;
    using Microsoft.RTConvert.Models;
    using Microsoft.RTConvert.MedIO.Extensions;
    using Microsoft.RTConvert.MedIO.Tests.Extensions;
    using NUnit.Framework;

    [TestFixture]
    public class FloodFillHolesTests
    {
        private static string GetTestDataPath(string relativePath)
        {
            return Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "MaskToContourTestData", relativePath));
        }

        [TestCase]
        public void FloodFillTest3D()
        {
            var actual = new Volume3D<byte>(3, 3, 3);

            // 0 X 0
            // X 0 X
            // 0 X 0
            for (int i= 0; i < 3; i++)
            {
                actual[1, 0, i] = 1;
                actual[0, 1, i] = 1;
                actual[2, 1, i] = 1;
                actual[1, 2, i] = 1;
            }

            var expected = actual.Copy();
            expected[1, 1, 0] = 1;
            expected[1, 1, 1] = 1;
            expected[1, 1, 2] = 1;

            actual.FillHoles();

            CollectionAssert.AreEqual(expected.Array, actual.Array);
        }

        [TestCase("mask1.png")]
        [TestCase("mask2.png")]
        [TestCase("mask3.png")]
        [TestCase("mask4.png")]
        [TestCase("holes2.png")]
        public void FloodFillTest2(string filename)
        {
            var path = GetTestDataPath(filename);

            var image = new Bitmap(path);
            var mask = image.ToByteArray();

            Assert.IsTrue(mask.Any(x => x == 0));
            Assert.IsTrue(mask.Any(x => x == 1));
            Assert.IsTrue(mask.Length == image.Width * image.Height);

            var actual = new Volume2D<byte>(mask, image.Width, image.Height, 1, 1, new Point2D(), new Matrix2());
            var contoursFilled = actual.ContoursFilled();
            var expected = actual.CreateSameSize<byte>();
            expected.Fill(contoursFilled, (byte)1);

            var stopwatch = Stopwatch.StartNew();

            FillPolygon.FloodFillHoles(actual.Array, expected.DimX, expected.DimY, 0, 0, 1, 0);

            stopwatch.Stop();

            actual.SaveBrushVolumeToPng(@"C:\Temp\Actual.png");
            expected.SaveBrushVolumeToPng(@"C:\Temp\Expected.png");
            Assert.AreEqual(expected.Array, actual.Array, "Extracting filled contours and filling those should give the same result as flood filling holes.");
            var contoursWithHoles = actual.ContoursWithHoles();
            var filledWithHoles = actual.CreateSameSize<byte>();
            filledWithHoles.Fill(contoursWithHoles, (byte)1);
            Assert.AreEqual(actual.Array, filledWithHoles.Array, "Extracting contours with holes and filling those in should not change the mask");
        }

        [TestCase()]
        public void FloodFillTest3()
        {
            var actual = new Volume3D<byte>(3, 3, 3);

            // X X X
            // X 0 X
            // X X X
            for (var i = 0; i < actual.Length; i++)
            {
                actual[i] = 1;
            }

            actual[1, 1, 0] = 0;
            actual[1, 1, 1] = 0;
            actual[1, 1, 2] = 0;

            var expected = actual.Copy();
            expected[1, 1, 0] = 1;
            expected[1, 1, 1] = 1;
            expected[1, 1, 2] = 1;

            actual.FillHoles();

            CollectionAssert.AreEqual(expected.Array, actual.Array);
        }
    }
}
