// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.RTConvert.Converters
{
    using Microsoft.RTConvert.MedIO.Models;
    using Microsoft.RTConvert.MedIO.Readers;
    using Microsoft.RTConvert.Common;
    using Microsoft.RTConvert.Common.Helpers;
    using Microsoft.RTConvert.Converters.Models;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    public class DicomInputDecoder : ISegmentationInputDecoder
    {
        public SegmentationInputEncoding InputEncoding => SegmentationInputEncoding.ZippedDicomFolderChannels;

        /// <summary>
        /// Assumes input stream contains gzipped DICOM file contents.
        /// </summary>
        /// <param name="inputStream">The input stream.</param>
        /// <returns></returns>
        public DecodedSegmentationTaskData Decode(Stream inputStream)
        {
            // Get Dicom Files
            var dicomFolders = DicomZipSeries.DecompressSegmentationData(inputStream);
            var output = LoadVolumes(dicomFolders);

            // The primary series is always the first volume in the input/output data
            // we take the first set of DICOM identifiers of that series.
            var firstIdentifiers = output.First().Value.Identifiers.First();

            return new DecodedSegmentationTaskData(output, firstIdentifiers.Series.SeriesInstanceUid, firstIdentifiers.Patient.Id);
        }

        private static IReadOnlyDictionary<string, MedicalVolume> LoadVolumes(IEnumerable<(string ChannelId, DicomFolderContents Content)> folderContents)
        {
            return folderContents.ToDictionary(x => x.ChannelId, x => DicomSeriesHelpers.LoadVolume(x.Content));
        }
    }
}
