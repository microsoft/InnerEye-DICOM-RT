///  ------------------------------------------------------------------------------------------
///  Copyright (c) Microsoft Corporation. All rights reserved.
///  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
///  ------------------------------------------------------------------------------------------

namespace Microsoft.RTConvert.Converters.Tests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;

    using NUnit.Framework;
    using Dicom;

    using Microsoft.RTConvert.Common;
    using Microsoft.RTConvert.Contours.Tests;
    using Microsoft.RTConvert.MedIO;
    using Microsoft.RTConvert.MedIO.Extensions;
    using Microsoft.RTConvert.MedIO.Readers;
    using Microsoft.RTConvert.MedIO.RT;
    using Microsoft.RTConvert.MedIO.Tests;
    using Microsoft.RTConvert.MedIO.Tests.Extensions;
    using Microsoft.RTConvert.Models;

    class RTConvertersTests
    {
        /// <summary>
        /// Test that an array of strings can be parsed.
        /// </summary>
        [TestCase(" External,    parotid_l  ,  parotid_r  ")]
        [TestCase(" [ External,    parotid_l  ,  parotid_r ] ")]
        [TestCase(" \" External,    parotid_l  ,  parotid_r \" ")]
        [TestCase(" \" [ External,    parotid_l  ,  parotid_r ] \" ")]
        public void TestConvertStringArray(string stringArrayString)
        {
            var expectedStrings = new[] { "External", "parotid_l", "parotid_r" };
            var actualStrings = RTConverters.GetStringArrayFromString(stringArrayString);

            Assert.AreEqual(expectedStrings, actualStrings);
        }

        /// <summary>
        /// Test that valid strings can be converted to RGBColors.
        /// </summary>
        /// <param name="r">Expected red.</param>
        /// <param name="g">Expected green.</param>
        /// <param name="b">Expected blue.</param>
        /// <param name="colorString">Color string to parse.</param>
        [TestCase(0xFF, null, null, "FF")]
        [TestCase(0xFF, 0x80, null, "FF80")]
        [TestCase(0xFF, 0x80, 0x40, "FF8040")]
        [TestCase(0xFF, null, null, "#FF")]
        [TestCase(0xFF, 0x80, null, "#FF80")]
        [TestCase(0xFF, 0x80, 0x40, "#FF8040")]
        [TestCase(0xFF, null, null, "ff")]
        [TestCase(0xFF, 0x80, null, "ff80")]
        [TestCase(0xFF, 0x80, 0x40, "ff8040")]
        [TestCase(0xFF, null, null, "#ff")]
        [TestCase(0xFF, 0x80, null, "#ff80")]
        [TestCase(0xFF, 0x80, 0x40, "#ff8040")]
        public void TestConvertColorValid(
            byte r, byte? g, byte? b, string colorString)
        {
            var expectedRGBColour = new RGBColorOption(r, g, b);
            var actualRGBColor = RTConverters.ConvertColor(colorString);

            Assert.AreEqual(expectedRGBColour, actualRGBColor);
        }

        /// <summary>
        /// Test that parsing invalid color strings throws an exception.
        /// </summary>
        /// <param name="colorString">Color string.</param>
        [TestCase("HH")]
        [TestCase("FFHH")]
        [TestCase("FF80HH")]
        public void TestConvertColorInvalid(string colorString)
        {
            var exception = Assert.Throws<ArgumentException>(() => RTConverters.ConvertColor(colorString));

            Assert.True(exception.Message.Contains("Cannot parse "));
            Assert.True(exception.Message.Contains(" is not a hexadecimal string"));
        }

        /// <summary>
        /// Test that an array of colors can be parsed.
        /// </summary>
        [TestCase(" #ff,  FF80 , #ff8040 ")]
        [TestCase(" [ #ff,  FF80 , #ff8040 ] ")]
        [TestCase(" \" #ff,  FF80 , #ff8040 \" ")]
        [TestCase(" \" [ #ff,  FF80 , #ff8040 ] \" ")]
        public void TestConvertColorArrayValid(string colorArrayString)
        {
            var expectedRGBColors = new[] { new RGBColorOption(0xFF, null, null), new RGBColorOption(0xFF, 0x80, null), new RGBColorOption(0xFF, 0x80, 0x40) };
            var actualRGBColors = RTConverters.GetRGBColorArrayFromString(colorArrayString);

            Assert.AreEqual(expectedRGBColors, actualRGBColors);
        }

        /// <summary>
        /// Test that an array of colors can be parsed, even with first missing.
        /// </summary>
        [TestCase(" ,  FF80 , #ff8040 ")]
        public void TestConvertColorArrayFirstMissingValid(string colorArrayString)
        {
            var expectedRGBColors = new RGBColorOption?[] { null, new RGBColorOption(0xFF, 0x80, null), new RGBColorOption(0xFF, 0x80, 0x40) };
            var actualRGBColors = RTConverters.GetRGBColorArrayFromString(colorArrayString);

            Assert.AreEqual(expectedRGBColors, actualRGBColors);
        }

        /// <summary>
        /// Test that an array of colors can be parsed, even with middle missing.
        /// </summary>
        [TestCase(" #ff,   , #ff8040 ")]
        public void TestConvertColorArrayMiddleMissingValid(string colorArrayString)
        {
            var expectedRGBColors = new RGBColorOption?[] { new RGBColorOption(0xFF, null, null), null, new RGBColorOption(0xFF, 0x80, 0x40) };
            var actualRGBColors = RTConverters.GetRGBColorArrayFromString(colorArrayString);

            Assert.AreEqual(expectedRGBColors, actualRGBColors);
        }

        /// <summary>
        /// Test that an array of colors can be parsed, even with last missing.
        /// </summary>
        [TestCase(" [ #ff,  FF80 , ] ")]
        public void TestConvertColorArrayLastMissingValid(string colorArrayString)
        {
            var expectedRGBColors = new RGBColorOption?[] { new RGBColorOption(0xFF, null, null), new RGBColorOption(0xFF, 0x80, null), null };
            var actualRGBColors = RTConverters.GetRGBColorArrayFromString(colorArrayString);

            Assert.AreEqual(expectedRGBColors, actualRGBColors);
        }

        /// <summary>
        /// Test that parsing an invalid color array throws an exception.
        /// </summary>
        [Test]
        public void TestConvertColorArrayInvalid()
        {
            var exception = Assert.Throws<ArgumentException>(() =>
                RTConverters.GetRGBColorArrayFromString(" [ #ff,  FF80HH , #ff8040 ] "));

            Assert.True(exception.Message.Contains("Cannot parse array at index: 2"));
        }

        /// <summary>
        /// Test that a bool string can be parsed.
        /// </summary>
        /// <param name="expected">Expected bool string.</param>
        /// <param name="value">String to test.</param>
        [TestCase(true, " True ")]
        [TestCase(true, " true ")]
        [TestCase(false, " False ")]
        [TestCase(false, " false ")]
        public void TestConvertBoolValid(
            bool expected, string value)
        {
            var actual = RTConverters.ConvertBool(value);

            Assert.AreEqual(expected, actual);
        }

        /// <summary>
        /// Test that parsing an invalid bool string throws an exception.
        /// </summary>
        /// <param name="value">String to test.</param>
        [TestCase("t")]
        [TestCase("1")]
        [TestCase("f")]
        [TestCase("0")]
        public void TestConvertBoolInvalid(string value)
        {
            var exception = Assert.Throws<ArgumentException>(() => RTConverters.ConvertBool(value));

            Assert.True(exception.Message.Contains("Cannot parse bool "));
        }

        /// <summary>
        /// Test that a bool array string can be parsed.
        /// </summary>
        [TestCase("  True, false,   true ] ")]
        [TestCase(" [ True, false,   true ] ")]
        [TestCase(" \" True, false,   true \" ")]
        [TestCase(" \" [ True, false,   true ] \" ")]
        public void TestConvertBoolArrayValid(string boolArrayString)
        {
            var expectedBools = new[] { true, false, true };
            var actualBools = RTConverters.GetBoolArrayFromString(boolArrayString);

            Assert.AreEqual(expectedBools, actualBools);
        }

        /// <summary>
        /// Test that a bool array string can be parsed when the first entry is empty.
        /// </summary>
        [TestCase("  , false,   true ] ")]
        [TestCase(" [ , false,   true ] ")]
        [TestCase(" \" , false,   true \" ")]
        [TestCase(" \" [ , false,   true ] \" ")]
        public void TestConvertBoolArrayFirstMissingValid(string boolArrayString)
        {
            var expectedBools = new bool?[] { null, false, true };
            var actualBools = RTConverters.GetBoolArrayFromString(boolArrayString);

            Assert.AreEqual(expectedBools, actualBools);
        }

        /// <summary>
        /// Test that a bool array string can be parsed when a middle entry is missing.
        /// </summary>
        [TestCase("  True, ,   true ] ")]
        [TestCase(" [ True, ,   true ] ")]
        [TestCase(" \" True, ,   true \" ")]
        [TestCase(" \" [ True, ,   true ] \" ")]
        public void TestConvertBoolArrayMiddleMissingValid(string boolArrayString)
        {
            var expectedBools = new bool?[] { true, null, true };
            var actualBools = RTConverters.GetBoolArrayFromString(boolArrayString);

            Assert.AreEqual(expectedBools, actualBools);
        }

        /// <summary>
        /// Test that a bool array string can be parsed when the last entry is missing.
        /// </summary>
        [TestCase("  True, false,    ] ")]
        [TestCase(" [ True, false,    ] ")]
        [TestCase(" \" True, false,    \" ")]
        [TestCase(" \" [ True, false,    ] \" ")]
        public void TestConvertBoolArrayLastMissingValid(string boolArrayString)
        {
            var expectedBools = new bool?[] { true, false, null };
            var actualBools = RTConverters.GetBoolArrayFromString(boolArrayString);

            Assert.AreEqual(expectedBools, actualBools);
        }

        /// <summary>
        /// Test that parsing an invalid bool array string throws an exception.
        /// </summary>
        [Test]
        public void TestConvertBoolArrayInvalid()
        {
            var exception = Assert.Throws<ArgumentException>(() =>
                RTConverters.GetBoolArrayFromString(" [ True, F, 1 ] "));

            Assert.True(exception.Message.Contains("Cannot parse array at index: 2"));
        }

        /// <summary>
        /// Test that a planar Nifti file can be converted to a DICOM-RT file, and back, with fillHoles=true.
        /// </summary>
        /// <param name="sourceMaskFilename">Source mask filename.</param>
        /// <param name="expectedMaskFilename">Expected mask filename.</param>
        [TestCase("checkerboard.png", "checkerboard-result.png")]
        [TestCase("checkerboard2.png", "checkerboard2-result.png")]
        [TestCase("holes.png", "mask1.png")]
        [TestCase("holes2.png", "triangle.png")]
        [TestCase("mask2.png", "mask2-noholes.png")]
        [TestCase("mask3.png", "mask3-noholes.png")]
        [TestCase("mask4.png", "mask4-noholes.png")]
        [TestCase("problem.png", "problem-noholes.png")]
        public void TestNiftiToDicomFillHoles(string sourceMaskFilename, string expectedMaskFilename)
        {
            CommonTestNiftiToDicomFillHoles(sourceMaskFilename, expectedMaskFilename, true, "PlanarFill");
        }

        /// <summary>
        /// Test that a planar Nifti file can be converted to a DICOM-RT file, and back, with fillHoles=false.
        /// </summary>
        /// <param name="sourceMaskFilename"></param>
        [TestCase("mask1.png")]
        [TestCase("mask2-noholes.png")]
        [TestCase("mask3-noholes.png")]
        [TestCase("mask4-noholes.png")]
        [TestCase("triangle.png")]
        [TestCase("circle.png")]
        [TestCase("smallCircle.png")]
        [TestCase("4contours.png")]
        [TestCase("specialCase.png")]
        [TestCase("problem.png")]
        public void TestNiftiToDicomNoFillHoles(string sourceMaskFilename)
        {
            CommonTestNiftiToDicomFillHoles(sourceMaskFilename, null, false, "PlanarNoFill");
        }

        /// <summary>
        /// Common code for planar Nifti to DICOM-RT tests.
        /// </summary>
        /// <param name="sourceMaskFilename">Source mask filename.</param>
        /// <param name="expectedMaskFilename">Optional expected mask filename if not the same as source.</param>
        /// <param name="fillHoles">Fill holes flag.</param>
        /// <param name="debugFolderName">Optional folder name for debug images.</param>
        public static void CommonTestNiftiToDicomFillHoles(string sourceMaskFilename, string expectedMaskFilename, bool? fillHoles, string debugFolderName)
        {
            // Create the Nifti file as a NxMx1 volume
            var sourceMaskVolume2D = ExtractContourTests.LoadMask(sourceMaskFilename);

            Assert.IsTrue(sourceMaskVolume2D.Array.Any(x => x == 0));
            Assert.IsTrue(sourceMaskVolume2D.Array.Any(x => x == 1));

            var dimZ = 2;
            var sourceVolume3D = sourceMaskVolume2D.Extrude(dimZ, 1.0);

            var niftiFile = TestHelpers.CreateTempNiftiName(NiftiCompression.GZip);
            MedIO.SaveNifti(sourceVolume3D, niftiFile);

            // Create the reference DICOM files.
            var referenceDicomFolder = Path.Combine(TestContext.CurrentContext.TestDirectory, "ReferenceDicom");
            if (Directory.Exists(referenceDicomFolder))
            {
                Directory.Delete(referenceDicomFolder, recursive: true);
                Thread.Sleep(1000);
            }
            Directory.CreateDirectory(referenceDicomFolder);
            var scan = new Volume3D<short>(sourceVolume3D.DimX, sourceVolume3D.DimY, sourceVolume3D.DimZ);
            foreach (var index in scan.Array.Indices())
            {
                scan.Array[index] = (short)index;
            }
            var seriesDescription = "description";
            var patientID = DicomUID.Generate().UID;
            var studyInstanceID = DicomUID.Generate().UID;
            var dicomFiles = NiiToDicomHelpers.ScanToDicomInMemory(scan, ImageModality.CT, seriesDescription, patientID, studyInstanceID, null);

            var dicomFilesOnDisk = new List<string>();
            foreach (var dicomFile in dicomFiles)
            {
                dicomFilesOnDisk.Add(dicomFile.SaveToFolder(referenceDicomFolder));
            }

            // Turn the expected mask into a volume.
            var expectedNiftiFile = string.Empty;

            if (!string.IsNullOrEmpty(expectedMaskFilename))
            {
                var expectedMask = ExtractContourTests.LoadMask(expectedMaskFilename);
                var expectedVolume = expectedMask.Extrude(dimZ, 1.0);

                expectedNiftiFile = TestHelpers.CreateTempNiftiName(NiftiCompression.GZip);
                MedIO.SaveNifti(expectedVolume, expectedNiftiFile);
            }
            else
            {
                // Expected is the same as the source.
                expectedNiftiFile = niftiFile;
            }

            DoTestNiftiToDicom(
                niftiFile,
                referenceDicomFolder,
                new[] { "background", "foreground" },
                new RGBColorOption?[] { new RGBColorOption(0xFF, 0x00, 0x00), new RGBColorOption(0x00, 0xFF, 0x00) },
                new[] { fillHoles, fillHoles },
                new[] { ROIInterpretedType.ORGAN, ROIInterpretedType.CTV },
                debugFolderName,
                true,
                expectedNiftiFile);
        }

        /// <summary>
        /// Test that a Nifti file can be converted to a DICOM-RT file.
        /// </summary>
        /// <remarks>Holes should not be filled.</remarks>
        [Test]
        public void TestNiftiToDicomNoFillHolesHAndN()
        {
            DoTestNiftiToDicom(
                TestHelper.TestNiftiSegmentationLocation,
                TestHelper.TestDicomVolumeLocation,
                TestHelper.StructureNames,
                TestHelper.StructureColors,
                new bool?[] { },
                TestHelper.ROIInterpretedTypes,
                null,
                false, // @TODO This test will fail.
                TestHelper.TestNiftiSegmentationLocation);
        }

        /// <summary>
        /// Test Nifti file to DICOM-RT translation.
        /// </summary>
        /// <param name="niftiFilename">Source Nifti file.</param>
        /// <param name="referenceSeries">Reference DICOM series folder.</param>
        /// <param name="structureNames">List of structure names for DICOM-RT.</param>
        /// <param name="structureColors">List of structure colours for DICOM-RT.</param>
        /// <param name="fillHoles">List of fill hole flags for DICOM-RT.</param>
        /// <param name="roiInterpretedType">List of roiInterpretedType for DICOM-RT.</param>
        /// <param name="debugFolderName">If present, create a full set of debug images.</param>
        /// <param name="testVolumesMatch">If true, check the volumes match.</param>
        /// <param name="expectedNiftiFilename">Expect volume to match Nifti file.</param>
        public static void DoTestNiftiToDicom(
            string niftiFilename,
            string referenceSeries,
            string[] structureNames,
            RGBColorOption?[] structureColors,
            bool?[] fillHoles,
            ROIInterpretedType[] roiInterpretedTypes,
            string debugFolderName,
            bool testVolumesMatch,
            string expectedNiftiFilename)
        {
            var outputFileName = Path.GetRandomFileName() + ".dcm";

            RTConverters.ConvertNiftiToDicom(
                niftiFilename,
                referenceSeries,
                structureNames,
                structureColors,
                fillHoles,
                roiInterpretedTypes,
                outputFileName,
                "modelX:1",
                "manufacturer",
                "interpreter");

            // Open the newly created DICOM-RT file
            var dicomRTFile = DicomFile.Open(outputFileName);

            // Get the medical volume from the reference
            var acceptanceTests = new ModerateGeometricAcceptanceTest(string.Empty, string.Empty);
            var referenceMedicalVolumes = MedIO.LoadAllDicomSeriesInFolderAsync(referenceSeries, acceptanceTests).Result;
            Assert.AreEqual(1, referenceMedicalVolumes.Count);

            var referenceMedicalVolume = referenceMedicalVolumes.First().Volume;

            var referenceIdentifiers = referenceMedicalVolume.Identifiers.First();

            // Extract the RTStruct from the DICOM-RT file
            var reloaded = RtStructReader.LoadContours(
                dicomRTFile.Dataset,
                referenceMedicalVolume.Volume.Transform.DicomToData,
                referenceIdentifiers.Series.SeriesInstanceUid,
                referenceIdentifiers.Study.StudyInstanceUid);

            Assert.IsNotNull(reloaded);

            var reloadedRTStruct = reloaded.Item1;

            Assert.AreEqual(referenceIdentifiers.Patient.Id, reloadedRTStruct.Patient.Id);
            Assert.AreEqual(referenceIdentifiers.Study.StudyInstanceUid, reloadedRTStruct.Study.StudyInstanceUid);
            Assert.AreEqual(DicomRTSeries.RtModality, reloadedRTStruct.RTSeries.Modality);

            // Load the nifti file
            var sourceVolume = MedIO.LoadNiftiAsByte(expectedNiftiFilename);

            // Split this volume from segment id to a set of mask volumes
            var labels = VolumeMetadataMapper.MultiLabelMapping(sourceVolume, structureNames.Length);

            // Make tuples of mask volume, color, names, fill holes.
            var volumesWithMetadata = VolumeMetadataMapper.MapVolumeMetadata(labels, structureNames, structureColors, fillHoles, roiInterpretedTypes).ToArray();

            Assert.AreEqual(structureNames.Length, reloadedRTStruct.Contours.Count);

            for (int i = 0; i < reloadedRTStruct.Contours.Count; i++)
            {
                var contourVolumes = new Volume3D<byte>(
                    referenceMedicalVolume.Volume.DimX,
                    referenceMedicalVolume.Volume.DimY,
                    referenceMedicalVolume.Volume.DimZ,
                    referenceMedicalVolume.Volume.SpacingX,
                    referenceMedicalVolume.Volume.SpacingY,
                    referenceMedicalVolume.Volume.SpacingZ,
                    referenceMedicalVolume.Volume.Origin,
                    referenceMedicalVolume.Volume.Direction);

                contourVolumes.Fill(reloadedRTStruct.Contours[i].Contours, (byte)1);

                var v = volumesWithMetadata[i];

                if (!string.IsNullOrWhiteSpace(debugFolderName))
                {
                    for (var z = 0; z < referenceMedicalVolume.Volume.DimZ; z++)
                    {
                        var actualFileName = Path.Combine(debugFolderName, "actual", $"slice_{v.name}_{z}.png");
                        var actualSlice = contourVolumes.Slice(SliceType.Axial, z);
                        actualSlice.SaveBinaryMaskToPng(actualFileName);

                        var expectedFileName = Path.Combine(debugFolderName, "expected", $"slice_{v.name}_{z}.png");
                        var expectedSlice = v.volume.Slice(SliceType.Axial, z);
                        expectedSlice.SaveBinaryMaskToPng(expectedFileName);
                    }
                }

                if (testVolumesMatch)
                {
                    VolumeAssert.AssertVolumesMatch(v.volume, contourVolumes, $"Loaded mask {i}");
                }

                Assert.AreEqual(v.name, reloadedRTStruct.Contours[i].StructureSetRoi.RoiName, $"Loaded mask name {i}");

                var expectedColor = Tuple.Create(v.color.R, v.color.G, v.color.B);
                Assert.AreEqual(expectedColor, reloadedRTStruct.Contours[i].DicomRtContour.RGBColor, $"Loaded color {i}");
            }
        }
    }
}
