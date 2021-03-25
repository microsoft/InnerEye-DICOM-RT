// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.RTConvert.MedIO.Readers
{
    using System.Collections.Generic;
    using Dicom;

    /// <summary>
    /// Readonly collection of CT/MR images associated with a given series. 
    /// </summary>
    public sealed class DicomSeriesContent
    {
        public DicomSeriesContent(DicomUID seriesUID, IReadOnlyList<DicomFileAndPath> content)
        {
            SeriesUID = seriesUID;
            Content = content;
        }

        /// <summary>
        /// The unique DICOM Series UID
        /// </summary>
        public DicomUID SeriesUID { get; private set; }

        /// <summary>
        /// The list of recognised Sop Class instances in this series.
        /// </summary>
        public IReadOnlyList<DicomFileAndPath> Content { get; private set; }
    }
}