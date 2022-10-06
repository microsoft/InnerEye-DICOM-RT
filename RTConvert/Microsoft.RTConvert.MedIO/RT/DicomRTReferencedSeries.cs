// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.RTConvert.MedIO.RT
{ 
    using System.Collections.Generic;

    using FellowOakDicom;

    using Microsoft.RTConvert.MedIO.Extensions;

    public class DicomRTReferencedSeries
    {
        public string SeriesInstanceUID { get; }

        public IReadOnlyList<DicomRTContourImageItem> ContourImages { get; }

        public DicomRTReferencedSeries(string seriesInstanceUid, IReadOnlyList<DicomRTContourImageItem> contourImages)
        {
            SeriesInstanceUID = seriesInstanceUid;
            ContourImages = contourImages;
        }

        public static DicomRTReferencedSeries Read(DicomDataset ds)
        {
            var seriesInstanceUID = ds.GetStringOrEmpty(DicomTag.SeriesInstanceUID);

            var contourImages = new List<DicomRTContourImageItem>();
            if (ds.Contains(DicomTag.ContourImageSequence))
            {
                // Changed for new OSS fo-dicom-desktop
                var seq = ds.GetSequence(DicomTag.ContourImageSequence);
                //var seq = ds.Get<DicomSequence>(DicomTag.ContourImageSequence);

                foreach (var item in seq)
                {
                    var contourImageItem = DicomRTContourImageItem.Read(item);
                    contourImages.Add(contourImageItem);
                }
            }

            return new DicomRTReferencedSeries(seriesInstanceUID, contourImages);
        }

        public static DicomDataset Write(DicomRTReferencedSeries series)
        {
            var ds = new DicomDataset();
            ds.Add(DicomTag.SeriesInstanceUID, series.SeriesInstanceUID);

            var listOfContour = new List<DicomDataset>();
            foreach (var contour in series.ContourImages)
            {
                var newDS = DicomRTContourImageItem.Write(contour);
                listOfContour.Add(newDS);
            }

            ds.Add(new DicomSequence(DicomTag.ContourImageSequence, listOfContour.ToArray()));
            return ds;
        }
    }
}