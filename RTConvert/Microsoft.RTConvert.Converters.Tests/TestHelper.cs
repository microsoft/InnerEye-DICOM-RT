// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.RTConvert.Converters.Tests
{
    using Microsoft.RTConvert.MedIO.RT;
    using Microsoft.RTConvert.Models;
    using NUnit.Framework;
    using System;
    using System.IO;
    using System.Linq;

    class TestHelper
    {
        public static string GetTestDataPath(string relativePath)
        {
            return Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData", relativePath));
        }

        public static string TestNiftiSegmentationLocation = GetTestDataPath("hnsegmentation.nii.gz");
        public static string TestDicomVolumeLocation = GetTestDataPath("HN");

        // Our source volume has NumLabelsToMap non-empty distinct labels
        public const int NumValidLabels = 22;

        /// <summary>
        /// Parse a string known to contain a bool and return a bool option.
        /// </summary>
        /// <param name="b">String containing a bool.</param>
        /// <returns>Bool from string, as an option.</returns>
        public static bool? ParseBoolOption(string b) => bool.Parse(b);

        public static readonly bool?[] FillHoles = (new[] {
            "true", "true", "true", "true",
            "false", "false", "true", "true",
            "true", "true", "false", "true",
            "true", "true", "true", "false",
            "true", "false", "true", "true",
            "false", "true"}).Select(ParseBoolOption).ToArray();

        /// <summary>
        /// Parse a string known to contain a color string and return an RGBColorOption.
        /// </summary>
        /// <param name="c">String containing a color.</param>
        /// <returns>RGBColorOption</returns>
        public static RGBColorOption? ParseColorOption(string str)
        {
            var colors = str.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            byte.TryParse(colors[0], out byte r);
            byte.TryParse(colors[1], out byte g);
            byte.TryParse(colors[2], out byte b);

            return new RGBColorOption(r, g, b);
        }

        public static readonly RGBColorOption?[] StructureColors = (new[] {
            "255,0,1", "255,0,2", "255,0,3", "255,0,4",
            "255,1,1", "255,1,2", "255,1,3", "255,1,3",
            "255,2,1", "255,2,255", "255,2,3", "255,2,4",
            "255,3,1", "255,3,2", "1,255,3", "255,3,4",
            "255,4,1", "0,255,255", "255,4,3", "255,4,4",
            "255,5,1", "255,5,2"}).Select(ParseColorOption).ToArray();

        public static readonly string[] StructureNames = {
            "External", "parotid_l", "parotid_r", "smg_l",
            "smg_r", "spinal_cord", "brainstem", "globe_l",
            "Globe_r", "mandible", "spc_muscle", "mpc_muscle",
            "Cochlea_l", "cochlea_r", "lens_l", "lens_r",
            "optic_chiasm", "optic_nerve_l", "optic_nerve_r", "pituitary_gland",
            "lacrimal_gland_l", "lacrimal_gland_r"};

        public static readonly ROIInterpretedType[] ROIInterpretedTypes = {
            ROIInterpretedType.ORGAN, ROIInterpretedType.ORGAN, ROIInterpretedType.ORGAN, ROIInterpretedType.ORGAN,
            ROIInterpretedType.ORGAN, ROIInterpretedType.ORGAN, ROIInterpretedType.ORGAN, ROIInterpretedType.ORGAN,
            ROIInterpretedType.ORGAN, ROIInterpretedType.ORGAN, ROIInterpretedType.ORGAN, ROIInterpretedType.ORGAN,
            ROIInterpretedType.ORGAN, ROIInterpretedType.ORGAN, ROIInterpretedType.ORGAN, ROIInterpretedType.ORGAN,
            ROIInterpretedType.None, ROIInterpretedType.ORGAN, ROIInterpretedType.ORGAN, ROIInterpretedType.ORGAN,
            ROIInterpretedType.EXTERNAL, ROIInterpretedType.CTV};
    }
}
