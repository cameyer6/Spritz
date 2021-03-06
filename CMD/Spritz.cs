﻿using CommandLine;
using Proteogenomics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using ToolWrapperLayer;
using WorkflowLayer;

namespace CMD
{
    public class Spritz
    {
        public static void Main(string[] args)
        {
            if (!WrapperUtility.CheckBashSetup())
            {
                throw new FileNotFoundException("The Windows Subsystem for Windows has not been enabled. Please see https://smith-chem-wisc.github.io/Spritz/ for more details.");
            }

            // main setup involves installing tools
            if (args.Contains(ManageToolsFlow.Command))
            {
                ManageToolsFlow.Install(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location));
                return;
            }

            Parsed<Options> result = Parser.Default.ParseArguments<Options>(args) as Parsed<Options>;
            if (result == null)
            {
                Console.WriteLine("Please use GUI.exe if you are a first time user of Spritz.");
                Console.WriteLine("It aims to guide you through setting up tools and running a workflow.");
                Console.WriteLine();
                Console.WriteLine("See above for commandline arguments for CMD.exe.");
                Console.WriteLine("    Required: -c for a command");
                Console.WriteLine("    1) Setting up tools: -c setup");
                Console.WriteLine("    2) Generating a protein database from ensembl: -c proteins");
                Console.WriteLine("    3) Analyzing variants: -c proteins");
                Console.WriteLine("          Also required: --fq1 (and --fq2 if paired-end) for FASTQ files that exist or -s to download an SRA (see https://www.ncbi.nlm.nih.gov/sra).");
                Console.WriteLine();
                Console.WriteLine("Press any key to exit.");
                Console.ReadKey();
                return;
            }

            Options options = result.Value;
            FinishSetup(options);

            // Download SRAs if they're specified
            bool useSraMethod = options.SraAccession != null && options.SraAccession.StartsWith("SR");
            List<string[]> fastqsSeparated = useSraMethod ?
                SRAToolkitWrapper.GetFastqsFromSras(options.SpritzDirectory, options.Threads, options.AnalysisDirectory, options.SraAccession) :
                SeparateFastqs(options.Fastq1, options.Fastq2);

            if (options.Command.Equals(SampleSpecificProteinDBFlow.Command, StringComparison.InvariantCultureIgnoreCase))
            {
                if (options.ReferenceVcf == null)
                {
                    options.ReferenceVcf = new GATKWrapper(1).DownloadEnsemblKnownVariantSites(options.SpritzDirectory, true, options.Reference, false);
                }

                if (options.UniProtXml == null)
                {
                    Console.WriteLine("Note: You can specify a UniProt XML file with the -x flag to transfer modificaitons and database references.");
                }

                // Parse the experiment type
                ExperimentType experimentType;
                if (options.ExperimentType == ExperimentType.RNASequencing.ToString())
                {
                    experimentType = ExperimentType.RNASequencing;
                }
                else if (options.ExperimentType == ExperimentType.WholeGenomeSequencing.ToString())
                {
                    experimentType = ExperimentType.WholeGenomeSequencing;
                }
                else if (options.ExperimentType == ExperimentType.ExomeSequencing.ToString())
                {
                    experimentType = ExperimentType.ExomeSequencing;
                }
                else
                {
                    throw new ArgumentException("Error: experiment type was not recognized.");
                }

                // Check that options make sense with experiment type
                if (options.DoTranscriptIsoformAnalysis && experimentType != ExperimentType.RNASequencing)
                {
                    throw new ArgumentException("Error: cannot do isoform analysis without RNA sequencing data.");
                }

                SampleSpecificProteinDBFlow flow = new SampleSpecificProteinDBFlow();
                flow.Parameters.SpritzDirectory = options.SpritzDirectory;
                flow.Parameters.AnalysisDirectory = options.AnalysisDirectory;
                flow.Parameters.Reference = options.Reference;
                flow.Parameters.Threads = options.Threads;
                flow.Parameters.Fastqs = fastqsSeparated;
                flow.Parameters.ExperimentType = experimentType;
                flow.Parameters.StrandSpecific = options.StrandSpecific;
                flow.Parameters.InferStrandSpecificity = options.InferStrandSpecificity;
                flow.Parameters.OverwriteStarAlignment = options.OverwriteStarAlignments;
                flow.Parameters.GenomeStarIndexDirectory = options.GenomeStarIndexDirectory;
                flow.Parameters.GenomeFasta = options.GenomeFasta;
                flow.Parameters.ProteinFastaPath = options.ProteinFastaPath;
                flow.Parameters.ReferenceGeneModelGtfOrGff = options.GeneModelGtfOrGff;
                flow.Parameters.NewGeneModelGtfOrGff = options.NewGeneModelGtfOrGff;
                flow.Parameters.EnsemblKnownSitesPath = options.ReferenceVcf;
                flow.Parameters.UniProtXmlPath = options.UniProtXml;
                flow.Parameters.SkipVariantAnalysis = options.SkipVariantAnalysis;
                flow.Parameters.DoTranscriptIsoformAnalysis = options.DoTranscriptIsoformAnalysis;
                flow.Parameters.DoFusionAnalysis = options.DoFusionAnalysis;
                flow.Parameters.IndelFinder = options.IndelFinder;
                flow.Parameters.VariantCallingWorkers = options.VariantCallingWorkers;
                flow.GenerateSampleSpecificProteinDatabases();

                Console.WriteLine("done");
            }

            else if (options.Command.Equals(LncRNADiscoveryFlow.Command, StringComparison.InvariantCultureIgnoreCase))
            {
                if (options.ExperimentType != null && !options.ExperimentType.Equals(ExperimentType.RNASequencing.ToString()))
                {
                    throw new ArgumentException("Error: lncRNA discovery requires RNA-Seq reads.");
                }
                LncRNADiscoveryFlow lnc = new LncRNADiscoveryFlow();
                lnc.Parameters.SpritzDirectory = options.SpritzDirectory;
                lnc.Parameters.AnalysisDirectory = options.AnalysisDirectory;
                lnc.Parameters.Reference = options.Reference;
                lnc.Parameters.Threads = options.Threads;
                lnc.Parameters.Fastqs = fastqsSeparated;
                lnc.Parameters.StrandSpecific = options.StrandSpecific;
                lnc.Parameters.InferStrandSpecificity = options.InferStrandSpecificity;
                lnc.Parameters.OverwriteStarAlignment = options.OverwriteStarAlignments;
                lnc.Parameters.GenomeStarIndexDirectory = options.GenomeStarIndexDirectory;
                lnc.Parameters.GenomeFasta = options.GenomeFasta;
                lnc.Parameters.ProteinFasta = options.ProteinFastaPath;
                lnc.Parameters.GeneModelGtfOrGff = options.GeneModelGtfOrGff;
                lnc.LncRNADiscoveryFromFastqs();
                return;
            }

            else if (options.Command.Equals(GeneFusionDiscoveryFlow.Command, StringComparison.InvariantCultureIgnoreCase))
            {
                if (options.ExperimentType != null && !options.ExperimentType.Equals(ExperimentType.RNASequencing.ToString()))
                {
                    throw new ArgumentException("Error: gene fusion discovery with STAR fusion requires RNA-Seq reads.");
                }

                GeneFusionDiscoveryFlow flow = new GeneFusionDiscoveryFlow();
                flow.Parameters.SpritzDirectory = options.SpritzDirectory;
                flow.Parameters.AnalysisDirectory = options.AnalysisDirectory;
                flow.Parameters.Reference = options.Reference;
                flow.Parameters.Threads = options.Threads;
                flow.Parameters.Fastqs = fastqsSeparated;
                flow.DiscoverGeneFusions();
                return;
            }

            else if (options.Command.Equals(TransferModificationsFlow.Command))
            {
                string[] xmls = options.UniProtXml.Split(',');
                TransferModificationsFlow transfer = new TransferModificationsFlow();
                transfer.TransferModifications(options.SpritzDirectory, xmls[0], xmls[1]);
                return;
            }

            else if (options.Command.Equals(TranscriptQuantificationFlow.Command, StringComparison.InvariantCultureIgnoreCase))
            {
                if (options.ExperimentType != null && !options.ExperimentType.Equals(ExperimentType.RNASequencing.ToString()))
                {
                    throw new ArgumentException("Error: transcript quantification requires RNA-Seq reads.");
                }

                foreach (string[] fastq in fastqsSeparated)
                {
                    Strandedness strandedness = options.StrandSpecific ? Strandedness.Forward : Strandedness.None;
                    if (options.InferStrandSpecificity)
                    {
                        var bamProps = AlignmentFlow.InferStrandedness(options.SpritzDirectory, options.AnalysisDirectory, options.Threads,
                            fastq, options.GenomeStarIndexDirectory, options.GenomeFasta, options.GeneModelGtfOrGff);
                        strandedness = bamProps.Strandedness;
                    }
                    TranscriptQuantificationFlow quantify = new TranscriptQuantificationFlow();
                    quantify.Parameters = new TranscriptQuantificationParameters(
                        options.SpritzDirectory,
                        options.AnalysisDirectory,
                        options.GenomeFasta,
                        options.Threads,
                        options.GeneModelGtfOrGff,
                        RSEMAlignerOption.STAR,
                        strandedness,
                        fastq,
                        true);
                    quantify.QuantifyTranscripts();
                }
                return;
            }

            else if (options.Command.Equals("strandedness"))
            {
                string[] fastqs = options.Fastq2 == null ?
                    new[] { options.Fastq1 } :
                    new[] { options.Fastq1, options.Fastq2 };
                BAMProperties b = AlignmentFlow.InferStrandedness(options.SpritzDirectory, options.AnalysisDirectory, options.Threads,
                        fastqs, options.GenomeStarIndexDirectory, options.GenomeFasta, options.GeneModelGtfOrGff);
                Console.WriteLine(b.ToString());
                return;
            }

            else
            {
                throw new ArgumentException($"Error: command not recognized, {options.Command}");
            }
        }

        /// <summary>
        /// Always download reference that aren't present and set default options
        /// </summary>
        /// <param name="options"></param>
        public static void FinishSetup(Options options)
        {
            // Download ensembl references and set default paths
            EnsemblDownloadsWrapper downloadsWrapper = new EnsemblDownloadsWrapper();
            downloadsWrapper.DownloadReferences(options.SpritzDirectory, options.SpritzDirectory, options.Reference, false);
            options.GenomeFasta = options.GenomeFasta ?? downloadsWrapper.GenomeFastaPath;
            options.GeneModelGtfOrGff = options.GeneModelGtfOrGff ?? downloadsWrapper.Gff3GeneModelPath;
            options.GenomeStarIndexDirectory = options.GenomeStarIndexDirectory ?? STARWrapper.GetGenomeStarIndexDirectoryPath(options.GenomeFasta, options.GeneModelGtfOrGff);
            options.ProteinFastaPath = options.ProteinFastaPath ?? downloadsWrapper.ProteinFastaPath;
        }

        /// <summary>
        /// Split the fastq lists into a list of paired strings
        /// </summary>
        /// <param name="fastq1string"></param>
        /// <param name="fastq2string"></param>
        /// <returns></returns>
        private static List<string[]> SeparateFastqs(string fastq1string, string fastq2string)
        {
            List<string[]> fastqsSeparated = null;
            if (fastq1string != null)
            {
                // Parse comma-separated fastq lists
                if (fastq2string != null && fastq1string.Count(x => x == ',') != fastq2string.Count(x => x == ','))
                {
                    throw new ArgumentException("Error: There are a different number of first-strand and second-strand fastq files.");
                }
                string[] fastqs1 = fastq1string.Split(',');
                fastqsSeparated = fastq2string == null ?
                    fastqs1.Select(x => new string[] { x }).ToList() :
                    fastqs1.Select(x => new string[] { x, fastq2string.Split(',')[fastqs1.ToList().IndexOf(x)] }).ToList();
            }
            return fastqsSeparated;
        }
    }
}