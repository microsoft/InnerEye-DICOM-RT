// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.RTConvert.Console
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using CommandLine;
    using CommandLine.Text;
    using Microsoft.RTConvert.Converters;

    class Options
    {
        /// <summary>
        /// Path to file on disk containing input NIFTI file.
        /// </summary>
        [Option('i', "in-file", Required = true, HelpText = "Path to the input Nifti file.")]
        public string InFile { get; set; }

        /// <summary>
        /// Path to folder on disk containing reference DICOM series.
        /// </summary>
        [Option('r', "reference-series", Required = true, HelpText = "Path to the input folder containing the reference DICOM series.")]
        public string ReferenceSeries { get; set; }

        /// <summary>
        /// Output DICOM-RT files.
        /// </summary>
        [Option('o', "out-file", Required = true, HelpText = "Path to the output DICOM-RT file.")]
        public string OutFile { get; set; }

        /// <summary>
        /// List of structure names.
        /// </summary>
        [Option('n', "struct-names", Required = true, HelpText = "List of comma separated structure names.")]
        public string StructureNames { get; set; }

        /// <summary>
        /// List of structure colors.
        /// </summary>
        [Option('c', "struct-colors", Required = true, HelpText = "List of comma separated structure colors in hexadecimal notation.")]
        public string StructureColors { get; set; }

        /// <summary>
        /// List of fill hole flags.
        /// </summary>
        [Option('f', "fill-holes", Required = true, HelpText = "List of comma separated flags, whether to fill holes in the structure.")]
        public string FillHoles { get; set; }

        /// <summary>
        /// List of ROIInterpretedTypes. Possible values (None, CTV, ORGAN, EXTERNAL)
        /// </summary>
        [Option('t', "roi-interpreted-types", Required = true, HelpText = "List of comma separated strings specifiying the ROIInterpretedTypes for each structure. Possible values (None, CTV, ORGAN, EXTERNAL).")]
        public string ROIInterpretedTypes { get; set; }

        /// <summary>
        /// Manufacturer
        /// </summary>
        [Option('m', "manufacturer", Required = true, HelpText = "Manufacturer for the DICOM-RT")]
        public string Manufacturer { get; set; }

        /// <summary>
        /// Interpreter
        /// </summary>
        [Option('t', "interpreter", Required = true, HelpText = "Interpreter for the DICOM-RT")]
        public string Interpreter { get; set; }

        /// <summary>
        /// Model name and version
        /// </summary>
        [Option('d', "modelId", Required = true, HelpText = "Model name and version. This will be added to the SoftwareVersions in DICOM-RT")]
        public string ModelNameAndVersion { get; set; }

        [Usage(ApplicationAlias = "dotnet Microsoft.RTConvert.Console.dll")]
        public static IEnumerable<Example> Examples
        {
            get
            {
                return new List<Example>() {
                    new Example("Convert segmentation file in Nifti format, and reference DICOM series to file in DICOM-RT format", new Options {
                        InFile = "data/segmentation.nii.gz",
                        ReferenceSeries = "data/dicom",
                        OutFile = "out.dcm",
                        StructureNames = "External, parotid_l, parotid_r, smg_l",
                        StructureColors = "000000, FF0080, 00FF00, 0000FF",
                        FillHoles = "True, False, True",
                        ROIInterpretedTypes= "Organ, CTV, External",
                        ModelNameAndVersion = "ModelXYZ:234",
                        Manufacturer = "Contoso",
                        Interpreter = "AI"
                    })
                };
            }
        }
    }

    class Program
    {
        static int Main(string[] args)
        {
            if (args is null)
            {
                args = new string[] { };
            }

            var result = Parser.Default.ParseArguments<Options>(args).MapResult(
                (opts) => RunOptionsAndReturnExitCode(opts), //in case parser sucess
                errs => HandleParseError(errs));

            return result;
        }

        /// <summary>
        /// Handle options, if no errors.
        /// </summary>
        /// <param name="opts">Command line options</param>
        /// <returns>Exit code.</returns>
        static int RunOptionsAndReturnExitCode(Options opts)
        {
            try
            {
                RTConverters.ConvertNiftiToDicom(opts.InFile, opts.ReferenceSeries, opts.StructureNames,
                    opts.StructureColors, opts.FillHoles, opts.ROIInterpretedTypes, opts.OutFile, opts.ModelNameAndVersion, opts.Manufacturer, opts.Interpreter);

                Console.WriteLine($"Successfully written {opts.OutFile}");
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"Error while processing: {e}");
                return 1;
            }

            return 0;
        }

        /// <summary>
        /// Handle options errors, --help, or --version.
        /// </summary>
        /// <param name="errs">List of errors.</param>
        /// <returns>Exit code.</returns>
        static int HandleParseError(IEnumerable<Error> errs)
        {
            var result = -2;
            Console.WriteLine("errors {0}", errs.Count());
            if (errs.Any(x => x is HelpRequestedError || x is VersionRequestedError))
                result = -1;

            return result;
        }
    }
}
