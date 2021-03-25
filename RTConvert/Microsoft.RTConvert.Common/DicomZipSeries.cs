// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.RTConvert.Common
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Helpers;

    using Microsoft.RTConvert.MedIO.Readers;

    /// <summary>
    /// Helper methods to deserialize segmentation data
    /// </summary>
    public static class DicomZipSeries
    {
        /// <summary>
        /// Deserializes a compressed zip with dicom files
        /// </summary>
        /// <param name="compressedData"></param>
        /// <returns></returns>
        public static IEnumerable<(string ChannelId, DicomFolderContents Content)> DecompressSegmentationData(byte[] compressedData)
        {
            using (var memoryStream = new MemoryStream(compressedData))
            {
                return DecompressSegmentationData(memoryStream);
            }
        }

        /// <summary>
        /// Decompresses the segmentation data from an input stream containing zipped Dicom files.
        /// </summary>
        /// <param name="inputStream">The input stream.</param>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
        public static IEnumerable<(string ChannelId, DicomFolderContents Content)> DecompressSegmentationData(Stream inputStream)
        {
            var files = DicomCompressionHelpers.DecompressPayload(inputStream);
            var dictionaryChannelToFiles = new Dictionary<string, List<byte[]>>();

            foreach ((string filename, byte[] data) in files)
            {
                var channelId = filename.Split(DicomCompressionHelpers.ChannelIdAndDicomSeriesSeparator).First();

                if (!dictionaryChannelToFiles.ContainsKey(channelId))
                {
                    dictionaryChannelToFiles.Add(channelId, new List<byte[]>());
                }

                dictionaryChannelToFiles[channelId].Add(data);
            }

            var result = new List<(string ChannelId, DicomFolderContents content)>();

            foreach (var item in dictionaryChannelToFiles)
            {
                var fileAndPaths = item.Value
                    .Select(x => DicomFileAndPath.SafeCreate(new MemoryStream(x), string.Empty))
                    .ToList();
                result.Add((item.Key, DicomFolderContents.Build(fileAndPaths)));
            }

            return result;
        }
    }
}