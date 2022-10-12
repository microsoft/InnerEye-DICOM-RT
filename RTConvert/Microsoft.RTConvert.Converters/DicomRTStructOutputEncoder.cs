// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
namespace Microsoft.RTConvert.Converters
{
    using Dicom;
    using Microsoft.RTConvert.MedIO.Models;
    using Microsoft.RTConvert.MedIO.Models.DicomRT;
    using System.Collections.Generic;
    using System.Linq;
    using System;
    using Microsoft.RTConvert.MedIO.Writers;
    using Microsoft.RTConvert.MedIO.RT;
    using System.IO;
    using Microsoft.RTConvert.Models;
    using Microsoft.RTConvert.Contours;
    using Microsoft.RTConvert.Converters.Models;
    /// <summary>
    /// Creates RT Structs from segmentation output
    /// </summary>
    public class DicomRTStructOutputEncoder : ISegmentationOutputEncoder
    {
        private static readonly RGBColor DefaultColour = new RGBColor(255, 0, 0);

        public SegmentationOutputEncoding OutputEncoding => SegmentationOutputEncoding.RTStruct;

        public DicomFile EncodeStructures(
            IEnumerable<(string name, Volume3D<byte> volume, RGBColor color, bool fillHoles, ROIInterpretedType roiInterpretedType)> outputStructuresWithMetadata,
            IReadOnlyDictionary<string, MedicalVolume> inputChannels,
            string modelNameAndVersion,
            string manufacturer,
            string interpreter)
        {
            // the first channel is always the master contouring one
            var masterImage = inputChannels.FirstOrDefault().Value;

            var structureSetFile = CreateStructureSetFile(
                masterImage,
                outputStructuresWithMetadata,
                modelNameAndVersion,
                manufacturer,
                interpreter);

            return structureSetFile;
        }

        private static DicomFile CreateStructureSetFile(
            MedicalVolume medicalVolume,
            IEnumerable<(string name, Volume3D<byte> volume, RGBColor color, bool fillHoles, ROIInterpretedType roiInterpretedType)> structures,
            string modelNameAndVersion,
            string manufacturer,
            string interpreter)
        {
            var radiotherapyStruct = RadiotherapyStruct.CreateDefault(medicalVolume.Identifiers);

            // Extract pixel coordinate contours for each structure & slice
            var structuresAndContours = structures.Select(
                x =>
                    (x.fillHoles ?
                        ExtractContours.ContoursFilledPerSlice(x.volume) :
                        ExtractContours.ContoursWithHolesPerSlice(x.volume),
                    x.name,
                    x.color,
                    x.roiInterpretedType));

            int i = 1; // ROIs need to start at 1 by DICOM spec

            foreach (var (contours, name, color, roiInterpretedType) in structuresAndContours)
            {
                // Record and increment the roiNumber
                var roiNumberString = i.ToString();
                i++;
                // Create contours - mapping each contour into the volume. 
                var radiotherapyContour = RTStructCreator.CreateRadiotherapyContour(
                    contours,
                    medicalVolume.Identifiers,
                    medicalVolume.Volume.Transform,
                    name: name,
                    color: (color.R, color.G, color.B),
                    roiNumber: roiNumberString,
                    interpreterName: new DicomPersonNameConverter(interpreter, interpreter, string.Empty, string.Empty, string.Empty),
                    roiInterpretedType: roiInterpretedType);
                    radiotherapyStruct.Contours.Add(radiotherapyContour);
            }

            AddVersioning(modelNameAndVersion, manufacturer, radiotherapyStruct);

            return RtStructWriter.GetRtStructFile(radiotherapyStruct);
        }

        private static void AddVersioning(string modelNameAndVersion, string manufacturer, RadiotherapyStruct radiotherapyStruct)
        {
            // Add versioning to the RT file
            radiotherapyStruct.Equipment.Manufacturer = manufacturer;
            radiotherapyStruct.Equipment.SoftwareVersions = modelNameAndVersion;
        }
    }
}

