// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.RTConvert.Converters
{
    using Microsoft.RTConvert.MedIO.Models;
    using Microsoft.RTConvert.MedIO.RT;
    using Microsoft.RTConvert.Models;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// How the segmentation output data is encoded
    /// </summary>
    public enum SegmentationOutputEncoding
    {
        /// <summary>
        /// As a DICOM RT Structure set
        /// </summary>
        RTStruct = 0,

        /// <summary>
        /// Output data is encoded as a set of binary masks.
        /// </summary>
        ZippedInnerEyeProto3MaskEncoding = 1,
    }

    /// <summary>
    /// Encapsulates encoding segmentation model outputs into a binary format for dispatch back
    /// to the requesting client. 
    /// </summary>
    public interface ISegmentationOutputEncoder
    {
        /// <summary>
        /// Gets the output encoding.
        /// </summary>
        /// <value>
        /// The output encoding.
        /// </value>
        SegmentationOutputEncoding OutputEncoding { get; }

        /// <summary>
        /// Encodes the segmentation output in a binary format. 
        /// </summary>
        /// <param name="outputStructures">The output structures from eh machine learning</param>
        /// <param name="inputChannels">The input channels that were used to generate the structures</param>
        /// <param name="modelAndVersion">The model name and version in AzureML.</param>
        /// <param name="manufacturer">The creator of the model.</param>
        /// <param name="interpreter">The interpreter of the model.</param>
        /// <returns></returns>
        ArraySegment<byte> EncodeStructures(
              IEnumerable<(string name, Volume3D<byte> volume, RGBColor color, bool fillHoles, ROIInterpretedType roiInterpretedType)> outputStructures,
              IReadOnlyDictionary<string, MedicalVolume> inputChannels,
              string modelAndVersion,
              string institutionId,
              string interpreter);
    }
}
