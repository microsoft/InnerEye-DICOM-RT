// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.RTConvert.Converters.Tests
{
    using NUnit.Framework;

    using System;
    using System.IO;
    using System.Diagnostics;
    using System.Linq;
    using System.Collections.Generic;

    using FellowOakDicom;

    using Microsoft.RTConvert.Models;
    using Microsoft.RTConvert.Common.Helpers;
    using Microsoft.RTConvert.Converters.Models;
    using Microsoft.RTConvert.MedIO;
    using Microsoft.RTConvert.MedIO.Models;
    using Microsoft.RTConvert.MedIO.Readers;
    using Microsoft.RTConvert.Converters;
    using static TestHelper;

    class RTEncodingTests
    {
        private Volume3D<byte> sourceVolume;

        [SetUp]
        public void Setup()
        {
            sourceVolume = MedIO.LoadNiftiAsByte(TestNiftiSegmentationLocation);
            Trace.TraceInformation($"Loaded NIFTI from {TestNiftiSegmentationLocation}");

            Assert.AreEqual(NumValidLabels, FillHoles.Length);
            Assert.AreEqual(NumValidLabels, StructureColors.Length);
            Assert.AreEqual(NumValidLabels, StructureNames.Length);
        }

        /// <summary>
        /// Checks that RT struct file is correctly generated from the NIFTI volume
        /// </summary>
        [Test]
        public void RtStructOutputEncoder_SuccessWithValidInputs()
        {            
            var labels = VolumeMetadataMapper.MultiLabelMapping(sourceVolume, NumValidLabels);
            var volumesWithMetadata = VolumeMetadataMapper.MapVolumeMetadata(labels, StructureNames, StructureColors, FillHoles, ROIInterpretedTypes);

            var referenceVolume = DicomSeriesHelpers.LoadVolume(DicomFolderContents.Build(
                Directory.EnumerateFiles(TestDicomVolumeLocation).Select(x => DicomFileAndPath.SafeCreate(x)).ToList()
                ));

            var outputEncoder = new DicomRTStructOutputEncoder();

            var outputStructureBytes = outputEncoder.EncodeStructures(
                volumesWithMetadata,
                new Dictionary<string, MedicalVolume>() { { "", referenceVolume } },
                "modelX:1",
                "manufacturer",
                "interpreter");

            var dcm = DicomFile.Open(new MemoryStream(outputStructureBytes.Array));

            // Check the output format (should be RT struct)
            Assert.AreEqual(DicomUID.RTStructureSetStorage, dcm.FileMetaInfo.MediaStorageSOPClassUID);

            // Check stuff in StructureSet ROI sequence
            var structSetRois = dcm.Dataset.GetSequence(DicomTag.StructureSetROISequence).Items;
            var iter = StructureNames.GetEnumerator();
            iter.MoveNext();

            var origReferencedFrameOfReference = referenceVolume.Identifiers.First().FrameOfReference.FrameOfReferenceUid;

            foreach (var roi in structSetRois)
            {
                // Verify that names in the generated DICOM Rt structure are the ones we've supplied
                Assert.AreEqual(iter.Current, roi.GetString(DicomTag.ROIName));
                iter.MoveNext();

                // Verify that this roi references the same frame of reference as the original image
                Assert.AreEqual(roi.GetString(DicomTag.ReferencedFrameOfReferenceUID), origReferencedFrameOfReference);
            }

            // Check stuff in ROI Contour sequence
            var roiContours = dcm.Dataset.GetSequence(DicomTag.ROIContourSequence).Items;
            var iterColors = 0;

            var sopInstanceUIDs = referenceVolume.Identifiers.Select(x => x.Image.SopCommon.SopInstanceUid);
            foreach (var contourSequence in roiContours)
            {
                // Verify that colors in the generated contour sequence are the ones we've supplied
                var currentColor = StructureColors[iterColors].Value;
                var currentColorString = string.Format("{0}\\{1}\\{2}", currentColor.R, currentColor.G, currentColor.B);

                Assert.AreEqual(contourSequence.GetString(DicomTag.ROIDisplayColor), currentColorString);
                iterColors++;

                // Verify that all contour types are closed planar
                Assert.IsTrue(contourSequence.GetSequence(DicomTag.ContourSequence).Items.All(
                    x => x.GetString(DicomTag.ContourGeometricType) == "CLOSED_PLANAR"));

                // Verify that for all contours there exists a SOP Instance UID in the original series
                Assert.IsTrue(contourSequence.GetSequence(DicomTag.ContourSequence).Items.All(
                    x => sopInstanceUIDs.Contains(x.GetSequence(DicomTag.ContourImageSequence).Items[0].GetString(DicomTag.ReferencedSOPInstanceUID))));
            }
        }
    }
}
