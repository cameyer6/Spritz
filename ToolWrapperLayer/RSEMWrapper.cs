﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace ToolWrapperLayer
{
    public enum RSEMAlignerOption
    {
        STAR,
        Bowtie1,
        Bowtie2,
    }

    /// <summary>
    /// RSEM is a program for calculating RNA-Seq Abundancy by Estimation Maximization.
    /// </summary>
    public class RSEMWrapper :
        IInstallable
    {
        public string ReferenceIndexPrefix { get; private set; }
        public string OutputPrefix { get; private set; }

        public static string IsoformResultsSuffix { get; } = ".isoforms.results";
        public static string GeneResultsSuffix { get; } = ".genes.results";
        public static string TranscriptBamSuffix { get; } = ".transcript.bam";
        public static string GenomeBamSuffix { get; } = ".genome.bam";
        public static string GenomeSortedBamSuffix { get; } = ".genome.sorted.bam";
        public static string GenomeSortedBamIndexSuffix { get; } = ".genome.sorted.bam.bai";
        public static string TimeSuffix { get; } = ".time";
        public static string StatDirectorySuffix { get; } = ".stat";

        #region Installation Methods

        /// <summary>
        /// Writes an installation script for RSEM.
        /// </summary>
        /// <param name="spritzDirectory"></param>
        /// <returns></returns>
        public string WriteInstallScript(string spritzDirectory)
        {
            string scriptPath = Path.Combine(spritzDirectory, "scripts", "installScripts", "installRSEM.bash");
            WrapperUtility.GenerateScript(scriptPath, new List<string>
            {
                "cd " + WrapperUtility.ConvertWindowsPath(spritzDirectory),
                "wget https://github.com/deweylab/RSEM/archive/v1.3.0.tar.gz",
                "tar -xvf v1.3.0.tar.gz",
                "cd RSEM-1.3.0",
                "make",
            });
            return scriptPath;
        }

        /// <summary>
        /// Writes a script for removing RSEM.
        /// </summary>
        /// <param name="spritzDirectory"></param>
        /// <returns></returns>
        public string WriteRemoveScript(string spritzDirectory)
        {
            string scriptPath = Path.Combine(spritzDirectory, "scripts", "installScripts", "removeRSEM.bash");
            WrapperUtility.GenerateScript(scriptPath, new List<string>
            {
                "cd " + WrapperUtility.ConvertWindowsPath(spritzDirectory),
                "rm -rf RSEM-1.3.0",
            });
            return scriptPath;
        }

        #endregion Installation Methods

        /// <summary>
        /// Gets commands to prepare an RSEM reference
        /// </summary>
        /// <param name="spritzDirectory"></param>
        /// <param name="referenceFastaPath"></param>
        /// <param name="referencePrefix"></param>
        /// <param name="threads"></param>
        /// <param name="geneModelPath"></param>
        /// <param name="aligner"></param>
        /// <returns></returns>
        public List<string> PrepareReferenceCommands(string spritzDirectory, string referenceFastaPath, int threads, string geneModelPath, RSEMAlignerOption aligner)
        {
            // make option strings, including putting reference files into a new directory
            string alignerOption = GetAlignerOption(spritzDirectory, aligner);
            string threadOption = "--num-threads " + threads.ToString();
            string referencePrefixDirectory = Path.Combine(Path.GetDirectoryName(referenceFastaPath), Path.GetFileNameWithoutExtension(referenceFastaPath)) +
                (aligner == RSEMAlignerOption.STAR ? "RsemStarReference" : "RsemBowtieReference") +
                "_GeneModel" + geneModelPath.GetHashCode().ToString();
            ReferenceIndexPrefix = Path.Combine(referencePrefixDirectory, Path.GetFileNameWithoutExtension(referenceFastaPath));
            string geneModelOption = Path.GetExtension(geneModelPath).StartsWith(".gff") ? "--gff3 " + WrapperUtility.ConvertWindowsPath(geneModelPath) :
                Path.GetExtension(geneModelPath) == ".gtf" ? "--gtf " + WrapperUtility.ConvertWindowsPath(geneModelPath) :
                null;

            // construct the commands
            var scriptStrings = new List<string>
            {
                "cd " + WrapperUtility.ConvertWindowsPath(Path.Combine(spritzDirectory, "RSEM-1.3.0")),
                "mkdir " + WrapperUtility.ConvertWindowsPath(referencePrefixDirectory),
                "if [[ ! -f " + WrapperUtility.ConvertWindowsPath(Path.Combine(referencePrefixDirectory, "SA")) + " && ! -s " + WrapperUtility.ConvertWindowsPath(Path.Combine(referencePrefixDirectory, "SA")) + " ]]; then " +
                    "./rsem-prepare-reference " +
                        geneModelOption + " "  +
                        alignerOption + " " +
                        threadOption + " " +
                        WrapperUtility.ConvertWindowsPath(referenceFastaPath) + " " +
                        WrapperUtility.ConvertWindowsPath(ReferenceIndexPrefix) +
                "; fi"
            };
            return scriptStrings;
        }

        /// <summary>
        /// Gets commands to calculate expression an RSEM reference
        /// </summary>
        /// <param name="spritzDirectory"></param>
        /// <returns></returns>
        public List<string> CalculateExpressionCommands(string spritzDirectory, string referencePrefix, int threads, RSEMAlignerOption aligner, Strandedness strandedness,
            string[] fastqPaths, bool doOuptutBam)
        {
            if (fastqPaths.Length < 1)
            {
                throw new ArgumentOutOfRangeException("No fastq files were given for RSEM calculate expression.");
            }
            if (fastqPaths.Length > 2)
            {
                throw new ArgumentOutOfRangeException("Too many fastq file types given for RSEM calculate expression.");
            }

            string alignerOption = GetAlignerOption(spritzDirectory, aligner);
            string threadOption = "--num-threads " + threads.ToString();
            string strandOption = "--strandedness " + strandedness.ToString().ToLowerInvariant();
            bool fastqIsGunzipped = fastqPaths[0].EndsWith(".gz");
            bool fastqIsBunzipped = fastqPaths[0].EndsWith(".bz2") || fastqPaths[0].EndsWith(".bz") || fastqPaths[0].EndsWith(".tbz") || fastqPaths[0].EndsWith(".tbz2");
            string compressionOption = fastqIsGunzipped && aligner == RSEMAlignerOption.STAR ? "--star-gzipped-read-file" :
                fastqIsBunzipped && aligner == RSEMAlignerOption.STAR ? "--star-bzipped-read-file" :
                "";
            string inputOption = fastqPaths.Length == 1 ? String.Join(",", fastqPaths[0].Split(',').Select(f => WrapperUtility.ConvertWindowsPath(f))) :
                "--paired-end " +
                    String.Join(",", fastqPaths[0].Split(',').Select(f => WrapperUtility.ConvertWindowsPath(f))) +
                    " " +
                    String.Join(",", fastqPaths[1].Split(',').Select(f => WrapperUtility.ConvertWindowsPath(f)));
            var megabytes = Math.Floor(new PerformanceCounter("Memory", "Available MBytes").NextValue());
            string bamOption = doOuptutBam ? "--output-genome-bam" : "--no-bam-output";
            OutputPrefix = Path.Combine(Path.GetDirectoryName(fastqPaths[0].Split(',')[0]), Path.GetFileNameWithoutExtension(fastqPaths[0].Split(',')[0]) + "_reference" + referencePrefix.GetHashCode().ToString());

            // RSEM likes to sort the transcript.bam file, which takes forever and isn't very useful, I've found. Just sort the genome.bam file instead
            string samtoolsCommands = !doOuptutBam ?
                "" :
                "if [[ ! -f " + WrapperUtility.ConvertWindowsPath(OutputPrefix + GenomeSortedBamSuffix) + " && ! -s " + WrapperUtility.ConvertWindowsPath(OutputPrefix + GenomeSortedBamSuffix) + " ]]; then\n" +
                    "  " + SamtoolsWrapper.SortBam(spritzDirectory, OutputPrefix + GenomeBamSuffix) + "\n" +
                    "  " + SamtoolsWrapper.IndexBamCommand(spritzDirectory, OutputPrefix + GenomeSortedBamSuffix) + "\n" +
                    "fi";

            // construct the commands
            var scriptStrings = new List<string>
            {
                "cd " + WrapperUtility.ConvertWindowsPath(Path.Combine(spritzDirectory, "RSEM-1.3.0")),
                "if [[ ! -f " + WrapperUtility.ConvertWindowsPath(OutputPrefix + IsoformResultsSuffix) + " && ! -s " + WrapperUtility.ConvertWindowsPath(OutputPrefix + IsoformResultsSuffix) + " ]]; then " +
                    "./rsem-calculate-expression " +
                        "--time " + // include timed results
                        "--calc-ci " + // posterior calculation of 95% confidence intervals
                        alignerOption + " " +
                        threadOption + " " +
                        compressionOption + " " +
                        bamOption + " " +
                        inputOption + " " +
                        WrapperUtility.ConvertWindowsPath(referencePrefix) + " " +
                        WrapperUtility.ConvertWindowsPath(OutputPrefix) +
                "; fi",
                samtoolsCommands
            };
            return scriptStrings;
        }

        /// <summary>
        /// Make sure the aligner is supported
        /// </summary>
        /// <param name="aligner"></param>
        public static string GetAlignerOption(string spritzDirectory, RSEMAlignerOption aligner)
        {
            if (aligner == RSEMAlignerOption.Bowtie1)
            {
                throw new NotSupportedException("Use of Bowtie1 is not supported. Use STAR or Bowtie2 instead.");
            }
            string alignerOption = aligner == RSEMAlignerOption.STAR ? "--star --star-path " + WrapperUtility.ConvertWindowsPath(STARWrapper.GetStarDirectoryPath(spritzDirectory)) :
                aligner == RSEMAlignerOption.Bowtie2 ? "--bowtie2 --bowtie2-path " + WrapperUtility.ConvertWindowsPath(TopHatWrapper.GetBowtie2DirectoryPath(spritzDirectory)) :
                null;
            return alignerOption;
        }
    }
}