// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.RTConvert.Converters
{
    using System.IO;
    using Microsoft.RTConvert.Converters.Models;

    /// <summary>
    /// How the segmentation input data is encoded
    /// </summary>
    public enum SegmentationInputEncoding
    {
        /// <summary>
        /// Data is encoded as a gzipped collection of folders containing input channel DICOM data, folder names
        /// must match the model channel names.
        /// </summary>
        ZippedDicomFolderChannels = 0,

        /// <summary>
        /// Data is encoded as a gzipped binary encoding using the InnerEye Proto3 schema
        /// </summary>
        ZippedInnerEyeProto3BinaryEncoding = 1,
    }

    /// <summary>
    /// Internal interface for decoding segmentation model input data from a a byte array sent over the wire from 
    /// the client. 
    /// </summary>
    public interface ISegmentationInputDecoder
    {
        /// <summary>
        /// Gets the input encoding.
        /// </summary>
        /// <value>
        /// The input encoding.
        /// </value>
        SegmentationInputEncoding InputEncoding { get; }

        /// <summary>
        /// Decodes the specified segmentation model input data from the given stream.
        /// </summary>
        /// <param name="inputStream">The input stream.</param>
        /// <returns>
        /// Decoded segmentation task data
        /// </returns>
        DecodedSegmentationTaskData Decode(Stream inputStream);
    }
}
