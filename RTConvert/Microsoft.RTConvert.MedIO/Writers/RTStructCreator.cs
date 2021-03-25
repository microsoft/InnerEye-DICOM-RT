// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.RTConvert.MedIO.Writers
{
    using System;
    using System.Collections.Generic;
    using Microsoft.RTConvert.MedIO.Extensions;
    using Microsoft.RTConvert.MedIO.Models.DicomRT;
    using Microsoft.RTConvert.MedIO.Readers;
    using Microsoft.RTConvert.MedIO.RT;
    using Microsoft.RTConvert.Contours;
    using Microsoft.RTConvert.Models;

    public class RTStructCreator
    {
        /// <summary>
        /// Creates a new RadiotherapyContour for inclusion in a RadiotherapyStruct ready for serialization
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="axialContours">The contours relative to the given volume you wish to map into the DICOM reference coordinate system</param>
        /// <param name="identifiers"> The DICOM identifiers describing the origin of the volume</param>
        /// <param name="volumeTransform">The volume transform.</param>
        /// <param name="name">The DICOM structure name</param>
        /// <param name="color">The color of this structure</param>
        /// <param name="roiNumber">The roiNumber of this structure</param>
        /// <returns></returns>
        public static RadiotherapyContour CreateRadiotherapyContour(
            ContoursPerSlice axialContours, 
            IReadOnlyList<DicomIdentifiers> identifiers, 
            VolumeTransform volumeTransform, 
            string name, 
            (byte R, byte G, byte B) color, 
            string roiNumber, 
            DicomPersonNameConverter interpreterName,
            ROIInterpretedType roiInterpretedType)
        {
            if (identifiers == null || identifiers.Count == 0)
            {
                throw new ArgumentException("The DICOM identifiers cannot be null or empty");
            }

            var contours = axialContours.ToDicomRtContours(identifiers, volumeTransform);
            var rtcontour = new DicomRTContour(roiNumber, Tuple.Create(color.R, color.G, color.B), contours);
            var rtRoIstructure = new DicomRTStructureSetROI(roiNumber, name, identifiers[0].FrameOfReference.FrameOfReferenceUid, ERoiGenerationAlgorithm.Semiautomatic);
            var observation = new DicomRTObservation(roiNumber, interpreterName, roiInterpretedType);
            var output = new RadiotherapyContour(rtcontour, rtRoIstructure, observation);
            output.Contours = axialContours;
            return output;
        }
    }
}