// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.RTConvert.MedIO.RT
{
    using System.Collections.Generic;

    using FellowOakDicom;

    using Microsoft.RTConvert.MedIO.Extensions;

    public class DicomRTReferencedStudy
    {

        public static readonly string StudyComponentManagementSopClass = DicomUID.StudyComponentManagementRETIRED.UID;

        public string ReferencedSOPClassUID { get; }

        public string ReferencedSOPInstanceUID { get; }

        public IReadOnlyList<DicomRTReferencedSeries> ReferencedSeries { get; }

        public DicomRTReferencedStudy(
            string referencedSopClassUid,
            string referencedSopInstanceUid,
            IReadOnlyList<DicomRTReferencedSeries> referencedSeries)
        {
            ReferencedSOPClassUID = referencedSopClassUid;
            ReferencedSOPInstanceUID = referencedSopInstanceUid;
            ReferencedSeries = referencedSeries;
        }

        public static DicomRTReferencedStudy Read(DicomDataset ds)
        {
            var refSOPClass = ds.GetStringOrEmpty(DicomTag.ReferencedSOPClassUID);
            var refSOPInstance = ds.GetStringOrEmpty(DicomTag.ReferencedSOPInstanceUID);
            var listSeries = new List<DicomRTReferencedSeries>();

            if (ds.Contains(DicomTag.RTReferencedSeriesSequence))
            {
                // Changed for new OSS fo-dicom-desktop
                var seq = ds.GetSequence(DicomTag.RTReferencedSeriesSequence);
                //var seq = ds.Get<DicomSequence>(DicomTag.RTReferencedSeriesSequence);
                foreach (var item in seq)
                {
                    listSeries.Add(DicomRTReferencedSeries.Read(item));
                }
            }
            return new DicomRTReferencedStudy(refSOPClass, refSOPInstance, listSeries);
        }

        public static DicomDataset Write(DicomRTReferencedStudy refStudy)
        {
            var ds = new DicomDataset();
            ds.Add(DicomTag.ReferencedSOPClassUID, refStudy.ReferencedSOPClassUID);
            ds.Add(DicomTag.ReferencedSOPInstanceUID, refStudy.ReferencedSOPInstanceUID);
            var listOfContour = new List<DicomDataset>();
            foreach (var series in refStudy.ReferencedSeries)
            {
                var newDS = DicomRTReferencedSeries.Write(series);
                listOfContour.Add(newDS);
            }
            if (listOfContour.Count > 0)
            {
                ds.Add(new DicomSequence(DicomTag.RTReferencedSeriesSequence, listOfContour.ToArray()));
            }
            return ds;
        }
    }
}