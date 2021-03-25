// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.RTConvert.Converters.Tests
{
    using NUnit.Framework;
    
    using System.Diagnostics;
    using System.Linq;
    using System.Threading.Tasks;

    using Microsoft.RTConvert.MedIO;
    using Microsoft.RTConvert.Converters;
    using Microsoft.RTConvert.Models;
    using static TestHelper;

    public class VolumeMetadataMapperTests
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

        private int CountAndValidateVoxelsInLabel(Volume3D<byte> volume)
        {
            int voxelcount = 0;
            for (var i = 0; i < volume.Length; i++)
            {
                voxelcount += volume[i];
                if (volume[i] > 1)
                    Assert.Fail("Invalid data in converted image. Should only be ones or zeroes");
            }
            return voxelcount;
        }


        /// <summary>
        /// Checks that NIFTI label mappings are correctly extracted
        /// </summary>
        [Test]
        public void MultiLabelVolumeMapping_SuccessWithValidInputs()
        {
            var labels = VolumeMetadataMapper.MultiLabelMapping(sourceVolume, NumValidLabels);

            Assert.AreEqual(labels.Length, NumValidLabels);
            Parallel.ForEach(labels, (img) =>
            {
                Assert.AreEqual(img.DimX, sourceVolume.DimX);
                Assert.AreEqual(img.DimY, sourceVolume.DimY);
                Assert.AreEqual(img.DimZ, sourceVolume.DimZ);

                Assert.AreEqual(img.SpacingX, sourceVolume.SpacingX);
                Assert.AreEqual(img.SpacingY, sourceVolume.SpacingY);
                Assert.AreEqual(img.SpacingZ, sourceVolume.SpacingZ);

                Assert.AreNotEqual(CountAndValidateVoxelsInLabel(img), 0);
            }
            );
        }

        /// <summary>
        /// Checks that NIFTI label mappings are correctly extracted when more labels are requested than we have
        /// </summary>
        [Test]
        public void MultiLabelVolumeMapping_SuccessWithTooManyRequestedLabels()
        {
            const int LabelsRequested = NumValidLabels + 1;

            var labels = VolumeMetadataMapper.MultiLabelMapping(sourceVolume, LabelsRequested);
            Assert.AreEqual(labels.Length, LabelsRequested);

            Assert.AreNotEqual(CountAndValidateVoxelsInLabel(labels[labels.Length - 2]), 0);
            Assert.AreEqual(CountAndValidateVoxelsInLabel(labels[labels.Length - 1]), 0);
        }

        /// <summary>
        /// Checks that NIFTI label mappings are correctly extracted when fewer labels are requested than available
        /// </summary>
        [Test]
        public void MultiLabelVolumeMapping_SuccessWithTooFewRequestedLabels()
        {
            const int LabelsRequested = 2;
            var labels = VolumeMetadataMapper.MultiLabelMapping(sourceVolume, LabelsRequested);
            Assert.AreEqual(labels.Length, LabelsRequested);

            Assert.AreNotEqual(CountAndValidateVoxelsInLabel(labels[0]), 0);
            Assert.AreNotEqual(CountAndValidateVoxelsInLabel(labels[1]), 0);
        }

        /// <summary>
        /// Checks that metadata is correctly mapped to volumes
        /// </summary>
        [Test]
        public void MapVolumeMetadata_SuccessWithValidInputs()
        {
            var labels = VolumeMetadataMapper.MultiLabelMapping(sourceVolume, NumValidLabels);
            // we will use voxel counts as a hash for our volumes
            var labelVoxelCounts = labels.AsParallel().Select(l => CountAndValidateVoxelsInLabel(l)).ToArray();

            var volumesWithMetadata = VolumeMetadataMapper.MapVolumeMetadata(labels, StructureNames, StructureColors, FillHoles, ROIInterpretedTypes);
            // pre-compute voxel counts in parallel
            var mappedVolumesVoxelCounts = volumesWithMetadata.AsParallel().Select(l => CountAndValidateVoxelsInLabel(l.volume)).ToArray();
            Assert.AreEqual(labels.Length, volumesWithMetadata.Count());

            for (int i = 0; i < NumValidLabels; i++)
            {
                Assert.AreEqual(labelVoxelCounts.ElementAt(i), mappedVolumesVoxelCounts.ElementAt(i));
                Assert.AreEqual(StructureNames[i], volumesWithMetadata.ElementAt(i).name);

                var expectedStructureColor = StructureColors[i].Value.ApplyDefault(null);
                Assert.AreEqual(expectedStructureColor, volumesWithMetadata.ElementAt(i).color);
                Assert.AreEqual(FillHoles[i], volumesWithMetadata.ElementAt(i).fillHoles);
            }
        }

        /// <summary>
        /// Checks that metadata is correctly mapped when there is insufficient metadata with missing metadata set to default values
        /// </summary>
        [Test]
        public void MapVolumeMetadata_SuccessWithIncompleteMetadata()
        {
            int numLabels = 5;

            var labels = VolumeMetadataMapper.MultiLabelMapping(sourceVolume, numLabels);

            var structureNames = new []{ "struct1", "struct2" };
            var structureColors = (new [] { "1,2,3", "4,65,  8", "12, 22,510" }).Select(ParseColorOption).ToArray();
            var fillHoles = (new [] { "true", "True" }).Select(ParseBoolOption).ToArray();

            var volumesWithMetadata = VolumeMetadataMapper.MapVolumeMetadata(labels, structureNames, structureColors, fillHoles, ROIInterpretedTypes);
            Assert.AreEqual(labels.Length, volumesWithMetadata.Count());

            var expectedStructureNames = structureNames.Concat(
                new[] {
                    VolumeMetadataMapper.DefaultStructureName.Invoke(1),
                    VolumeMetadataMapper.DefaultStructureName.Invoke(2),
                    VolumeMetadataMapper.DefaultStructureName.Invoke(3) }).ToArray();

            var expectedStructureColors = new[] { new RGBColor(1, 2, 3), new RGBColor(4, 65, 8), new RGBColor(12, 22, 0),
                VolumeMetadataMapper.DefaultStructureColor, VolumeMetadataMapper.DefaultStructureColor}.ToArray();

            var expectedFillHoles = new[] { true, true,
                VolumeMetadataMapper.DefaultFillHoles, VolumeMetadataMapper.DefaultFillHoles, VolumeMetadataMapper.DefaultFillHoles }.ToArray();

            for (var i = 0; i < numLabels; i++)
            {
                Assert.AreEqual(expectedStructureNames[i], volumesWithMetadata.ElementAt(i).name);
                Assert.AreEqual(expectedStructureColors[i], volumesWithMetadata.ElementAt(i).color);
                Assert.AreEqual(expectedFillHoles[i], volumesWithMetadata.ElementAt(i).fillHoles);
            }
        }

        /// <summary>
        /// Checks that metadata is correctly mapped when there is more metadata than actual labels
        /// </summary>
        [Test]
        public void MapVolumeMetadata_SuccessWithExtraMetadata()
        {
            int numLabels = 5;

            var labels = VolumeMetadataMapper.MultiLabelMapping(sourceVolume, numLabels);
            var labelVoxelCounts = labels.AsParallel().Select(l => CountAndValidateVoxelsInLabel(l)).ToArray();

            var volumesWithMetadata = VolumeMetadataMapper.MapVolumeMetadata(labels, StructureNames, StructureColors, FillHoles, ROIInterpretedTypes);
            var mappedVolumesVoxelCounts = volumesWithMetadata.AsParallel().Select(l => CountAndValidateVoxelsInLabel(l.volume)).ToArray();
            Assert.AreEqual(numLabels, volumesWithMetadata.Count());

            for (int i = 0; i < labelVoxelCounts.Count(); i++)
            {
                Assert.AreEqual(labelVoxelCounts[i], mappedVolumesVoxelCounts[i]);
            }
        }
    }
}