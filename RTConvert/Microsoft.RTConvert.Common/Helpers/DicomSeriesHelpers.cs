// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.RTConvert.Common.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.RTConvert.MedIO;
    using Microsoft.RTConvert.MedIO.Models;
    using Microsoft.RTConvert.MedIO.Readers;

    /// <summary>
    /// Helper methods for extracting volumes from packaged DICOM files.
    /// </summary>
    public sealed class DicomSeriesHelpers
    {
        /// <summary>
        /// Given a compressed package of DICOM data, reads MedicalVolumes from the package.
        /// </summary>
        /// <param name="compressedData"></param>
        /// <returns></returns>
        public static IReadOnlyDictionary<string, MedicalVolume> LoadCompressedImageSeries(byte[] compressedData)
        {
            var dicomFolders = DicomZipSeries.DecompressSegmentationData(compressedData);

            var result = new Dictionary<string, MedicalVolume>();

            foreach (var (channelId, content) in dicomFolders)
            {
                result.Add(channelId, LoadVolume(content));
            }

            return result;
        }

        /// <summary>
        /// Build a MedicalVolume from a DicomFolderContents instance
        /// </summary>
        /// <param name="dicomFolder"></param>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2201:DoNotRaiseReservedExceptionTypes")]
        public static MedicalVolume LoadVolume(DicomFolderContents dicomFolder)
        {
            dicomFolder = dicomFolder ?? throw new ArgumentNullException(nameof(dicomFolder));

            var volumeLoaderResult = MedIO.LoadAllDicomSeries(
                                    dicomFolder,
                                    new ModerateGeometricAcceptanceTest("Non Square pixels", "Unsupported Orientation"),
                                    loadStructuresIfExists: false,
                                    supportLossyCodecs: false);

            if (volumeLoaderResult.Count > 1)
            {
                throw new Exception($"More than 1 volume loaded for path {dicomFolder.FolderPath}");
            }

            var volumeLoaderFirstResult = volumeLoaderResult.First();

            if (volumeLoaderFirstResult.Error != null)
            {
                throw volumeLoaderFirstResult.Error;
            }

            if (volumeLoaderFirstResult.Warnings != null && volumeLoaderFirstResult.Warnings.Count > 0)
            {
                throw new Exception(string.Join(",", volumeLoaderFirstResult.Warnings));
            }

            return volumeLoaderFirstResult.Volume;
        }

        private DicomSeriesHelpers()
        {
        }
    }
}