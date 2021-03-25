// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Dicom;
using Microsoft.RTConvert.MedIO.Extensions;

namespace Microsoft.RTConvert.MedIO.RT
{
    /// <summary>
    /// Encodes the DICOM Frame of reference module.
    /// <see cref="http://dicom.nema.org/medical/dicom/current/output/html/part03.html#sect_C.7.4.1"/>
    /// </summary>
    public class DicomFrameOfReference
    {

        private DicomFrameOfReference(string uid, string referenceIndicator)
        {
            FrameOfReferenceUid = uid;
            PositionReferenceIndicator = referenceIndicator;
        }

        /// <summary>
        /// UID of the reference coordinate frame. Type 1.
        /// </summary>
        public string FrameOfReferenceUid { get; }

        /// <summary>
        /// Reference indicator for the coordinate system. Type 2
        /// </summary>
        public string PositionReferenceIndicator { get; }

        /// <summary>
        /// Read a DicomFrameOfReference from the given dataset throwing if Type 1 parameters are not present.
        /// </summary>
        /// <param name="ds"></param>
        /// <returns></returns>
        public static DicomFrameOfReference Read(DicomDataset ds)
        {
            // throw
            var uid = ds.GetSingleValue<DicomUID>(DicomTag.FrameOfReferenceUID);
            //var uid = ds.Get<DicomUID>(DicomTag.FrameOfReferenceUID);
            // no throw
            var posRef = ds.GetTrimmedStringOrEmpty(DicomTag.PositionReferenceIndicator);

            return new DicomFrameOfReference(uid.UID, posRef);
        }

        /// <summary>
        /// Creates an empty DicomFrameOfReference instance
        /// </summary>
        public static DicomFrameOfReference CreateEmpty()
        {
            return new DicomFrameOfReference(
                string.Empty, string.Empty
            );
        }
    }
}
