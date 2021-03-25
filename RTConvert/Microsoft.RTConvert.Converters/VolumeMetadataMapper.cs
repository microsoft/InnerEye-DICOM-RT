// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.RTConvert.Converters
{
    using System;
    using System.Collections.Generic;

    using Microsoft.RTConvert.MedIO.RT;
    using Microsoft.RTConvert.Models;

    public static class VolumeMetadataMapper
    {
        /// <summary>
        /// Default naming policy if not enough or a blank entry supplied for structure name.
        /// </summary>
        public static readonly Func<int, string> DefaultStructureName = i =>
            $"Structure {i}";

        /// <summary>
        /// Color to use if not enough, or a blank entry, supplied for structure color.
        /// </summary>
        public static readonly RGBColor DefaultStructureColor = new RGBColor(0xFF, 0, 0);

        /// <summary>
        /// FillHoles setting to use if not enough, or a blank entry, supplied for fill holes.
        /// </summary>
        public static readonly bool DefaultFillHoles = false;

        /// <summary>
        /// Given lists of binary masks, structure names, colors, and fill holes flags, create a set of tuples of (name, binary mask, color, fill holes flag).
        /// </summary>
        /// <param name="binaryMaps">List of binary masks.</param>
        /// <param name="structureNames">List of structure names, of any length. If a name is missing, default will be supplied by DefaultStructureName.</param>
        /// <param name="structureColors">List of structure colors, of any length. If a color is missing, default will be supplied by DefaultStructureColor.</param>
        /// <param name="fillHoles">List of fill holes flags, of any length. If a flag is missing, default will be DefaultFillHoles.</param>
        /// <returns>List of tuples, one for each of the binary maps.</returns>
        public static IEnumerable<(string name, Volume3D<byte> volume, RGBColor color, bool fillHoles, ROIInterpretedType roiInterpretedType)> MapVolumeMetadata(
            Volume3D<byte>[] binaryMaps,
            string[] structureNames,
            RGBColorOption?[] structureColors,
            bool?[] fillHoles,
            ROIInterpretedType[] roiInterpretedTypes)
        {
            return MapVolumeMetadata(binaryMaps, structureNames, structureColors, fillHoles, roiInterpretedTypes,
                DefaultStructureName, DefaultStructureColor, DefaultFillHoles);
        }

        /// <summary>
        /// Given lists of binary masks, structure names, colors, and fill holes flags, create a set of tuples of (name, binary mask, color, fill holes flag).
        /// </summary>
        /// <param name="binaryMaps">List of binary masks.</param>
        /// <param name="structureNames">List of structure names, of any length. If a name is missing, default will be supplied by defaultStructureName.</param>
        /// <param name="structureColors">List of structure colors, of any length. If a color is missing, default will be supplied by defaultStructureColor.</param>
        /// <param name="fillHoles">List of fill holes flags, of any length. If a flag is missing, default will be defaultFillHole.</param>
        /// <param name="defaultStructureName">Default structure name function.</param>
        /// <param name="defaultStructureColor">Default structure color.</param>
        /// <param name="defaultFillHole">Default fill holes flag.</param>
        /// <returns>List of tuples, one for each of the binary maps.</returns>
        public static IEnumerable<(string name, Volume3D<byte> volume, RGBColor color, bool fillHoles, ROIInterpretedType roiInterpretedType)> MapVolumeMetadata(
            Volume3D<byte>[] binaryMaps,
            string[] structureNames,
            RGBColorOption?[] structureColors,
            bool?[] fillHoles,
            ROIInterpretedType[] roiInterpretedTypes,
            Func<int, string> defaultStructureName,
            RGBColor defaultStructureColor,
            bool defaultFillHole)
        {
            var volumeMap = new List<(string name, Volume3D<byte> volume, RGBColor color, bool fillHoles, ROIInterpretedType roiInterpretedType)>();

            var structsWithNoNames = 0;

            for (int i = 0; i < binaryMaps.Length; i++)
            {
                var _clr = structureColors.Length > i && structureColors[i].HasValue ?
                    structureColors[i].Value.ApplyDefault(defaultStructureColor) : defaultStructureColor;

                // Default struct name: "Structure ##"
                var _name = structureNames.Length > i && !string.IsNullOrWhiteSpace(structureNames[i]) ?
                    structureNames[i] : defaultStructureName.Invoke(++structsWithNoNames);

                var _fillHoles = fillHoles.Length > i && fillHoles[i].HasValue ?
                    fillHoles[i].Value : defaultFillHole;

                volumeMap.Add((
                        name: _name,
                        volume: binaryMaps[i],
                        color: _clr,
                        fillHoles: _fillHoles,
                        roiInterpretedType: roiInterpretedTypes[i]
                    ));
            }

            return volumeMap;
        }

        /// <summary>
        /// Converts a multi-label map to a set of binary volumes. If a voxel has value v in the
        /// input image, v >= 1, then the (v-1)th result volume will have set that voxel to 1.
        /// Put another way: The i.th result volume will have voxels non-zero wherever the input
        /// volume had value (i+1).
        /// </summary>
        /// <param name="image">A multi-label input volume.</param>
        /// <param name="numOutputMasks">The number of result volumes that will be generated.
        /// This value must be at least equal to the maximum voxel value in the input volume.</param>
        /// <returns></returns>
        public static Volume3D<byte>[] MultiLabelMapping(Volume3D<byte> image, int numOutputMasks)
        {
            var result = new Volume3D<byte>[numOutputMasks];

            for (var i = 0; i < numOutputMasks; i++)
            {
                result[i] = image.CreateSameSize<byte>();
            }

            for (var i = 0; i < image.Length; ++i)
            {
                if (image[i] != 0 && image[i] <= numOutputMasks)
                {
                    result[image[i] - 1][i] = 1;
                }
            }

            return result;
        }
    }
}
