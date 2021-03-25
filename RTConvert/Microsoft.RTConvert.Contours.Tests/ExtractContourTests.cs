///  ------------------------------------------------------------------------------------------
///  Copyright (c) Microsoft Corporation. All rights reserved.
///  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
///  ------------------------------------------------------------------------------------------

namespace Microsoft.RTConvert.Contours.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Drawing.Drawing2D;
    using System.Drawing.Imaging;
    using System.IO;
    using System.Linq;
    using System.Text;
    using Microsoft.RTConvert.Contours;
    using Microsoft.RTConvert.Models;
    using Microsoft.RTConvert.MedIO.Extensions;
    using Microsoft.RTConvert.MedIO.Tests.Extensions;
    using NUnit.Framework;
    using Point = Models.PointF;
    using PointInt = Models.Point;

    [TestFixture()]
    public class ExtractContourTests
    {
        public static Volume2D<byte> CreateVolume(byte[] array, int dimX, int dimY) =>
            new Volume2D<byte>(array, dimX, dimY, 1, 1, new Models.Point2D(), new Models.Matrix2());

        public static Volume2D<byte> CreateVolume(int dimX, int dimY) =>
            new Volume2D<byte>(dimX, dimY, 1, 1, new Models.Point2D(), new Models.Matrix2());

        private static string GetTestDataPath(string relativePath)
        {
            return Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, relativePath));
        }

        private static string ExpectedResultsFolder = GetTestDataPath("ExpectedResults");
        private static string SaveImageFolder = GetTestDataPath("TestOut");
        private static string MaskToContourTestDataFolder = GetTestDataPath("MaskToContourTestData");

        /// <summary>
        /// Creates a new point from the arguments, using a cast from double to float.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public Point CreatePoint(double x, double y) => new Point((float)x, (float)y);

        public static Volume2D<byte> LoadMask(string filename)
        {
            var fullFilename = Path.Combine(MaskToContourTestDataFolder, filename);
            var image = new Bitmap(fullFilename);
            var mask = CreateVolume(image.ToByteArray(), image.Width, image.Height);
            Assert.IsTrue(mask.Length == image.Width * image.Height);
            return mask;
        }

        [SetUp]
        public void SetUpTests()
        {
            if (!Directory.Exists(SaveImageFolder))
            {
                Directory.CreateDirectory(SaveImageFolder);
            }
        }

        [TestCase("problem-noholes.png")]
        // @TODO Don't run this test yet, it will fail. A large region does not get
        // picked up in contour extraction.
        //[TestCase("slice_External_98.png")]
        // @TODO Don't run this test yet, it will fail. This is a patch taken from
        // slice_External_98.png containing the problem region.
        //[TestCase("slice_External_98_min.png")]
        public void Conversionin3D(string filename)
        {
            var mask = LoadMask(filename);

            Assert.IsTrue(mask.Array.Any(x => x == 0));
            Assert.IsTrue(mask.Array.Any(x => x == 1));

            var dimZ = 10;
            var expected = mask.Extrude(dimZ, 1.0);
            var pointsPerSlice = expected.ContoursWithHolesPerSlice();

            var renderingContours = pointsPerSlice.ContoursForSlice(0);
            var rendered = mask.CreateSameSize<byte>();
            foreach (var contour in renderingContours)
            {
                FillPolygon.Fill(contour.ContourPoints, rendered.Array, rendered.DimX, rendered.DimY, 0, 0, (byte)1);
            }
            PlotMaskAndContour(Path.Combine(SaveImageFolder, "Contours.png"), mask, rendered, renderingContours.Select(c => c.ContourPoints));

            var actual = pointsPerSlice.ToVolume3D(expected);
#if DEBUG
            var actual2d = actual.Slice(SliceType.Axial, 0);
            actual2d.SaveBrushVolumeToPng(Path.Combine(SaveImageFolder,"Actual.png"));

            var expected2d = expected.Slice(SliceType.Axial, 0);
            expected2d.SaveBrushVolumeToPng(Path.Combine(SaveImageFolder, "Expected.png"));

            var diff = actual2d.Array.Zip(expected2d.Array, (act, exp) => act != exp ? act == (byte)1 ? (byte)1 : (byte)2 : (byte)0).ToArray();

            var diff2d = CreateVolume(diff, expected2d.DimX, expected2d.DimY);
            diff2d.SaveBrushVolumeToPng(Path.Combine(SaveImageFolder, "Diff.png"));
#endif
            for (int i = 0; i < expected.Length; i++)
            {
                Assert.AreEqual(expected[i], actual[i]);
            }

        }

        [TestCase]
        public void CheckPointIndividualPixels()
        {
            var image = new Bitmap(Path.Combine(MaskToContourTestDataFolder, "isolatedPixels.png"));
            var mask = image.ToByteArray();

            Assert.IsTrue(mask.Any(x => x == 0));
            Assert.IsTrue(mask.Any(x => x == 1));
            Assert.IsTrue(mask.Length == image.Width * image.Height);
            var volume = CreateVolume(mask, image.Width, image.Height);
            var contours = volume.ContoursWithHoles();

            Assert.AreEqual(6, contours.Count(x => x.RegionAreaPixels == 1), 6);
            Assert.AreEqual(6, contours.Count);
        }

        [TestCase("checkerboard.png", "checkerboard-result.png", 1)]
        [TestCase("checkerboard2.png", "checkerboard2-result.png", 1)]
        [TestCase("holes.png", "mask1.png", 1)]
        [TestCase("holes2.png", "triangle.png", 1)]
        [TestCase("mask2.png", "mask2-noholes.png", 1)]
        [TestCase("mask3.png", "mask3-noholes.png", 2)]
        [TestCase("mask4.png", "mask4-noholes.png", 1)]
        [TestCase("problem.png", "problem-noholes.png", 4)]
        public void FillHoles(string filename, string expectedResult, int expectedContourCount)
        {
            var mask = LoadMask(filename);
            var contours = mask.ContoursFilled().ToList();
            Assert.AreEqual(expectedContourCount, contours.Count, "Expected number of contours does not match");
            var actualMask = mask.CreateSameSize<byte>();
            actualMask.Fill(contours, (byte)1);
            var filenameWithoutExtension = Path.GetFileNameWithoutExtension(filename);
            actualMask.SaveBrushVolumeToPng(Path.Combine(SaveImageFolder, "Fillholes-" + filenameWithoutExtension + "-actualMask.png"));
            var expectedMask = LoadMask(expectedResult);
            for (int i = 0; i < expectedMask.Length; i++)
            {
                Assert.AreEqual(expectedMask[i], actualMask[i], $"Mismatch at pixel index {i}");
            }
        }

        [TestCase("mask1.png")]
        [TestCase("mask2-noholes.png")]
        [TestCase("mask3-noholes.png")]
        [TestCase("mask4-noholes.png")]
        [TestCase("triangle.png")]
        [TestCase("circle.png")]
        [TestCase("smallCircle.png")]
        [TestCase("4contours.png")]
        [TestCase("specialCase.png")]
        [TestCase("problem.png")]
        public void SingleShape(string filename)
        {
            var mask = LoadMask(filename);
            Assert.IsTrue(mask.Array.Any(x => x == 0));
            Assert.IsTrue(mask.Array.Any(x => x == 1));

            var expected = mask.CreateSameSize<byte>();
            var contours = expected.ContoursWithHoles();
            expected.Fill(contours, (byte)1);

            var actual = mask.CreateSameSize<byte>();
            actual.Fill(contours, (byte)1);

            actual.SaveBrushVolumeToPng(Path.Combine(SaveImageFolder, "Actual.png"));
            expected.SaveBrushVolumeToPng(Path.Combine(SaveImageFolder, "Expected.png"));

            for (int i = 0; i < expected.Length; i++)
            {
                Assert.AreEqual(expected[i], actual[i]);
            }
        }

        [TestCase(0, 0, 1, 0)]
        [TestCase(1, 0, 0, 0)]
        [TestCase(0, 0, 0, 1)]
        [TestCase(0, 1, 0, 0)]
        [TestCase(0, 0, 1, 1)]
        [TestCase(1, 1, 0, 0)]
        [TestCase(1, 0, 0, 1)]
        [TestCase(0, 1, 1, 0)]
        [TestCase(1, 1, 1, 1)]
        public void TwoPointTests(int point1X, int point1Y, int point2X, int point2Y)
        {
            var expected = CreateVolume(5, 5);
            var actual = CreateVolume(5, 5);

            expected[point1X, point1Y] = 1;
            expected[point2X, point2Y] = 1;

            var contours = expected.ContoursWithHoles();
            actual.Fill(contours, (byte)1);

            for (int i = 0; i < expected.Length; i++)
            {
                Assert.AreEqual(expected[i], actual[i]);
            }
        }

        [TestCase(0, 0, 1, 0, 1, 1)]
        [TestCase(0, 0, 1, 1, 1, 0)]
        [TestCase(0, 0, 0, 1, 1, 0)]
        [TestCase(0, 0, 1, 0, 0, 1)]
        [TestCase(1, 0, 1, 1, 0, 1)]
        [TestCase(1, 0, 0, 1, 1, 1)]
        [TestCase(1, 1, 0, 1, 1, 0)]
        public void ThreePointTests(int point1X, int point1Y, int point2X, int point2Y, int point3X, int point3Y)
        {
            var expected = CreateVolume(5, 5);
            var actual = CreateVolume(5, 5);

            expected[point1X, point1Y] = 1;
            expected[point2X, point2Y] = 1;
            expected[point3X, point3Y] = 1;

            var contours = expected.ContoursWithHoles();
            actual.Fill(contours, (byte)1);

            for (int i = 0; i < expected.Length; i++)
            {
                Assert.AreEqual(expected[i], actual[i]);
            }
        }

        /// <summary>
        /// Checks in detail which contour points are extracted from a binary mask, checking inner
        /// and outer rim of contours separately.
        /// </summary>
        /// <param name="filename"></param>
        [TestCase("isolatedPixel.png", 1, 1, new int[] { 1, 1 }, 0, null)]
        // A 3 pixel long line, 1 pixel wide. The search has to go to the end of the line and back to the start,
        // creating 4 points total.
        [TestCase("line.png", 1, 3, new int[] { 1, 1, 2, 1, 3, 1, 2, 1 }, 0, null)]
        // 4 pixel diamond, center pixel is background: Outer contour traverses the diamond CW.
        // Inner contour is around the white center pixel, CCW, meaning it traverses the same pixels that the
        // outside has, but in reverse order.
        [TestCase("diamond.png", 1, 4, new int[] { 2, 1, 3, 2, 2, 3, 1, 2 }, 1, new int[] { 2, 1, 1, 2, 2, 3, 3, 2 })]
        // There are 18 black pixel in the map. Inside of those, there are 7 white ones.
        // Contour starts CCW from (2, 0) along the outside black pixels.
        [TestCase("checkerboard.png", 3, 18, null, 7, new int[] { 2, 0, 1, 1, 0, 2, 1, 3, 2, 4, 3, 3, 4, 4, 5, 3, 6, 2, 5, 1, 4, 0, 3, 1 }, 2)]
        public void ExtractDegenerateContours(string filename, int expectedContourCount,
            int foregroundVoxels1, int[] expectedPoints1,
            int foregroundVoxels2, int[] expectedPoints2,
            int holesInContour2 = 0)
        {
            PointInt[] IntArrayToPoints(int[] integers)
            {
                if (integers == null)
                {
                    return null;
                }

                var points = new PointInt[integers.Length / 2];
                for (var index = 0; index < integers.Length; index += 2)
                {
                    points[index / 2] = new PointInt(integers[index], integers[index + 1]);
                }

                return points;
            }

            var mask = LoadMask(filename);
            var contours = ExtractContours.PolygonsWithHoles(mask, enableVerboseOutput: true).ToList();
            Assert.AreEqual(expectedContourCount, contours.Count, "Number of contours");
            var contour1 = contours[0];
            Assert.AreEqual(foregroundVoxels1, contour1.Outer.VoxelCounts.Foreground, "Single contour foreground count");
            if (expectedPoints1 != null)
            {
                Assert.AreEqual(IntArrayToPoints(expectedPoints1), contour1.Outer.Points);
            }
            Assert.AreEqual(foregroundVoxels2, contour1.Outer.VoxelCounts.Other, "First contour background count should match foreground count of hole contour");
            if (expectedPoints2 != null)
            {
                Assert.AreEqual(1, contours[0].Inner.Count, "Expected 1 hole in outermost contour");
                var contour2 = contours[0].Inner[0];
                Assert.AreEqual(foregroundVoxels2, contour2.VoxelCounts.Foreground, "Single contour foreground count");
                Assert.AreEqual(holesInContour2, contour2.VoxelCounts.Other, "Hole contour background count");
                Assert.AreEqual(IntArrayToPoints(expectedPoints2), contour2.Points);
            }
        }

        /// <summary>
        /// Test contour extraction where there are multiple top-level contours, each of which has holes.
        /// </summary>
        [Test]
        public void ExtractContoursMultipleToplevel()
        {
            var filename = "2contoursWithHoles.png";
            var mask = LoadMask(filename);
            var contours = ExtractContours.PolygonsWithHoles(mask, enableVerboseOutput: true).ToList();
            Assert.AreEqual(2, contours.Count, "Number of top-level contours");
            Assert.AreEqual(2, contours[0].Inner.Count, "Number of holes inside contour[0]");
            Assert.AreEqual(60 - 4 - 3, contours[0].TotalPixels, "Number of foreground voxels inside contour[0]");
            Assert.AreEqual(4, contours[0].Inner[0].VoxelCounts.Total, "Number of voxels inside contour[0].Inner[0]");
            Assert.AreEqual(3, contours[0].Inner[1].VoxelCounts.Total, "Number of voxels inside contour[0].Inner[1]");
            Assert.AreEqual(1, contours[1].Inner.Count, "Number of holes inside contour[1]");
            Assert.AreEqual(36 - 2, contours[1].TotalPixels, "Number of foreground voxels inside contour[1]");
            Assert.AreEqual(2, contours[1].Inner[0].VoxelCounts.Total, "Number of voxels inside contour[1].Inner[0]");
        }
        
        /// <summary>
        /// Test the computation of the size of contours when there are nested holes and inserts.
        /// </summary>
        [Test]
        public void ExtractContoursVoxelCountWhenHolesArePresent()
        {
            var filename = "concentricCircles.png";
            var mask = LoadMask(filename);
            var contours = ExtractContours.PolygonsWithHoles(mask, enableVerboseOutput: true).ToList();
            void IsInsideOf(PolygonPoints c, int expectedParent, int expectedForeground, int expectedOther)
            {
                Assert.AreEqual(expectedParent, c.InsideOfPolygon, $"Expected a polygon inside of {expectedParent}");
                Assert.AreEqual(expectedForeground, c.VoxelCounts.Foreground, "Foreground count");
                Assert.AreEqual(expectedOther, c.VoxelCounts.Other, "Other count");
            }
            
            // Outermost foreground contour
            IsInsideOf(contours[0].Outer, 0, 40 + 24 + 8, 32 + 16 + 1);
            // The inner rim of the outermost contour
            IsInsideOf(contours[0].Inner[0], 1, 32 + 16 + 1, 24 + 8);
            // The circle inside the outermost contour
            IsInsideOf(contours[1].Outer, 2, 24 + 8, 16 + 1);
            // The inner rim of the inserted circle 
            IsInsideOf(contours[1].Inner[0], 3, 16 + 1, 8);
            // The innermost 3x3 circle with a 1-pixel hole
            IsInsideOf(contours[2].Outer, 4, 8, 1);
            // The innermost 1-pixel hole
            IsInsideOf(contours[2].Inner[0], 5, 1, 0);
            // Check the number of voxels for each of the 3 concentric circles. 
            Assert.AreEqual(40, contours[0].TotalPixels);
            Assert.AreEqual(24, contours[1].TotalPixels);
            Assert.AreEqual(8, contours[2].TotalPixels);
        }

        /// <summary>
        /// Check that two arrays of points (floating point coordinates) match,
        /// allowing for a discrepancy of 1e-7 for each coordinate.
        /// </summary>
        /// <param name="expected"></param>
        /// <param name="actual"></param>
        private static void AssertPointsMatch(Point[] expected, Point[] actual)
        {
            Assert.AreEqual(expected.Length, actual.Length, "Number of points mismatch");
            foreach (var index in Enumerable.Range(0, expected.Length))
            {
                Assert.AreEqual(expected[index].X, actual[index].X, 1e-7, $"X at index {index}");
                Assert.AreEqual(expected[index].Y, actual[index].Y, 1e-7, $"Y at index {index}");
            }
        }

        /// <summary>
        /// Test traversing the inner polygon counterclockwise.
        /// </summary>
        [Test]
        public void ContourSmoothingCounterclockwise1()
        {
            // This is equivalent to the inner polygon of the "diamong.png"
            // testcase: 4 black pixels that surround a white one.
            var points = new PointInt[]
            {
                new PointInt(1, 0),
                new PointInt(0, 1),
                new PointInt(1, 2),
                new PointInt(2, 1),
            };
            var innerRim = SmoothPolygon.SmoothPoints(points, isCounterClockwise: true,
                smoothingType: ContourSmoothingType.None);
            // The expected contour should encircle the white pixel at (1,1)
            var expected = new Point[]
            {
                CreatePoint(1.5, 0.5),
                CreatePoint(0.5, 0.5),
                CreatePoint(0.5, 1.5),
                CreatePoint(1.5, 1.5)
            };
            AssertPointsMatch(expected, innerRim);
        }

        /// <summary>
        /// Test traversing the inner polygon counterclockwise.
        /// </summary>
        [Test]
        public void ContourSmoothingCounterclockwise2()
        {
            // Foreground pixels that surround two white pixels at (1,1) and (2,1)
            var points = new PointInt[]
            {
                new PointInt(1, 0),
                new PointInt(0, 1),
                new PointInt(1, 2),
                new PointInt(2, 2),
                new PointInt(3, 1),
                new PointInt(2, 0),
            };
            var innerRim = SmoothPolygon.SmoothPoints(points, isCounterClockwise: true,
                smoothingType: ContourSmoothingType.None);
            var expected = new Point[]
            {
                CreatePoint(1.5, 0.5),
                CreatePoint(0.5, 0.5),
                CreatePoint(0.5, 1.5),
                CreatePoint(1.5, 1.5),
                CreatePoint(2.5, 1.5),
                CreatePoint(2.5, 0.5),
            };
            AssertPointsMatch(expected, innerRim);
        }

        /// <summary>
        /// Test traversing the inner polygon counterclockwise.
        /// </summary>
        [Test]
        public void ContourSmoothingCounterclockwise3()
        {
            // Foreground pixels that surround 3 white pixels at (1,1), (1,2) and (2,2)
            // 1011
            // 1001
            // 1111
            var points = new PointInt[]
            {
                new PointInt(1, 0),
                new PointInt(0, 1),
                new PointInt(0, 2),
                new PointInt(1, 3),
                new PointInt(2, 3),
                new PointInt(3, 2),
                new PointInt(2, 1),
            };
            var innerRim = SmoothPolygon.SmoothPoints(points, isCounterClockwise: true,
                smoothingType: ContourSmoothingType.None);
            var expected = new Point[]
            {
                CreatePoint(1.5, 0.5),
                CreatePoint(0.5, 0.5),
                CreatePoint(0.5, 1.5),
                CreatePoint(0.5, 2.5),
                CreatePoint(1.5, 2.5),
                CreatePoint(2.5, 2.5),
                CreatePoint(2.5, 1.5),
                CreatePoint(1.5, 1.5),
            };
            AssertPointsMatch(expected, innerRim);
        }

        /// <summary>
        /// Tests whether the extraction of contours as integer points works, and if
        /// point contours can be merged correctly.
        /// </summary>
        /// <param name="filename"></param>
        [TestCase("isolatedPixel.png")]
        [TestCase("singlePixelWithHole.png")]
        [TestCase("twoPixelWithHole.png")]
        [TestCase("twoPixelWithHoleAndThinPart.png")]
        [TestCase("isolatedPixels.png")]
        [TestCase("mask3.png")]
        [TestCase("holeAndInsert.png")]
        [TestCase("2contoursWithHoles.png")]
        [TestCase("line.png")]
        [TestCase("diamond.png")]
        [TestCase("checkerboard.png")]
        [TestCase("checkerboard2.png")]
        [TestCase("u-shape1.png")]
        [TestCase("u-shape2.png")]
        [TestCase("concentricCircles2.png")]
        [TestCase("concentricCircles.png")]
        [TestCase("zigZagHole.png")]
        [TestCase("holeWith3Pixels.png")]
        public void ExtractContoursAndSmooth(string filename)
        {
            ExtractAndSmooth(filename);
        }

        // This is presently the only corner case of contour extraction that we know fails:
        // The hole is only 2 pixels, diagonally connected. When traversing the outside of the
        // hole, both pixels are correctly found. Afterwards, we traverse the inner rim of the
        // enclosing contour, which now disconnects the two points.
        [TestCase("holeWithCornerConnectivity.png")]
        public void ExtractContoursAndSmoothFailing(string filename)
        {
            Assert.Throws<AssertionException>(() => ExtractAndSmooth(filename));
        }

        private void ExtractAndSmooth(string filename)
        {
            var mask = LoadMask(filename);
            var filenamePrefix = $"ExtractContoursAndSmooth-{Path.GetFileNameWithoutExtension(filename)}";
            var files = MaskToContourToMask(mask, filenamePrefix);
            foreach (var file in files)
            {
                var expectedFilename = Path.Combine(ExpectedResultsFolder, file);
                var actualFilename = Path.Combine(SaveImageFolder, file);
                CompareImages(expectedFilename, actualFilename);
            }
        }

        private void CompareImages(string expectedFilename, string actualFilename)
        {
            var expectedTextFilename = Path.ChangeExtension(expectedFilename, "txt");
            var actualTextFilename = Path.ChangeExtension(actualFilename, "txt");

            var expectedLines = File.ReadAllLines(expectedTextFilename);
            var actualLines = File.ReadAllLines(actualTextFilename);

            Assert.AreEqual(expectedLines, actualLines, "Files have the same length");
        }

        /// <summary>
        /// Generates a random pixel mask, and tests that the roundrip from mask to contour to mask
        /// works correctly. Some of the test cases fail (exemplified in holeWithCornerConnectivity.png)
        /// </summary>
        /// <param name="maskSize"></param>
        /// <param name="foregroundProbability"></param>
        [TestCase(30, 0.1, false)]
        [TestCase(30, 0.2, false)]
        [TestCase(30, 0.3, false)]
        [TestCase(30, 0.4, true)]
        [TestCase(30, 0.5, true)]
        [TestCase(10, 0.8, true)]
        [TestCase(7, 0.8, true)]
        public void ExtractContoursFromRandomMask(int maskSize, 
            double foregroundProbability,
            bool isFailing)
        {
            var mask = CreateVolume(maskSize, maskSize);
            var random = new Random(0);
            foreach (var index in Enumerable.Range(0, mask.Length))
            {
                mask[index] = random.NextDouble() < foregroundProbability ? (byte)1 : (byte)0;
            }
            var filenamePrefix = $"ExtractContoursFromRandomMask-{maskSize}-{foregroundProbability}";
            PlotMaskAndContour(filenamePrefix + "-maskOnly.png", mask, null, null);
            if (isFailing)
            {
                Assert.Throws<AssertionException>(() => MaskToContourToMask(mask, filenamePrefix));
            }
            else
            {
                MaskToContourToMask(mask, filenamePrefix);
            }
        }

        private List<string> MaskToContourToMask(Volume2D<byte> mask, string filenamePrefix)
        {
            var smoothingTypes = new ContourSmoothingType[] { ContourSmoothingType.None, ContourSmoothingType.Small };
            var rendered = new List<Volume2D<byte>>();
            var files = new List<string>();
            foreach (var smoothingType in smoothingTypes)
            {
                var renderingImage = $"{filenamePrefix}-smooth{smoothingType}.png";
                Console.WriteLine("Rendering with legacy fill code, smoothing enabled");
                var (contours, r) = ExtractContoursAndPlot(mask, 10, smoothingType, renderingImage);
                rendered.Add(r);
                files.Add(renderingImage);
            }

            foreach (var index in Enumerable.Range(0, rendered.Count))
            {
                Console.WriteLine($"Comparing rendering for smoothing type {smoothingTypes[index]}");
                CompareRendering(mask, rendered[index]);
            }

            return files;
        }

        /// <summary>
        /// Tests whether the extraction of contours as integer points works, and if point contours can be merged correctly.
        /// Format of expected contours: (outer point count, inner contour count, total inner point count, pixels)
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="expectedContourProperties"></param>
        [TestCase("isolatedPixel.png", 2, new int[] { 1, 0, 0, 1 })]
        [TestCase("singlePixelWithHole.png", 2, new int[] { 8, 1, 4, 8 })]
        [TestCase("twoPixelWithHole.png", 2, new int[] { 20, 1, 8, 32 })]
        [TestCase("twoPixelWithHoleAndThinPart.png", 2, new int[] { 20, 1, 9, 31 })]
        [TestCase("isolatedPixels.png", 2, new int[] { 1, 0, 0, 1, 1, 0, 0, 1, 1, 0, 0, 1, 1, 0, 0, 1, 1, 0, 0, 1 })]
        [TestCase("mask3.png", 2, new int[] { })]
        [TestCase("holeAndInsert.png", 3, new int[] { 20, 1, 16, 20, 4, 0, 0, 4 })]
        [TestCase("2contoursWithHoles.png", 2, new int[] { 28, 2, 8 + 7, 60-7, 20, 1, 6, 34 })]
        // A 3 pixel long line, 1 pixel wide. The search has to go to the end of the line and back to the start,
        // creating 4 points total.
        [TestCase("line.png", 2, new int[] { 4, 0, 0, 3 })]
        [TestCase("diamond.png", 10, new int[] { 4, 1, 4, 4 })]
        // Checkboard patterns: To count expected number of points, traverse outside counterclockwise, starting
        // with leftmost point with lowest X coordinate. Traversing will visit some points twice!
        [TestCase("checkerboard.png", 10, new int[] { 20, 1, 12, 16, 1, 0, 0, 1, 1, 0, 0, 1 })]
        [TestCase("checkerboard2.png", 10, new int[] { 40, 2, 8 + 8, 36 })]
        // U-shapes have 34 points on the outside. For the 4 "inner corners", the next neighbors
        // are the diagonal neighbors, meaning that only 30 points end up on the polygon.
        [TestCase("u-shape1.png", 2, new int[] { 30, 0, 0, 34 })]
        [TestCase("u-shape2.png", 2, new int[] { 30, 0, 0, 34 })]
        [TestCase("concentricCircles2.png", 9, new int[] { 40, 1, 28, 72, 16, 1, 4, 24 })]
        [TestCase("concentricCircles.png", 0, new int[] { 40, 0, 0, 11*11 })]
        [TestCase("concentricCircles.png", 1, new int[] { 40, 1, 36, 40 })]
        [TestCase("concentricCircles.png", 2, new int[] { 40, 1, 36, 40, 24, 0, 0, 7*7 })]
        [TestCase("concentricCircles.png", 3, new int[] { 40, 1, 36, 40, 24, 1, 20, 24 })]
        [TestCase("concentricCircles.png", 9, new int[] { 40, 1, 36, 40, 24, 1, 20, 24, 8, 1, 4, 8 })]
        public void ExtractContoursAsPoints(string filename, int maxNestingLevel, int[] expectedContourProperties)
        {
            var mask = LoadMask(filename);
            var filenameWithoutExtension = Path.GetFileNameWithoutExtension(filename);
            var savePrefix = Path.Combine(SaveImageFolder, "ExtractContoursWithHoles-{filenameWithoutExtension}-nesting{maxNestingLevel}");
            var pointContours = ExtractContours.PolygonsWithHoles(mask, 1, maxNestingLevel, enableVerboseOutput: true).ToList();
            if (expectedContourProperties != null && expectedContourProperties.Length > 0)
            {
                for (var startIndex = 0; startIndex < expectedContourProperties.Length; startIndex += 4)
                {
                    var contourIndex = startIndex / 4;
                    var outerPointCount = expectedContourProperties[startIndex + 0];
                    var innerContourCout = expectedContourProperties[startIndex + 1];
                    var innerPointCount = expectedContourProperties[startIndex + 2];
                    var totalPixels = expectedContourProperties[startIndex + 3];
                    var contour = pointContours[contourIndex];
                    Assert.AreEqual(outerPointCount, contour.Outer.Count, $"Number of outer points for contour {contourIndex}");
                    Assert.AreEqual(innerContourCout, contour.Inner.Count, $"Number of inner contours for contour {contourIndex}");
                    Assert.AreEqual(innerPointCount, contour.Inner.Select(p=> p.Count).Sum(), $"Total number of points in inner contours for contour {contourIndex}");
                    Assert.AreEqual(totalPixels, contour.TotalPixels, $"Number of pixels for contour {contourIndex}");
                }
            }
        }
        
        private void CompareRendering(Volume2D<byte> mask, Volume2D<byte> rendered)
        {
            var mismatchMessages = new StringBuilder();
            var mismatchCount = 0;
            for (int i = 0; i < mask.Length; i++)
            {
                var (x, y) = mask.GetCoordinates(i);
                if (mask[i] != rendered[i])
                {
                    var message = $"Mismatch at index {i}, point ({x},{y}): Expected {mask[i]}, but got {rendered[i]}";
                    mismatchCount++;
                    if (mismatchCount < 20)
                    {
                        mismatchMessages.AppendLine(message);
                    }
                }
            }
            if (mismatchCount > 0)
            {
                Console.WriteLine(mismatchMessages.ToString());
                Assert.Fail($"Mask to contour to mask roundtrip had {mismatchCount} mismatches. Check console for details.");
            }
        }

        public Tuple<List<InnerOuterPolygon>, Volume2D<byte>> ExtractContoursAndPlot(Volume2D<byte> mask, 
            int maxNestingLevel, 
            ContourSmoothingType smoothingType,
            string filename)
        {
            var contours = ExtractContours.PolygonsWithHoles(mask, 1, maxNestingLevel, enableVerboseOutput: true).ToList();
            var renderingContours =
                contours
                .Select(contour => new ContourPolygon(SmoothPolygon.Smooth(contour, smoothingType), contour.TotalPixels))
                .ToList();
            var rendered = mask.CreateSameSize<byte>();
            foreach (var contour in renderingContours)
            {
                FillPolygon.Fill(contour.ContourPoints, rendered.Array, rendered.DimX, rendered.DimY, 0, 0, (byte)1);
            }
            PlotMaskAndContour(filename, mask, rendered, renderingContours.Select(c => c.ContourPoints));
            return Tuple.Create(contours, rendered);
        }

        /// <summary>
        /// Test how a single pixel horizontal line can be rendered.
        /// </summary>
        [Test]
        public void RenderSinglePixelLine()
        {
            var rendered = CreateVolume(10, 10);
            var points = new Point[]
            {
                CreatePoint(1, 0.9),
                CreatePoint(2, 0.9),
                CreatePoint(3, 0.9),
                CreatePoint(3, 1.1),
                CreatePoint(2, 1.1),
                CreatePoint(1, 1.1)
            };
            FillPolygon.Fill(points, rendered.Array, rendered.DimX, rendered.DimY, 0, 0, (byte)1);
            rendered.SaveBinaryMaskToPng(Path.Combine(SaveImageFolder, "RenderSinglePixelLine.png"));
            Assert.AreEqual(1, rendered[1, 1], "Point 1,1");
            Assert.AreEqual(1, rendered[2, 1], "Point 2,1");
            Assert.AreEqual(1, rendered[3, 1], "Point 3,1");
            Assert.AreEqual(3, rendered.Array.Where(p => p == 1).Count(), "Total number of rendered foreground points");
        }

        /// <summary>
        /// Test how a simple structure with a one pixel hole can be rendered, using the existing filling code
        /// in <see cref="FillPolygonHelpers.FillPolygon{T}(Point[], T[], int, int, int, int, T)"/>
        /// </summary>
        [Test]
        public void RenderDiamond()
        {
            void RenderAndCheck(Point[] points, string filenameSuffix)
            {
                var rendered = CreateVolume(10, 10);
                FillPolygon.Fill(points, rendered.Array, rendered.DimX, rendered.DimY, 0, 0, (byte)1);
                rendered.SaveBinaryMaskToPng(Path.Combine(SaveImageFolder, $"RenderDiamond -{filenameSuffix}.png"));
                Assert.AreEqual(1, rendered[2, 1], "Point 2, 1");
                Assert.AreEqual(1, rendered[3, 2], "Point 3, 2");
                Assert.AreEqual(1, rendered[2, 3], "Point 2, 3");
                Assert.AreEqual(1, rendered[1, 2], "Point 1, 2");
                Assert.AreEqual(4, rendered.Array.Where(p => p == 1).Count(), "Total number of rendered foreground points");
            }

            var pointsManual = new Point[]
            {
                CreatePoint(2, 0.9),
                CreatePoint(3.1, 2.0),
                CreatePoint(2, 3.1),
                CreatePoint(0.9, 2.0),
                CreatePoint(2, 0.9),
                CreatePoint(1.1, 2.0),
                CreatePoint(2, 2.9),
                CreatePoint(2.9, 2.0),
                CreatePoint(2, 1.1),
            };
            RenderAndCheck(pointsManual, "manualContour");
        }

        /// <summary>
        /// Creates a byte volume that marks all points in the given set of contours. For each contour,
        /// all points that make up the contour are set to value (index + 1), where index is the zero-based 
        /// index of the contour in the list. Points that would fall outside the image are omitted.
        /// </summary>
        /// <param name="imageWidth">The width of the volume that should be returned (X dimension)</param>
        /// <param name="imageHeight">The height of the volume that should be returned (Y dimension)</param>
        /// <param name="contours">The list of contours that should be plotted.</param>
        /// <returns></returns>
        public Volume2D<byte> PlotContourPoints(int imageWidth, int imageHeight, IEnumerable<ContourPolygon> contours)
        {
            if (contours == null)
            {
                throw new ArgumentNullException(nameof(contours));
            }

            var mask = CreateVolume(imageWidth, imageHeight);
            foreach (var item in contours.Select( (value, i) => new { i, value}))
            {
                var index = item.i;
                var contour = item.value;

                if (index < byte.MaxValue)
                {
                    foreach (var point in contour.ContourPoints)
                    {
                        var pointInt = FloatToIntPoint(point);
                        if (mask.IsValid(pointInt.X, pointInt.Y))
                        {
                            mask[pointInt.X, pointInt.Y] = (byte)(index + 1);
                        }
                    }
                }
            }

            return mask;
        }

        public static PointInt FloatToIntPoint(Point point)
        {
            var x = (int)Math.Round(point.X, MidpointRounding.AwayFromZero);
            var y = (int)Math.Round(point.Y, MidpointRounding.AwayFromZero);
            return new PointInt(x, y);
        }

        /// <summary>
        /// Tests the basic operations of merging outer with inner contours.
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="insertPosition"></param>
        /// <param name="child"></param>
        /// <param name="skipFirstChildElement"></param>
        /// <param name="connectionPoints"></param>
        /// <param name="expected"></param>
        // Inserting the child at different positions, no connecting points
        [TestCase(new int[] { 1, 2, 3 }, 0, new int[] { 4, 5 }, 0, new int[0], new int[] { 1, 4, 5, 2, 3 })]
        [TestCase(new int[] { 1, 2, 3 }, 1, new int[] { 4, 5 }, 0, new int[0], new int[] { 1, 2, 4, 5, 3 })]
        [TestCase(new int[] { 1, 2, 3 }, 2, new int[] { 4, 5 }, 0, new int[0], new int[] { 1, 2, 3, 4, 5 })]
        // Inserting the child starting at a non-zero index
        [TestCase(new int[] { 1, 2, 3 }, 1, new int[] { 4, 5 }, 1, new int[0], new int[] { 1, 2, 5, 4, 3 })]
        // Inserting the child and connection points between parent and child: Those points should be inserted
        // in reverse order when going back from child to parent
        [TestCase(new int[] { 1, 2, 3 }, 0, new int[] { 4, 5 }, 0, new int[] { 8, 9 }, new int[] { 1, 8, 9, 4, 5, 9, 8, 2, 3 })]
        [TestCase(new int[] { 1, 2, 3 }, 0, new int[] { 4, 5 }, 1, new int[] { 8, 9 }, new int[] { 1, 8, 9, 5, 4, 9, 8, 2, 3 })]
        public void ContourInsertChildIntoParent(int[] parent, int insertPosition,
            int[] child, int childStartPosition, int[] connectionPoints,
            int[] expected)
        {
            var actual = SmoothPolygon.InsertChildIntoParent(parent, insertPosition,
                child, childStartPosition, connectionPoints);
            Assert.AreEqual(expected, actual);
        }

        /// <summary>
        /// Tests whether the creation of vertical connection lines between parent (outer) and child (inner)
        /// contours works as expected.
        /// </summary>
        [TestCase(1, 0)]
        [TestCase(2, 0)]
        [TestCase(3, 1)]
        [TestCase(4, 3)]
        [TestCase(5, 6)]
        // Should hit the line between 6 and 10
        [TestCase(8, 7)]
        // Should hit the line between 10 and 8.5 (segment in reverse direction from large x to small x)
        [TestCase(9, 8)]
        public void ContourFindIntersectionsOnParent(int startX, int expectedIndex)
        {
            // Search for intersections always start at this Y coordinate.
            var startY = 10;
            // This mimics a "parent" contour, where we want to find the connection point
            // with a child contour that is right over (same x, lower y) the child starting point.
            // Testcases have different X coordinate for the child start
            var contour = new Point[]
            {
                // Should match if we search for intersections at anywhere in the range (1, 2)
                CreatePoint(1, 0),
                CreatePoint(3, 0),
                // Case where two points are identical: Searching for x = 4 should return the second of the duplicates
                CreatePoint(4, 0), // [2]
                CreatePoint(4, 0),
                // Two line segments at different y, should return the one at larger y
                CreatePoint(5, 0), // [4]
                CreatePoint(6, 0),
                CreatePoint(5, 4), // [6]
                CreatePoint(6, 4),
                // Cases where lines go from right to left (large x to small x)
                CreatePoint(10, 8), // [8]
                CreatePoint(8.5, 9),
                // Now go back to the start outside of the startPoint, to not create accidental matches with the 
                // contour closing line.
                CreatePoint(8.5, startY + 1),
                CreatePoint(1, startY + 1),
            };
            var index = SmoothPolygon.FindIntersectingPoints(contour, new PointInt(startX, startY), searchForHighestY: true);
            Assert.AreEqual(expectedIndex, index, $"Parent index for search highest Y starting at x = {startX}");
        }

        /// <summary>
        /// Tests whether the creation of vertical connection lines between parent (outer) and child (inner)
        /// contours works as expected.
        /// </summary>
        [TestCase(1, 0)]
        [TestCase(2, 0)]
        [TestCase(3, 1)]
        [TestCase(4, 3)]
        [TestCase(5, 4)]
        // Should hit the line between (6,4) and (10,8)
        [TestCase(8, 7)]
        // Should hit the line between (6,4) and (10,8)
        [TestCase(9, 7)]
        public void ContourFindIntersectionsChild(int startX, int expectedIndex)
        {
            // Search for intersections always start at this Y coordinate.
            var startY = 10;
            // This mimics a child contour, where we want to find the connection point
            // to a parent. The child contour can be smoothed already, and must not contain the starting
            // point anymore. Hence, find the topmost (lowest Y) line in the child contour that intersects
            // with the constant line X == child start point X
            var contour = new Point[]
            {
                // Should match if we search for intersections at anywhere in the range (1, 2)
                CreatePoint(1, 0),
                CreatePoint(3, 0),
                CreatePoint(4, 0), // [2]
                CreatePoint(4, 0),
                CreatePoint(5, 0), // [4]
                CreatePoint(6, 0),
                CreatePoint(5, 4), // [6]
                CreatePoint(6, 4),
                // Cases where lines go from right to left (large x to small x)
                CreatePoint(10, 8), // [8]
                CreatePoint(8.5, 9),
                // Now go back to the start outside of the startPoint, to not create accidental matches with the 
                // contour closing line.
                CreatePoint(8.5, startY + 1),
                CreatePoint(1, startY + 1),
            };
            var index = SmoothPolygon.FindIntersectingPoints(contour, new PointInt(startX, startY), searchForHighestY: false);
            Assert.AreEqual(expectedIndex, index, $"Contour index for search lowest Y starting at x = {startX}");
        }

        [Test]
        public void ContourFindIntersections2()
        {
            var contour = new Point[]
            {
                CreatePoint(1, 0.5),
                CreatePoint(1, 0.5),
                CreatePoint(2, -0.5),
                CreatePoint(2, -0.5),
                CreatePoint(3, 0.5),
                CreatePoint(3, 1.5),
                CreatePoint(2, 4.5),
                CreatePoint(2, 4.5),
                CreatePoint(1, 3.5),
                CreatePoint(1, 3.5),
            };
            var startX = 2;
            var index = SmoothPolygon.FindIntersectingPoints(contour, new PointInt(startX, 0), searchForHighestY: true);
            Assert.AreEqual(3, index);
        }

        [TestCase(1.5, -0.5, 2.5, -0.5, 2, -0.5)]
        [TestCase(2.5, -0.5, 1.5, -0.5, 2, -0.5)]
        [TestCase(2.4, 1.5, 1.5, 2.4, 2, 1.9)]
        public void ContourIntersectLine(double x1, double y1, double x2, double y2, int x, double expectedY)
        {
            var p1 = CreatePoint(x1, y1);
            var p2 = CreatePoint(x2, y2);
            var expected = CreatePoint(x, expectedY);
            var result = SmoothPolygon.IntersectLineAtX(p1, p2, x);
            Assert.AreEqual(expected.X, result.X, 1e-6, "X");
            Assert.AreEqual(expected.Y, result.Y, 1e-6, "Y");
        }

        public void PlotMaskAndContour(string fileName, Volume2D<byte> mask, Volume2D<byte> renderedMask, IEnumerable<IEnumerable<Point>> contours, RGBColor color = null)
        {
            color = color ?? new RGBColor(0xFF, 0, 0);
            PlotMaskAndContour(fileName, mask, renderedMask, contours.Select(contour => Tuple.Create(contour, color)));
        }

        public void PlotMaskAndContour(string fileName, Volume2D<byte> mask, Volume2D<byte> renderedMask, IEnumerable<Point> points, RGBColor color = null)
        {
            color = color ?? new RGBColor(0xFF, 0, 0);
            PlotMaskAndContour(fileName, mask, renderedMask, new[] { Tuple.Create(points, color) });
        }

        /// <summary>
        /// Creates a PNG image file that contains a visualization of a binary mask and a set of contours.
        /// The PNG will be magnified, to allow plotting a contour at sub-pixel accuracy.
        /// The mask foreground voxels will be plotted as small squares, centered at integer coordinates.
        /// The contours will be plotted and filled, with indicators for start and end point.
        /// Also saves a corresponding text description.
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="mask"></param>
        /// <param name="contours"></param>
        public void PlotMaskAndContour(string fileName, 
            Volume2D<byte> mask,
            Volume2D<byte> renderedMask,
            IEnumerable<Tuple<IEnumerable<Point>,RGBColor>> contoursAndColors)
        {
            var stringBuilder = new StringBuilder();

            contoursAndColors = contoursAndColors ?? new Tuple<IEnumerable<Point>, RGBColor>[0];
            var contours =
                contoursAndColors
                .Select(c => Tuple.Create(c.Item1.ToList(), c.Item2))
                .ToList();
            // create visualization this many times bigger than pixel scale
            var scale = 20;
            // Add 20 voxels on each side outside what would normally be drawing range for the mask.
            var offset = scale;
            var pixelSizeHalf = 5;
            var pointSizeHalf = 2;
            var tickLengthHalf = 2;
            var tickStep = mask.DimX > 30 || mask.DimY > 30 ? 10 : 5;
            int scaleCoordinate(int x) => offset + x * scale + scale / 2; 
            float scaleCoordinateF(float x) => offset + x * scale + scale / 2;
            using (var larger = new Bitmap(mask.DimX * scale + 2 * offset, mask.DimY * scale + 2 * offset, PixelFormat.Format24bppRgb))
            {
                var maskColor = Color.FromArgb(127, Color.Green);
                var renderedMaskColor = Color.FromArgb(127, Color.Gray);
                var contourColor = Color.Red;
                var contourFillingColor = Color.FromArgb(60, contourColor);
                using (var g = Graphics.FromImage(larger))
                using (var maskBrush = new SolidBrush(maskColor))
                using (var renderedMaskBrush = new SolidBrush(renderedMaskColor))
                using (var tickPen = new Pen(Color.Black))
                using (var tickBrush = new SolidBrush(Color.Black))
                using (var tickFont = new Font("Arial", 10))
                {
                    g.Clear(Color.White);
                    g.CompositingQuality = CompositingQuality.HighSpeed;
                    g.SmoothingMode = SmoothingMode.HighSpeed;
                    g.InterpolationMode = InterpolationMode.NearestNeighbor;
                    var sf = new StringFormat
                    {
                        LineAlignment = StringAlignment.Center,
                        Alignment = StringAlignment.Center
                    };
                    for (var tick = 0; tick < mask.DimX; tick += tickStep)
                    {
                        var x = scaleCoordinateF(tick);
                        var y = scaleCoordinateF(0);
                        g.DrawLine(tickPen, x, y - tickLengthHalf, x, y + tickLengthHalf);

                        stringBuilder.AppendLine(string.Format(
                            "g.DrawLine(tickPen, {0}, {1}, {2}, {3});",
                            x, y - tickLengthHalf, x, y + tickLengthHalf));

                        g.DrawString(tick.ToString(), tickFont, tickBrush, x, scaleCoordinateF(-0.5f), sf);

                        stringBuilder.AppendLine(string.Format(
                            "g.DrawString({0}, tickFont, tickBrush, {1}, {2}, sf);",
                            tick.ToString(), x, scaleCoordinateF(-0.5f)));
                    }

                    for (var tick = 0; tick < mask.DimY; tick += tickStep)
                    {
                        var y = scaleCoordinateF(tick);
                        var x = scaleCoordinateF(0);
                        g.DrawLine(tickPen, x - tickLengthHalf, y, x + tickLengthHalf, y);

                        stringBuilder.AppendLine(string.Format(
                            "g.DrawLine(tickPen, {0}, {1}, {2}, {3});",
                            x - tickLengthHalf, y, x + tickLengthHalf, y));

                        g.DrawString(tick.ToString(), tickFont, tickBrush, scaleCoordinateF(-0.5f), y, sf);

                        stringBuilder.AppendLine(string.Format(
                            "g.DrawString({0}, tickFont, tickBrush, {1}, {2}, sf);",
                            tick.ToString(), scaleCoordinateF(-0.5f), y));
                    }

                    foreach (var index in Enumerable.Range(0, mask.Length))
                    {
                        var (x, y) = mask.GetCoordinates(index);
                        if (mask[index] > 0)
                        {
                            g.FillRectangle(maskBrush, scaleCoordinate(x) - pixelSizeHalf, scaleCoordinate(y) - pixelSizeHalf,
                                pixelSizeHalf * 2, pixelSizeHalf * 2);

                            stringBuilder.AppendLine(string.Format(
                                "g.FillRectangle(maskBrush, {0}, {1}, {2}, {3});",
                                scaleCoordinate(x) - pixelSizeHalf, scaleCoordinate(y) - pixelSizeHalf,
                                pixelSizeHalf * 2, pixelSizeHalf * 2));
                        }

                        if (renderedMask != null && renderedMask[index] > 0)
                        {
                            g.FillRectangle(renderedMaskBrush, scaleCoordinate(x) - pointSizeHalf, scaleCoordinate(y) - pointSizeHalf,
                                pointSizeHalf * 2, pointSizeHalf * 2);

                            stringBuilder.AppendLine(string.Format(
                                "g.FillRectangle(renderedMaskBrush, {0}, {1}, {2}, {3});",
                                scaleCoordinate(x) - pointSizeHalf, scaleCoordinate(y) - pointSizeHalf,
                                pointSizeHalf * 2, pointSizeHalf * 2));
                        }
                    }

                    foreach (var (contour, color) in contours)
                    {
                        var fillingColor = Color.FromArgb(60, color.R, color.G, color.B);
                        using (var contourPen = new Pen(Color.FromArgb(color.R, color.G, color.B), 1.0f))
                        using (var contourFilling = new SolidBrush(contourFillingColor))
                        {
                            var scaled =
                                contour
                                .Select(point => new System.Drawing.PointF(scaleCoordinateF(point.X), scaleCoordinateF(point.Y)))
                                .ToArray();
                            var pointTypes = scaled.Select(_ => (byte)PathPointType.Line).ToArray();
                            var path = new GraphicsPath(scaled, pointTypes, FillMode.Alternate);

                            var scaledString = string.Format("[{0}]", string.Join(",", scaled.Select(s => string.Format("({0}, {1})", s.X, s.Y))));
                            var pointTypesString = string.Format("[{0}]", string.Join(",", pointTypes.Select(p => p.ToString())));

                            stringBuilder.AppendLine(string.Format(
                                "var path = new GraphicsPath({0}, {1}, FillMode.Alternate);",
                                scaledString,
                                pointTypesString));

                            g.FillPath(contourFilling, path);

                            stringBuilder.AppendLine("g.FillPath(contourFilling, path);");

                            g.DrawPath(contourPen, path);

                            stringBuilder.AppendLine("g.DrawPath(contourPen, path);");

                            g.DrawRectangle(contourPen,
                                scaled[0].X - pointSizeHalf,
                                scaled[0].Y - pointSizeHalf,
                                pointSizeHalf * 2,
                                pointSizeHalf * 2);

                            stringBuilder.AppendLine(string.Format(
                                "g.DrawRectangle(contourPen, {0}, {1}, {2}, {3});",
                                scaled[0].X - pointSizeHalf,
                                scaled[0].Y - pointSizeHalf,
                                pointSizeHalf * 2,
                                pointSizeHalf * 2));

                            g.DrawEllipse(contourPen,
                                scaled[scaled.Length - 1].X - pointSizeHalf,
                                scaled[scaled.Length - 1].Y - pointSizeHalf,
                                pointSizeHalf * 2,
                                pointSizeHalf * 2);

                            stringBuilder.AppendLine(string.Format(
                                "g.DrawEllipse(contourPen, {0}, {1}, {2}, {3});",
                                scaled[scaled.Length - 1].X - pointSizeHalf,
                                scaled[scaled.Length - 1].Y - pointSizeHalf,
                                pointSizeHalf * 2,
                                pointSizeHalf * 2));
                        }
                    }
                }

                Console.WriteLine($"Saving visualization to {fileName}");
                larger.Save(Path.Combine(SaveImageFolder, fileName));
            }
            var textFilename = Path.ChangeExtension(Path.Combine(SaveImageFolder, fileName), "txt");
            Console.WriteLine($"Saving text description to {fileName}");
            File.WriteAllText(textFilename, stringBuilder.ToString());
        }
    }
}
