// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.RTConvert.Converters.Models
{
    using Microsoft.RTConvert.MedIO.Models;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Simple internal container for packaging the output of decoding segmentation input data
    /// </summary>
    public class DecodedSegmentationTaskData
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DecodedSegmentationTaskData"/> class.
        /// </summary>
        /// <param name="data">The model input data received by the client.</param>
        /// <param name="primarySeriesID">The primary series identifier.</param>
        /// <param name="patientID">The patient identifier.</param>
        /// <exception cref="ArgumentNullException">
        /// data
        /// or
        /// primarySeriesID
        /// or
        /// patientID
        /// </exception>
        public DecodedSegmentationTaskData(
            IReadOnlyDictionary<string, MedicalVolume> data,
            string primarySeriesID,
            string patientID)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));
            _primarySeriesID = primarySeriesID ?? throw new ArgumentNullException(nameof(primarySeriesID));
            _patientId = patientID ?? throw new ArgumentNullException(nameof(patientID));
        }

        /// <summary>
        /// A MedicalVolume for each model input channel, as passed from the client.
        /// </summary>

        IReadOnlyDictionary<string, MedicalVolume> _data;
        public IReadOnlyDictionary<string, MedicalVolume> Data { get => _data; }

        /// <summary>
        /// A unique identifier for the primary series of the input data. This is used to match
        /// up any feedback received later on model output.
        /// </summary>
        string _primarySeriesID;
        public string PrimarySeriesID { get => _primarySeriesID; }

        /// <summary>
        /// A unique identifier for the Patient scanned in the input data. This is used to match
        /// up any feedback received later on model output.
        /// </summary>
        string _patientId;
        public string PatientID { get => _patientId; }
    }
}
