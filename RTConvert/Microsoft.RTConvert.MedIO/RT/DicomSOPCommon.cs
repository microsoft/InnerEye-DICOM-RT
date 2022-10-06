// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.RTConvert.MedIO.RT
{
    using FellowOakDicom;

    using Microsoft.RTConvert.MedIO.Extensions;

    /// <summary>
    /// Encodes important Type 1 tags from the SOP Common module
    /// <see cref="http://dicom.nema.org/medical/dicom/current/output/chtml/part03/sect_C.12.html"/>
    /// </summary>
    public class DicomSOPCommon
    {
        /// <summary>
        /// The SOP Class UID of the parent instance. This uniquely and authoratively defines
        /// the modules expected within a Dicom dataset. Type 1, VR: UI
        /// </summary>
        public string SopClassUid { get; }

        /// <summary>
        /// A unique identifier for the parent instance. In theory this is a GUID in DICOM format. 
        /// Type 1: VR: UI
        /// </summary>
        public string SopInstanceUid { get;  }

        private DicomSOPCommon(string sopClassUid, string sopInstanceUid)
        {
            SopClassUid = sopClassUid;
            SopInstanceUid = sopInstanceUid; 
        }

        /// <summary>
        /// Read a DicomSOPInstance from the given DicomDataset, throwing if required
        /// type 1 properties are not present. 
        /// </summary>
        /// <param name="ds"></param>
        /// <returns></returns>
        public static DicomSOPCommon Read(DicomDataset ds)
        {
            // throw
            // Changed for new OSS fo-dicom-desktop
            var sopClassUid = ds.GetSingleValue<DicomUID>(DicomTag.SOPClassUID).UID;
            var sopInstanceUid = ds.GetSingleValue<DicomUID>(DicomTag.SOPInstanceUID).UID;
            //var sopClassUid = ds.Get<DicomUID>(DicomTag.SOPClassUID).UID;
            //var sopInstanceUid = ds.Get<DicomUID>(DicomTag.SOPInstanceUID).UID;

            return new DicomSOPCommon(sopClassUid, sopInstanceUid);
        }

        /// <summary>
        /// Creates an empty DicomSOPCommon instance. 
        /// </summary>
        /// <returns></returns>
        public static DicomSOPCommon CreateEmpty()
        {
            return new DicomSOPCommon(DicomExtensions.EmptyUid.UID, DicomExtensions.EmptyUid.UID);
        }

    }
}
