///  ------------------------------------------------------------------------------------------
///  Copyright (c) Microsoft Corporation. All rights reserved.
///  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
///  ------------------------------------------------------------------------------------------

namespace Microsoft.RTConvert.MedIO.Tests.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Drawing.Imaging;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Threading.Tasks;
    using Microsoft.RTConvert.Models;

    public static class TestVolumeExtensions
    {
        /// <summary>
        /// Convert a volume to an image. Voxels with default value (usually 0) become white, everything
        /// else becomes black.
        /// </summary>
        /// <typeparam name="T">Volume data type.</typeparam>
        /// <param name="volume">Volume.</param>
        /// <returns>Image.</returns>
        public static Image ToImage<T>(this Volume2D<T> volume)
        {
            var result = new Bitmap(volume.DimX, volume.DimY);

            for (var y = 0; y < volume.DimY; y++)
            {
                for (var x = 0; x < volume.DimX; x++)
                {
                    var colorValue = volume[x, y].Equals(default(T)) ? 255 : 0;
                    result.SetPixel(x, y, Color.FromArgb(colorValue, colorValue, colorValue));
                }
            }

            return result;
        }

        public static Volume2D<byte> ToVolume(this Bitmap image)
        {
            return new Volume2D<byte>(image.ToByteArray(), image.Width, image.Height, 1, 1, new Point2D(), Matrix2.CreateIdentity());
        }

        /// <summary>
        /// Creates a PNG image file from the given volume. Specific voxel values in the volume are mapped
        /// to fixed colors in the PNG file, as per the given mapping.
        /// </summary>
        /// <param name="mask"></param>
        /// <param name="filePath"></param>
        /// <param name="voxelMapping"></param>
        public static void SaveVolumeToPng(this Volume2D<byte> mask, string filePath,
            IDictionary<byte, Color> voxelMapping,
            Color? defaultColor = null)
        {
            var width = mask.DimX;
            var height = mask.DimY;

            var image = new Bitmap(width, height);

            CreateFolderStructureIfNotExists(filePath);
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var maskValue = mask[x, y];
                    if (!voxelMapping.TryGetValue(maskValue, out var color))
                    {
                        color = defaultColor ?? throw new ArgumentException($"The voxel-to-color mapping does not contain an entry for value {maskValue} found at point ({x}, {y}), and no default color is set.", nameof(voxelMapping));
                    }

                    image.SetPixel(x, y, color);
                }
            }

            image.Save(filePath);
        }

        /// <summary>
        /// Creates a PNG image file from the given binary mask. Value 0 is plotted as white, 
        /// value 1 as black. If the mask contains other values, an exception is thrown.
        /// </summary>
        /// <param name="brushVolume"></param>
        /// <param name="filePath"></param>
        public static void SaveBinaryMaskToPng(this Volume2D<byte> mask, string filePath)
        {
            var voxelMapping = new Dictionary<byte, Color>
            {
                {0, Color.White },
                {1, Color.Black }
            };
            SaveVolumeToPng(mask, filePath, voxelMapping);
        }

        /// <summary>
        /// Creates a PNG image file from the given volume. Specific voxel values in the volume are mapped
        /// to fixed colors in the PNG file:
        /// Background (value 0) is plotted in Red
        /// Foreground (value 1) is Green
        /// Value 2 is Orange
        /// Value 3 is MediumAquamarine
        /// All other voxel values are plotted in Blue.
        /// </summary>
        /// <param name="brushVolume"></param>
        /// <param name="filePath"></param>
        public static void SaveBrushVolumeToPng(this Volume2D<byte> brushVolume, string filePath)
        {
            const byte fg = 1;
            const byte bg = 0;
            const byte bfg = 3;
            const byte bbg = 2;
            var voxelMapping = new Dictionary<byte, Color>
            {
                { fg, Color.Green },
                { bfg, Color.MediumAquamarine },
                { bg, Color.Red },
                { bbg, Color.Orange }
            };
            SaveVolumeToPng(brushVolume, filePath, voxelMapping, Color.Blue);
        }

        // DO NOT USE ONLY FOR DEBUGGING PNGS
        private static Tuple<float, float> MinMaxFloat(float[] volume)
        {
            var max = float.MinValue;
            var min = float.MaxValue;

            for (var i = 0; i < volume.Length; i++)
            {
                var value = volume[i];

                if (Math.Abs(value - short.MinValue) < 1 || Math.Abs(value - short.MaxValue) < 1)
                {
                    continue;
                }

                if (max < value)
                {
                    max = value;
                }

                if (min > value)
                {
                    min = value;
                }
            }

            return Tuple.Create(min, max);
        }


        public static void SaveDistanceVolumeToPng(this Volume2D<float> distanceVolume, string filePath)
        {
            var width = distanceVolume.DimX;
            var height = distanceVolume.DimY;
            var image = new Bitmap(distanceVolume.DimX, distanceVolume.DimY);

            CreateFolderStructureIfNotExists(filePath);

            var minMax = MinMaxFloat(distanceVolume.Array);

            var minimum = minMax.Item1;
            var maximum = minMax.Item2;
            float extval = Math.Min(Math.Min(Math.Abs(minimum), maximum), 3000);

            if (minimum >= 0)
            {
                extval = maximum;
            }
            else if (maximum <= 0)
            {
                extval = Math.Abs(minimum);
            }

            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var currentDistanceValue = distanceVolume[x, y];

                    if (currentDistanceValue < -extval) currentDistanceValue = -extval;
                    if (currentDistanceValue > extval) currentDistanceValue = extval;

                    float alpha = (currentDistanceValue - (-extval)) / (2 * extval);

                    float R, G, B;

                    R = 255 * alpha;
                    G = 255 * (1 - alpha);
                    B = 255 * (float)(1 - Math.Abs(alpha - 0.5) * 2);

                    Color color = Color.FromArgb(255, (byte)R, (byte)G, (byte)B);

                    // Background (color intensity for red)
                    if (currentDistanceValue < short.MinValue)
                    {
                        color = Color.Orange;
                    }
                    else if (currentDistanceValue > short.MaxValue)
                    {
                        color = Color.HotPink;
                    }
                    else if ((int)currentDistanceValue == 0)
                    {
                        color = Color.Yellow;
                    }
                    image.SetPixel(x, y, color);
                }
            }

            image.Save(filePath);
        }
        public static byte[] ToByteArray(this Bitmap image)
        {
            var imageWidth = image.Width;
            var imageHeight = image.Height;

            var result = new byte[imageWidth * imageHeight];

            var bitmapData = image.LockBits(new System.Drawing.Rectangle(0, 0, imageWidth, imageHeight), ImageLockMode.ReadWrite,
                image.PixelFormat);

            var stride = bitmapData.Stride / imageWidth;

            var pixelData = new byte[Math.Abs(bitmapData.Stride) * imageHeight];

            // Copy the values into the array.
            Marshal.Copy(bitmapData.Scan0, pixelData, 0, pixelData.Length);

            Parallel.For(0, imageHeight, delegate (int y)
            {
                for (var x = 0; x < imageWidth; x++)
                {
                    if (pixelData[y * bitmapData.Stride + (x * stride)] == 0)
                    {
                        result[x + y * imageWidth] = 1;
                    }
                }
            });

            image.UnlockBits(bitmapData);

            return result;
        }

        /// <summary>
        /// Checks if the directory structure for the given file path already exists.
        /// If not, the directory will be created.
        /// </summary>
        /// <param name="filePath"></param>
        public static void CreateFolderStructureIfNotExists(string filePath)
        {
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }
    }
}