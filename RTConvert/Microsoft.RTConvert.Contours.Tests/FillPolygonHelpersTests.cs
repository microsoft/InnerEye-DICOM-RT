///  ------------------------------------------------------------------------------------------
///  Copyright (c) Microsoft Corporation. All rights reserved.
///  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
///  ------------------------------------------------------------------------------------------

namespace Microsoft.RTConvert.Contours.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.RTConvert.Contours;
    using Microsoft.RTConvert.Models;
    using NUnit.Framework;

    [TestFixture]
    public class FillPolygonTests
    {
        /// <summary>
        /// Creates a new point from the arguments, using a cast from double to float.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public PointF CreatePoint(double x, double y) => new PointF((float)x, (float)y);

        [Test]
        public void TestPointNearLine()
        {
            Assert.IsFalse(FillPolygon.PointOnLine(CreatePoint(1.5, 1.5), CreatePoint(1.5, 1.5), CreatePoint(2.0, 2.0), 0.01f));
            Assert.IsTrue(FillPolygon.PointOnLine(CreatePoint(1.5, 1.5), CreatePoint(1.5, 1.5), CreatePoint(1.5, 1.5), 0.01f));
            Assert.IsTrue(FillPolygon.PointOnLine(CreatePoint(1.5, 1.5), CreatePoint(1.5, 1.5), CreatePoint(1.5001, 1.5001), 0.01f));

            // On line to float precision
            Assert.IsTrue(FillPolygon.PointOnLine(CreatePoint(1, 1), CreatePoint(2, 2), CreatePoint(1.5f, 1.5f), 0.01f));
            Assert.IsTrue(FillPolygon.PointOnLine(CreatePoint(2, 2), CreatePoint(1, 1), CreatePoint(1.5f, 1.5f), 0.01f));

            // Near line within tolerance
            Assert.IsTrue(FillPolygon.PointOnLine(CreatePoint(1, 1), CreatePoint(2, 2), CreatePoint(1.50f, 1.55f), 0.1f));
            Assert.IsTrue(FillPolygon.PointOnLine(CreatePoint(1, 1), CreatePoint(2, 2), CreatePoint(1.50f, 1.45f), 0.1f));

            // Near line but outside tolerance
            Assert.IsFalse(FillPolygon.PointOnLine(CreatePoint(1, 1), CreatePoint(2, 2), CreatePoint(1.50f, 1.55f), 0.01f));
            Assert.IsFalse(FillPolygon.PointOnLine(CreatePoint(1, 1), CreatePoint(2, 2), CreatePoint(1.50f, 1.45f), 0.01f));

            // On line but just past end with different accuracy
            Assert.IsTrue(FillPolygon.PointOnLine(CreatePoint(1, 1), CreatePoint(2, 2), CreatePoint(2.001f, 2.001f), 0.1f));
            Assert.IsTrue(FillPolygon.PointOnLine(CreatePoint(1, 1), CreatePoint(2, 2), CreatePoint(0.999f, 0.999f), 0.1f));
            Assert.IsFalse(FillPolygon.PointOnLine(CreatePoint(1, 1), CreatePoint(2, 2), CreatePoint(2.001f, 2.001f), 0.0001f));
            Assert.IsFalse(FillPolygon.PointOnLine(CreatePoint(1, 1), CreatePoint(2, 2), CreatePoint(0.999f, 0.999f), 0.0001f));

            Assert.IsTrue(FillPolygon.PointOnLine(CreatePoint(1, 1), CreatePoint(2, 1), CreatePoint(1.5f, 1.01f), 0.05f));
            Assert.IsTrue(FillPolygon.PointOnLine(CreatePoint(1, 1), CreatePoint(1, 2), CreatePoint(1.0f, 1.01f), 0.05f));
        }


        [Test]
        public void GetBoundingBoxTest()
        {
            var boundingBox = FillPolygon.GetBoundingBox(new[] { CreatePoint(1, 1), CreatePoint(2, 4) });

            Assert.AreEqual(3, boundingBox.Height);
            Assert.AreEqual(1, boundingBox.Width);
            Assert.AreEqual(1, boundingBox.X);
            Assert.AreEqual(1, boundingBox.Y);

            boundingBox = FillPolygon.GetBoundingBox(new[] {
                CreatePoint(1,1),
                CreatePoint(2,1),
                CreatePoint(2,2),
                CreatePoint(3,2),
                CreatePoint(3,1),
                CreatePoint(4,1),
                CreatePoint(2,7),
            });

            Assert.AreEqual(6, boundingBox.Height);
            Assert.AreEqual(3, boundingBox.Width);
            Assert.AreEqual(1, boundingBox.X);
            Assert.AreEqual(1, boundingBox.Y);

            boundingBox = FillPolygon.GetBoundingBox(new[] {
                CreatePoint(2, 1),
                CreatePoint(1, 2),
                CreatePoint(3, 2),
            });

            Assert.AreEqual(1, boundingBox.Height);
            Assert.AreEqual(2, boundingBox.Width);
            Assert.AreEqual(1, boundingBox.X);
            Assert.AreEqual(1, boundingBox.Y);
        }

        [Test]
        public void FillPolygonTests2()
        {
            var dimX = 5;
            var dimY = 5;

            var polygon1 = new[] {
                CreatePoint(-0.5, -0.5),
                CreatePoint(dimX + 0.5, -0.5),
                CreatePoint(dimX + 0.5,dimY + 0.5),
                CreatePoint(-0.5, dimY + 0.5)
            };

            var output1 = new byte[dimX * dimY];

            FillPolygon.Fill(polygon1, output1, dimX, dimY, 0, 0, (byte)1);

            for (var i = 0; i < output1.Length; i++)
            {
                Assert.AreEqual(1, output1[i]);
            }

            var polygon = new[] {
                CreatePoint(0.1, 0.1),
                CreatePoint(dimX - 1.1, 0.1),
                CreatePoint(dimX - 1.1,dimY - 1.1),
                CreatePoint(0.1, dimY - 1.1)
            };

            var output = new byte[dimX * dimY];

            FillPolygon.Fill(polygon, output, dimX, dimY, 0, 0, (byte)1);

            for (var y = 0; y < dimY; y++)
            {
                for (var x = 0; x < dimX; x++)
                {
                    Assert.AreEqual(y == 0 || y == dimY - 1 || x == 0 || x == dimX - 1 ? 0 : 1, output[x + y * dimX]);
                }
            }
        }

        [Test]
        public void FillPolygonOutsideVolume()
        {
            var dimX = 5;
            var dimY = 5;

            var polygon1 = new[] {
                CreatePoint(-10.5, -10.5), // Check that if a point is outside the volume this code still works
                CreatePoint(dimX + 0.5, -0.5),
                CreatePoint(dimX + 0.5,dimY + 0.5),
                CreatePoint(-0.5, dimY + 0.5)
            };

            var output1 = new byte[dimX * dimY];

            FillPolygon.Fill(polygon1, output1, dimX, dimY, 0, 0, (byte)1);

            for (var i = 0; i < output1.Length; i++)
            {
                Assert.AreEqual(1, output1[i]);
            }

            var polygon = new[] {
                CreatePoint(0.1,0.1),
                CreatePoint(dimX - 1.1, 0.1),
                CreatePoint(dimX - 1.1,dimY - 1.1),
                CreatePoint(0.1, dimY - 1.1)
            };

            var output = new byte[dimX * dimY];

            FillPolygon.Fill(polygon, output, dimX, dimY, 0, 0, (byte)1);

            for (var y = 0; y < dimY; y++)
            {
                for (var x = 0; x < dimX; x++)
                {
                    Assert.AreEqual(y == 0 || y == dimY - 1 || x == 0 || x == dimX - 1 ? 0 : 1, output[x + y * dimX]);
                }
            }
        }

        [Test]
        public void FillPolygonTests4()
        {
            var dimX = 5;
            var dimY = 5;

            var expectedArray = new byte[]
            {
                0, 0, 1, 0, 0,
                0, 1, 1, 1, 0,
                1, 1, 1, 1, 1,
                0, 1, 1, 1, 0,
                0, 0, 1, 0, 0,
            };

            var volume2D = new Volume2D<byte>(expectedArray, 5, 5, 1, 1, new Point2D(), new Matrix2());
            var extractContour = ExtractContours.ContoursWithHoles(volume2D);

            var output1 = new byte[dimX * dimY];
            var polygon = extractContour.First().ContourPoints.Select(x => CreatePoint(x.X + 0.00002, x.Y + 0.00001)).ToArray();

            FillPolygon.Fill(polygon, output1, dimX, dimY, 0, 0, (byte)1);

            for (var i = 0; i < output1.Length; i++)
            {
                Assert.AreEqual(expectedArray[i], output1[i]);
            }
        }

        [Test]
        public void FillPolygonTests5()
        {
            var dimX = 5;
            var dimY = 5;

            var expectedArray = new byte[]
            {
                0, 0, 0, 0, 0,
                0, 1, 0, 1, 0,
                1, 1, 1, 1, 1,
                0, 1, 1, 1, 0,
                0, 0, 1, 0, 0,
            };

            var volume2D = new Volume2D<byte>(expectedArray, 5, 5, 1, 1, new Point2D(), new Matrix2());

            var extractContour = ExtractContours.PolygonsFilled(volume2D);

            var output1 = new byte[dimX * dimY];
            var polygon = extractContour.First().Points.Select(x => CreatePoint(x.X + 0.00002, x.Y + 0.00001)).ToArray();

            FillPolygon.Fill(polygon, output1, dimX, dimY, 0, 0, (byte)1);

            for (var i = 0; i < output1.Length; i++)
            {
                Assert.AreEqual(expectedArray[i], output1[i]);
            }
        }

        [Timeout(60 * 1000)]
        [Test]
        public void FillPolygonTestsRandomCheckTermination()
        {
            var dimX = 2000;

            int seed = Guid.NewGuid().GetHashCode();
            Console.WriteLine($"Seed {seed}");
            var random = new Random(seed);


            var byteArray = Enumerable.Range(0, dimX * dimX).Select(_ => (byte)random.Next(0, 2)).ToArray();

            var volume2D = new Volume2D<byte>(byteArray, dimX, dimX, 1, 1, new Point2D(), new Matrix2());

            var extractContour = ExtractContours.PolygonsFilled(volume2D);

            Console.WriteLine($"Contours count {extractContour.Count}");

            Assert.IsTrue(extractContour.Count > 0);

        }

        [Test]
        public void FillPolygonTests6()
        {
            var dimX = 5;
            var dimY = 5;

            var expectedArray = new byte[]
            {
                0, 1, 1, 0, 0,
                1, 1, 1, 1, 0,
                1, 1, 0, 1, 1,
                1, 1, 0, 1, 0,
                0, 0, 0, 0, 0,
            };

            var volume2D = new Volume2D<byte>(expectedArray, 5, 5, 1, 1, new Point2D(), new Matrix2());

            var extractContour = ExtractContours.PolygonsFilled(volume2D);

            var output1 = new byte[dimX * dimY];
            var polygon = extractContour.First().Points.Select(x => CreatePoint(x.X + 0.00002, x.Y - 0.00001)).ToArray();

            FillPolygon.Fill(polygon, output1, dimX, dimY, 0, 0, (byte)1);

            for (var i = 0; i < output1.Length; i++)
            {
                Assert.AreEqual(expectedArray[i], output1[i]);
            }
        }

        [Test]
        public void FillPolygonTests7()
        {
            var dimX = 5;
            var dimY = 5;

            var expectedArray = new byte[]
            {
                0, 1, 1, 0, 0,
                1, 1, 1, 1, 0,
                1, 1, 1, 1, 1,
                1, 1, 0, 1, 0,
                0, 0, 0, 0, 0,
            };

            var volume2D = new Volume2D<byte>(expectedArray, 5, 5, 1, 1, new Point2D(), new Matrix2());

            var extractContour = ExtractContours.PolygonsFilled(volume2D);

            var output1 = new byte[dimX * dimY];
            var polygon = extractContour.First().Points.Select(x => CreatePoint(x.X + 0.00002, x.Y - 0.00001)).ToArray();

            FillPolygon.Fill(polygon, output1, dimX, dimY, 0, 0, (byte)1);

            for (var i = 0; i < output1.Length; i++)
            {
                Assert.AreEqual(expectedArray[i], output1[i]);
            }
        }

        [Test]
        public void FillPolygonTests3()
        {
            var polygon = new[] {
                CreatePoint(0.994356,1.00136305698),
                CreatePoint(3.0002425,0.99924525),
                CreatePoint(2.999235236,2.999252346),
                CreatePoint(1.00135357,3.001232363)
            };

            var boundingBox = FillPolygon.GetBoundingBox(polygon);

            Assert.AreEqual(0, FillPolygon.PointInComplexPolygon(polygon, CreatePoint(1, 1), boundingBox));
            Assert.AreEqual(0, FillPolygon.PointInComplexPolygon(polygon, CreatePoint(2, 1), boundingBox));
            Assert.AreEqual(0, FillPolygon.PointInComplexPolygon(polygon, CreatePoint(3, 1), boundingBox));
            Assert.AreEqual(0, FillPolygon.PointInComplexPolygon(polygon, CreatePoint(3, 2), boundingBox));
            Assert.AreEqual(0, FillPolygon.PointInComplexPolygon(polygon, CreatePoint(3, 3), boundingBox));
            Assert.AreEqual(0, FillPolygon.PointInComplexPolygon(polygon, CreatePoint(2, 3), boundingBox));
            Assert.AreEqual(0, FillPolygon.PointInComplexPolygon(polygon, CreatePoint(1, 3), boundingBox));
            Assert.AreEqual(0, FillPolygon.PointInComplexPolygon(polygon, CreatePoint(1, 2), boundingBox));
        }

        [Test]
        public void FillPolygonTests1()
        {
            // Figure of 8
            var polygon = new[] {
                CreatePoint(2,1),
                CreatePoint(3,2),
                CreatePoint(3,6),
                CreatePoint(3,6), // Double point - interesting case
                CreatePoint(5,6),
                CreatePoint(5,3),
                CreatePoint(2,4),
                CreatePoint(1,2),
                CreatePoint(2,1),
            };

            polygon = polygon.Select(x => CreatePoint(x.X - 0.5, x.Y - 0.5)).ToArray();

            var dimX = 6;
            var dimY = 7;

            var output = new byte[dimX * dimY];

            FillPolygon.Fill(polygon, output, dimX, dimY, 0, 0, (byte)1);

            var resultArray = new byte[]
            {
                0, 0, 0, 0, 0, 0,
                0, 1, 1, 0, 0, 0,
                0, 1, 1, 0, 0, 0,
                0, 0, 1, 1, 1, 0,
                0, 0, 0, 1, 1, 0,
                0, 0, 0, 1, 1, 0,
                0, 0, 0, 0, 0, 0,
            };

            Assert.AreEqual(resultArray.Length, output.Length);

            for (var i = 0; i < output.Length; i++)
            {
                Assert.AreEqual(resultArray[i], output[i]);
            }

            polygon = new[] {
                CreatePoint(0,0),
                CreatePoint(dimX,0),
                CreatePoint(dimX,dimY),
                CreatePoint(0,dimY),
                CreatePoint(0,0),
            };

            polygon = polygon.Select(x => CreatePoint(x.X - 0.5, x.Y - 0.5)).ToArray();

            FillPolygon.Fill(polygon, output, dimX, dimY, 0, 0, (byte)1);

            for (var i = 0; i < output.Length; i++)
            {
                Assert.AreEqual(1, output[i]);
            }
        }

        [Test]
        public void PointInPolygonTests()
        {
            var polygon = new[] {
                CreatePoint(1,1),
                CreatePoint(10,1),
                CreatePoint(10,10),
                CreatePoint(1,10), // Non repeating last value
            };

            var bounds = FillPolygon.GetBoundingBox(polygon);

            Assert.AreEqual(0, FillPolygon.PointInComplexPolygon(polygon, CreatePoint(1, 5), bounds)); // On the last point pair
            Assert.AreEqual(0, FillPolygon.PointInComplexPolygon(polygon, CreatePoint(5, 10), bounds));
            Assert.AreEqual(1, FillPolygon.PointInComplexPolygon(polygon, CreatePoint(5, 5), bounds));

            polygon = new[] {
                CreatePoint(1,1),
                CreatePoint(10,1),
                CreatePoint(10,10),
                CreatePoint(9,10),
                CreatePoint(8,10),
                CreatePoint(7,10),
                CreatePoint(6,10),
                CreatePoint(5,10),
                CreatePoint(4,10),
                CreatePoint(3,10),
                CreatePoint(2,10),
                CreatePoint(1,10),
                CreatePoint(1,9),
                CreatePoint(1,8),
                CreatePoint(1,7),
                CreatePoint(1,3),
                CreatePoint(1,2),
            };

            Assert.AreEqual(1, FillPolygon.PointInComplexPolygon(polygon, CreatePoint(5, 5)));

            polygon = new[] {
                CreatePoint(1,1),
                CreatePoint(1,1),
            };

            Assert.AreEqual(0, FillPolygon.PointInComplexPolygon(polygon, CreatePoint(1, 1)));
            Assert.AreEqual(0, FillPolygon.PointInComplexPolygon(polygon, CreatePoint(1.0001, 1.00001)));

            // Check all surrounding points
            Assert.AreEqual(-1, FillPolygon.PointInComplexPolygon(polygon, CreatePoint(0, 0)));
            Assert.AreEqual(-1, FillPolygon.PointInComplexPolygon(polygon, CreatePoint(1, 0)));
            Assert.AreEqual(-1, FillPolygon.PointInComplexPolygon(polygon, CreatePoint(2, 0)));
            Assert.AreEqual(-1, FillPolygon.PointInComplexPolygon(polygon, CreatePoint(2, 1)));
            Assert.AreEqual(-1, FillPolygon.PointInComplexPolygon(polygon, CreatePoint(2, 2)));
            Assert.AreEqual(-1, FillPolygon.PointInComplexPolygon(polygon, CreatePoint(1, 2)));
            Assert.AreEqual(-1, FillPolygon.PointInComplexPolygon(polygon, CreatePoint(0, 2)));
            Assert.AreEqual(-1, FillPolygon.PointInComplexPolygon(polygon, CreatePoint(0, 1)));

            polygon = new[] {
                CreatePoint(1,1),
                CreatePoint(2,1),
                CreatePoint(2,2),
                CreatePoint(3,2),
                CreatePoint(4,2),
                CreatePoint(4,1),
                CreatePoint(5,1),
                CreatePoint(5,4),
                CreatePoint(3,3),
                CreatePoint(1,4),
                CreatePoint(1,1),
            };

            bounds = FillPolygon.GetBoundingBox(polygon);

            Assert.AreEqual(0, FillPolygon.PointInComplexPolygon(polygon, CreatePoint(1, 1), bounds));
            Assert.AreEqual(-1, FillPolygon.PointInComplexPolygon(polygon, CreatePoint(3.5, 1.5), bounds));
            Assert.AreEqual(-1, FillPolygon.PointInComplexPolygon(polygon, CreatePoint(2, 4), bounds));
            Assert.AreEqual(0, FillPolygon.PointInComplexPolygon(polygon, CreatePoint(2, 1), bounds));

            polygon = new[] {
                CreatePoint(1,1),
                CreatePoint(1,2),
                CreatePoint(2,2),
                CreatePoint(1,1),
            };

            Assert.AreEqual(0, FillPolygon.PointInComplexPolygon(polygon, CreatePoint(1.5, 1.5)));
            Assert.AreEqual(0, FillPolygon.PointInComplexPolygon(polygon, CreatePoint(1.6, 1.6)));
            Assert.AreEqual(-1, FillPolygon.PointInComplexPolygon(polygon, CreatePoint(1.6, 1.5)));

            // Figure of 8
            polygon = new[] {
                CreatePoint(2,1),
                CreatePoint(3,2),
                CreatePoint(3,6),
                CreatePoint(3,6), // Double point - interesting case
                CreatePoint(5,6),
                CreatePoint(5,3),
                CreatePoint(2,4),
                CreatePoint(1,2),
                CreatePoint(2,1),
            };

            Assert.AreEqual(1, FillPolygon.PointInComplexPolygon(polygon, CreatePoint(2, 3)));
            Assert.AreEqual(1, FillPolygon.PointInComplexPolygon(polygon, CreatePoint(4, 4)));
            Assert.AreEqual(-1, FillPolygon.PointInComplexPolygon(polygon, CreatePoint(4, 2)));
        }

        [Test]
        public void GetPointsOnDiagonalLineTests()
        {
            var point1 = CreatePoint(0, 0);
            var point2 = CreatePoint(2, 4);

            var result1 = FillPolygon.GetPointsOnLine(point1, point2);

            CompareArrays(result1, new List<Point>()
            {
                new Point(0,0),
                new Point(0,1),
                new Point(1,2),
                new Point(1,3),
                new Point(2,4)
            });

            var result2 = FillPolygon.GetPointsOnLine(point2, point1);

            CompareArrays(result2, new List<Point>()
            {
                new Point(0,0),
                new Point(0,1),
                new Point(1,2),
                new Point(1,3),
                new Point(2,4)
            });
        }

        [Test]
        public void GetPointsOnStraightLineTests()
        {
            var point1 = CreatePoint(1, 1);
            var point2 = CreatePoint(4, 1);

            CompareArrays(FillPolygon.GetPointsOnLine(point1, point2),
                new List<Point>()
            {
                new Point(1,1),
                new Point(2,1),
                new Point(3,1),
                new Point(4,1),
            });

            CompareArrays(FillPolygon.GetPointsOnLine(point2, point1),
                new List<Point>()
            {
                new Point(1,1),
                new Point(2,1),
                new Point(3,1),
                new Point(4,1),
            });

        }

        private void CompareArrays(IReadOnlyList<Point> actual, IReadOnlyList<Point> expected)
        {
            Assert.AreEqual(actual.Count, expected.Count);

            for (var i = 0; i < actual.Count; i++)
            {
                var actualPoint = actual[i];
                var expectedPoint = expected[i];

                Assert.AreEqual(expectedPoint.X, actualPoint.X);
                Assert.AreEqual(expectedPoint.Y, actualPoint.Y);
            }
        }
    }
}