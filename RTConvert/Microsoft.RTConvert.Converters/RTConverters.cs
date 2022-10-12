// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.RTConvert.Converters
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;

    using Microsoft.RTConvert.Common.Helpers;
    using Microsoft.RTConvert.Converters.Models;
    using Microsoft.RTConvert.MedIO;
    using Microsoft.RTConvert.MedIO.Models;
    using Microsoft.RTConvert.MedIO.Readers;
    using Microsoft.RTConvert.MedIO.RT;

    public static class RTConverters
    {
        /// <summary>
        /// Arrays passed in may start or end with space, single or double quotes.
        /// </summary>
        private static readonly char[] CommonTrimChars = new[] { ' ', '\'', '"' };

        /// <summary>
        /// Arrays passed in may also start with left bracket.
        /// </summary>
        private static readonly char[] LeftTrimChars = CommonTrimChars.Append('[').ToArray();

        /// <summary>
        /// Arrays passed in may also end with right bracket.
        /// </summary>
        private static readonly char[] RightTrimChars = CommonTrimChars.Append(']').ToArray();

        /// <summary>
        /// Split array string into array of strings, removing quotes and optional brackets.
        /// </summary>
        /// <param name="str">Input string.</param>
        /// <returns>Array of strings.</returns>
        public static string[] GetStringArrayFromString(string str)
        {
            var strArray = str.TrimStart(LeftTrimChars).TrimEnd(RightTrimChars).Split(',', StringSplitOptions.None);

            return strArray.Select(s => s.Trim()).ToArray();
        }

        /// <summary>
        /// Split array string into array of type T.
        /// </summary>
        /// <param name="str">Input string.</param>
        /// <param name="itemConverter">Item converter.</param>
        /// <returns>Array of items of type T.</returns>
        public static T[] GetTArrayFromString<T>(string str, Func<string, T> itemConverter)
        {
            var stringArray = GetStringArrayFromString(str);
            var result = new T[stringArray.Length];

            for (var i = 0; i < stringArray.Length; i++)
            {
                try
                {
                    result[i] = itemConverter(stringArray[i]);
                }
                catch (ArgumentException e)
                {
                    throw new ArgumentException(string.Format("Cannot parse array at index: {0}, {1}",
                        i + 1, e.Message));
                }
            }

            return result;
        }

        /// <summary>
        /// Split array string into array of RGBColors.
        /// </summary>
        /// <param name="str">Input string.</param>
        /// <returns>Array of RGBColors.</returns>
        public static RGBColorOption?[] GetRGBColorArrayFromString(string str) =>
            GetTArrayFromString(str, ConvertColor);

        /// <summary>
        /// Split array string into array of bools.
        /// </summary>
        /// <param name="str">Input string.</param>
        /// <returns>Array of bools.</returns>
        public static bool?[] GetBoolArrayFromString(string str) =>
            GetTArrayFromString(str, ConvertBool);


        /// <summary>
        /// Split array string into RoiInterpretedTypes
        /// </summary>
        /// <param name="str">Input string.</param>
        /// <returns>Array of bools.</returns>
        public static ROIInterpretedType[] GetRoiInterpretedTypesArrayFromString(string str) =>
            GetTArrayFromString(str, ConvertRoiInterpretedType);

        public static ROIInterpretedType ConvertRoiInterpretedType(string roi) => (ROIInterpretedType)Enum.Parse(typeof(ROIInterpretedType), roi, ignoreCase:true);

            /// <summary>
            /// Convert from hexadecimal color string to RGBColor.
            /// </summary>
            /// <param name="colorIn">Color string to parse.</param>
            /// <returns>RGBColorOption or null.</returns>
            public static RGBColorOption? ConvertColor(string colorIn)
        {
            var colour = colorIn.Trim();

            if (colour.StartsWith("#"))
            {
                colour = colour.Substring(1);
            }

            if (string.IsNullOrEmpty(colour))
            {
                return null;
            }

            var r = ConvertColorComponent(colour, 0, colorIn, "red");
            var g = ConvertColorComponent(colour, 2, colorIn, "green");
            var b = ConvertColorComponent(colour, 4, colorIn, "blue");

            return new RGBColorOption(r, g, b);
        }

        /// <summary>
        /// Convert a color component, e.g. R, G or B from a hexadecimal string.
        /// </summary>
        /// <param name="color">Trimmed color string.</param>
        /// <param name="startIndex">Index to start from.</param>
        /// <param name="fullColor">Untrimmed string, for reporting.</param>
        /// <param name="componentName">Component name, for reporting.</param>
        /// <returns>Color component or null.</returns>
        private static byte? ConvertColorComponent(string color, int startIndex, string fullColor, string componentName)
        {
            if (color.Length < startIndex + 2)
            {
                return null;
            }

            var component = color.Substring(startIndex, 2);

            if (!byte.TryParse(component, NumberStyles.HexNumber, null, out byte result))
            {
                throw new ArgumentException(
                    string.Format(
                        "Cannot parse {0} of RGB color {1}, {2} is not a hexadecimal string",
                        componentName, fullColor, component));
            }

            return result;
        }

        /// <summary>
        /// Convert from string to bool.
        /// </summary>
        /// <param name="value">Input string.</param>
        /// <returns>Bool?.</returns>
        public static bool? ConvertBool(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            if (!bool.TryParse(value, out bool result))
            {
                throw new ArgumentException(
                    string.Format(
                        "Cannot parse bool {0}. Expect true, True, false or False",
                        value));
            }

            return result;
        }

        /// <summary>
        /// Convert from Nifti format to DICOM-RT format.
        /// </summary>
        /// <param name="niftiFilename">Path to the input Nifti file.</param>
        /// <param name="referenceSeries">Path to the input folder containing the reference DICOM series.</param>
        /// <param name="structNames">List of comma separated structure names.</param>
        /// <param name="structColors">List of comma separated structure colors in hexadecimal notation.</param>
        /// <param name="fillHoles">List of comma separated flags, whether to fill holes in the structure.</param>
        /// <param name="dcmFilename">Path to the output DICOM-RT file.</param>
        /// <remarks>
        /// <list type="bullet">
        /// <item>
        /// <term><paramref name="structNames"/></term>
        /// <description>An example of this would be:
        ///     "External, parotid_l, parotid_r, smg_l".
        ///     Each structure name corresponds to a non-zero voxel value in the input volume.
        ///     In the example "External" corresponds to voxel value 1, "parotid_l" to 2, etc.
        ///     Voxels with value 0 are dropped.
        ///     If there are voxels without a corresponding structure name, they will also be dropped.
        ///     The structure name will become
        ///     its "ROI Name" in the "Structure Set ROI Sequence" in the "Structure Set" in the DICOM-RT file.
        /// </description>
        /// </item>
        /// <item>
        /// <term><paramref name="structColors"/></term>
        /// <description>An example of this would be:
        ///     "000000, FF0080, 00FF00, 0000FF".
        ///     Each color in this list corresponds to a structure in structNames and will become
        ///     its "ROI Display Color" in the "ROI Contour Sequence" in the "ROI Contour" in the DICOM-RT file.
        ///     If there are less colors than struct_names, or if an entry is empty, the default is red (FF0000).
        /// </description>
        /// </item>
        /// <item>
        /// <term><paramref name="fillHoles"/></term>
        /// <description>An example of this would be:
        ///     "True, False, True".
        ///     If there are less bools than struct_names, or if an entry is empty, the default is false.
        ///     If True then any contours found per slice will have their holes filled,
        ///     otherwise contours will be returned as found.
        /// </description>
        /// </item>
        /// </list>
        /// </remarks>
        public static void ConvertNiftiToDicom(
            string niftiFilename,
            string referenceSeries,
            string structNames,
            string structColors,
            string fillHoles,
            string roiInterpretedTypes,
            string dcmFilename,
            string modelNameAndVersion,
            string manufacturer,
            string interpreter)
        {
            var structureNames = GetStringArrayFromString(structNames);
            var structureColors = GetRGBColorArrayFromString(structColors);
            var fillHolesArray = GetBoolArrayFromString(fillHoles);
            var roiInterpretedTypesArray = GetRoiInterpretedTypesArrayFromString(roiInterpretedTypes);

            ConvertNiftiToDicom(niftiFilename, referenceSeries, structureNames, structureColors, fillHolesArray, roiInterpretedTypesArray, 
                dcmFilename, modelNameAndVersion, manufacturer, interpreter);
        }

        /// <summary>
        /// Convert from Nifti format to DICOM-RT format.
        /// </summary>
        /// <param name="niftiFilename">Nifti input filename.</param>
        /// <param name="referenceSeries">Path to folder of reference DICOM files.</param>
        /// <param name="structureNames">Names for each structure.</param>
        /// <param name="structureColors">Colors for each structure, defaults to red if this array smaller than list of structures.</param>
        /// <param name="fillHoles">Flags to enable fill holes for each structure, defaults to false this array smaller than list of structures..</param>
        /// <param name="dcmFilename">Target output file.</param>
        public static void ConvertNiftiToDicom(
            string niftiFilename,
            string referenceSeries,
            string[] structureNames,
            RGBColorOption?[] structureColors,
            bool?[] fillHoles,
            ROIInterpretedType[] roiInterpretedTypes,
            string dcmFilename,
            string modelNameAndVersion,
            string manufacturer,
            string interpreter)
        {
            var sourceVolume = MedIO.LoadNiftiAsByte(niftiFilename);
            Trace.TraceInformation($"Loaded NIFTI from {niftiFilename}");

            var labels = VolumeMetadataMapper.MultiLabelMapping(sourceVolume, structureNames.Length);
            var volumesWithMetadata = VolumeMetadataMapper.MapVolumeMetadata(labels, structureNames, structureColors, fillHoles, roiInterpretedTypes);

            var referenceVolume = DicomSeriesHelpers.LoadVolume(
                DicomFolderContents.Build(
                    Directory.EnumerateFiles(referenceSeries).Select(x => DicomFileAndPath.SafeCreate(x)).ToList()
                ));

            var outputEncoder = new DicomRTStructOutputEncoder();

            var outputDicomFile = outputEncoder.EncodeStructures(
                volumesWithMetadata,
                new Dictionary<string, MedicalVolume>() { { "", referenceVolume } },
                modelNameAndVersion,
                manufacturer,
                interpreter);

            outputDicomFile.Save(dcmFilename);
        }
    }
}
