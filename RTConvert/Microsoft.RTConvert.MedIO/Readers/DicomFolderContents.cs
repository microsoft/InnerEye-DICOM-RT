// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


namespace Microsoft.RTConvert.MedIO.Readers
{
    using System.IO;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Dicom;
    using RT;

    public sealed class DicomFolderContents
    {
        /// <summary>
        /// The list of {SeriesUID, SOPInstance list} for recognized image types CT & MR in this folder 
        /// </summary>
        public IReadOnlyList<DicomSeriesContent> Series { get; private set; }

        /// <summary>
        /// The list of {Referenced SeriesUID, SOPInstance list} for recognized RTStructs in this folder. 
        /// </summary>
        public IReadOnlyList<DicomSeriesContent> RTStructs { get; private set; }

        /// <summary>
        /// The folder path this instance was generated from.
        /// </summary>
        public string FolderPath { get; private set; }

        public static DicomFolderContents Build(IReadOnlyList<DicomFileAndPath> fileAndPaths)
        {
            // Changed for new OSS fo-dicom-desktop
            // Extract the RT structs and group by referenced SeriesUID
            var rtStructs = fileAndPaths.Where((fp) => fp.File.Dataset.GetSingleValue<DicomUID>(DicomTag.SOPClassUID) == DicomUID.RTStructureSetStorage);
            //var rtStructs = fileAndPaths.Where((fp) => fp.File.Dataset.Get(DicomTag.SOPClassUID, DicomExtensions.EmptyUid) == DicomUID.RTStructureSetStorage);

            // We assume there is 1 and only 1 referenced series
            var parsedReferencedFoR = rtStructs.GroupBy(
                (rt) => DicomRTStructureSet.Read(rt.File.Dataset).
                    ReferencedFramesOfRef.FirstOrDefault()?.
                    ReferencedStudies?.FirstOrDefault()?.
                    ReferencedSeries.FirstOrDefault()?.
                    SeriesInstanceUID ?? string.Empty
            );
            // Changed for new OSS fo-dicom-desktop
            // Filter out the CT and MR SOPClasses
            var ctMR = fileAndPaths.Where((fp) => IsSupportedImageSOPClass(fp.File.Dataset.GetSingleValue<DicomUID>(DicomTag.SOPClassUID)));

            // Group by seriesUID
            var ctMRGroups = ctMR.GroupBy((fp) => fp.File.Dataset.GetSingleValue<DicomUID>(DicomTag.SeriesInstanceUID));

            //// Filter out the CT and MR SOPClasses
            //var ctMR = fileAndPaths.Where((fp) => IsSupportedImageSOPClass(fp.File.Dataset.Get(DicomTag.SOPClassUID, DicomExtensions.EmptyUid)));

            //// Group by seriesUID
            //var ctMRGroups = ctMR.GroupBy((fp) => fp.File.Dataset.Get<DicomUID>(DicomTag.SeriesInstanceUID));

            // construct output
            var seriesContent = ctMRGroups.Select((g) => new DicomSeriesContent(g.Key, g.ToList()));

            // RT structs without frame of reference information will be group into a null DicomUID entry,
            var rtContent = parsedReferencedFoR.Select((g) => new DicomSeriesContent(DicomUID.Parse(g.Key), g.ToList()));

            return new DicomFolderContents(seriesContent.ToList(), rtContent.ToList());
        }

        private DicomFolderContents(IReadOnlyList<DicomSeriesContent> series, IReadOnlyList<DicomSeriesContent> rtStructs)
        {
            Series = series;
            RTStructs = rtStructs;
        }

        private static bool IsSupportedImageSOPClass(DicomUID id)
        {
            return id == DicomUID.CTImageStorage || id == DicomUID.MRImageStorage;
        }
    }

    /// <summary>
    /// Construct a DicomFolderContents based upon a folder in the file system. 
    /// </summary>
    public sealed class DicomFileSystemSource
    {

        /// <summary>
        /// Return a task to asynchronously inspect and arrange all dicom folders in the given path. It is left to 
        /// the caller to insure the folderPath exists and is readable. 
        /// </summary>
        /// <param name="folderPath"></param>
        /// <returns></returns>
        public static async Task<DicomFolderContents> Build(string folderPath)
        {
            // Open all the DICOM files and read series ids
            var paths = Directory.EnumerateFiles(folderPath, "*.*");

            // Task.Run is needed because fo-dicom async doesnt work properly
            var fileAndPaths = (await Task.WhenAll(paths.Select(x => Task.Run(() => DicomFileAndPath.SafeCreate(x))))).Where(x => x != null);

            return DicomFolderContents.Build(fileAndPaths.ToList());
        }
    }
}