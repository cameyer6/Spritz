﻿using Proteogenomics;
using Proteomics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UsefulProteomicsDatabases;

namespace ToolWrapperLayer
{
    /// <summary>
    /// This class helps manage the downloading of Ensembl references, used for everything from alignment to annotaiton.
    /// </summary>
    public class EnsemblDownloadsWrapper
    {
        #region Primary Assembly Genome Fasta URLs and Filenames

        /// <summary>
        /// Primary assembly for GRCh37. See ftp://ftp.ensembl.org/pub/release-75/fasta/homo_sapiens/dna/README for more info.
        /// </summary>
        private static string GRCh37PrimaryAssemblyUrl = "ftp://ftp.ensembl.org/pub/release-75//fasta/homo_sapiens/dna/Homo_sapiens.GRCh37.75.dna.primary_assembly.fa.gz";

        /// <summary>
        /// Filename for Primary assembly for GRCh37. See ftp://ftp.ensembl.org/pub/release-75/fasta/homo_sapiens/dna/README for more info.
        /// </summary>
        public static string GRCh37PrimaryAssemblyFilename { get; } = "Homo_sapiens.GRCh37.75.dna.primary_assembly.fa";

        /// <summary>
        /// Primary assembly for GRCh38. See ftp://ftp.ensembl.org/pub/release-81/fasta/homo_sapiens/dna/README for more info.
        /// </summary>
        private static string GRCh38PrimaryAssemblyUrl = "ftp://ftp.ensembl.org/pub/release-81//fasta/homo_sapiens/dna/Homo_sapiens.GRCh38.dna.primary_assembly.fa.gz";

        /// <summary>
        /// Filename for Primary assembly for GRCh38. See ftp://ftp.ensembl.org/pub/release-81/fasta/homo_sapiens/dna/README for more info.
        /// </summary>
        public static string GRCh38PrimaryAssemblyFilename { get; } = "Homo_sapiens.GRCh38.dna.primary_assembly.fa";

        #endregion Primary Assembly Genome Fasta URLs and Filenames

        #region GTF Gene Model URLs and Filenames

        /// <summary>
        /// GTF gene model for GRCh37.
        /// </summary>
        private static string GRCh37GtfGeneModelUrl = "ftp://ftp.ensembl.org/pub/release-75/gtf/homo_sapiens/Homo_sapiens.GRCh37.75.gtf.gz";

        /// <summary>
        /// Filename for GRCh37 gene model.
        /// </summary>
        public static string GRCh37GtfGeneModelFilename { get; } = "Homo_sapiens.GRCh37.75.gtf";

        /// <summary>
        /// GTF gene model for GRCh38.
        /// </summary>
        private static string GRCh38GtfGeneModelUrl = "ftp://ftp.ensembl.org/pub/release-81/gtf/homo_sapiens/Homo_sapiens.GRCh38.81.gtf.gz";

        /// <summary>
        /// Filename for GRCh38 gene model.
        /// </summary>
        public static string GRCh38GtfGeneModelFilename { get; } = "Homo_sapiens.GRCh38.81.gtf";

        #endregion GTF Gene Model URLs and Filenames

        #region GFF3 Gene Model URLs and Filenames

        /// <summary>
        /// GFF3 gene model for GRCh38. Note, there are no gff3 gene models for GRCh37.
        /// </summary>
        private static string GRCh38Gff3GeneModelUrl = "ftp://ftp.ensembl.org/pub/release-81/gff3/homo_sapiens/Homo_sapiens.GRCh38.81.gff3.gz";

        /// <summary>
        /// Filename for GFF3 gene model for GRCh38.
        /// </summary>
        public static string GRCh38Gff3GeneModelFilename { get; } = "Homo_sapiens.GRCh38.81.gff3";

        #endregion GFF3 Gene Model URLs and Filenames

        #region Protein Fasta URLs and Filenames

        /// <summary>
        /// Protein fasta file (pep.all) for GRCh37.
        /// </summary>
        private static string GRCh37ProteinFastaUrl = "ftp://ftp.ensembl.org/pub/release-75//fasta/homo_sapiens/pep/Homo_sapiens.GRCh37.75.pep.all.fa.gz";

        /// <summary>
        /// Filename for Protein fasta file (pep.all) for GRCh37.
        /// </summary>
        public static string GRCh37ProteinFastaFilename { get; } = "Homo_sapiens.GRCh37.75.pep.all.fa";

        /// <summary>
        /// Protein fasta file (pep.all) for GRCh38.
        /// </summary>
        private static string GRCh38ProteinFastaUrl = "ftp://ftp.ensembl.org/pub/release-81//fasta/homo_sapiens/pep/Homo_sapiens.GRCh38.pep.all.fa.gz";

        /// <summary>
        /// Filename for Protein fasta file (pep.all) for GRCh38.
        /// </summary>
        public static string GRCh38ProteinFastaFilename { get; } = "Homo_sapiens.GRCh38.pep.all.fa";

        #endregion Protein Fasta URLs and Filenames

        public string GenomeFastaPath { get; private set; }
        public string GtfGeneModelPath { get; private set; }
        public string Gff3GeneModelPath { get; private set; }
        public string ProteinFastaPath { get; private set; }
        public Dictionary<string, string> ProteinAccessionSequence { get; private set; }
        public HashSet<string> BadProteinAccessions { get; private set; }
        public Dictionary<string, string> SelenocysteineProteinAccessions { get; private set; }
        public Genome EnsemblGenome { get; private set; }
        public string ReorderedFastaPath { get; private set; }

        /// <summary>
        /// Downloads Ensembl references for GRCh37 or GRCh38.
        ///
        /// Sets GenomeFastaPath, GtfGeneModelPath, Gff3GeneModelPath, and ProteinFastaPath properties.
        /// </summary>
        /// <param name="spritzDirectory"></param>
        /// <param name="targetDirectory"></param>
        /// <param name="reference"></param>
        /// <param name="genomeFastaPath"></param>
        /// <param name="gtfGeneModelPath"></param>
        /// <param name="gff3GeneModelPath"></param>
        /// <param name="proteinFastaPath"></param>
        public void DownloadReferences(string spritzDirectory, string targetDirectory, string reference, bool dryRun)
        {
            bool downloadGrch37 = string.Equals(reference, "GRCh37", StringComparison.CurrentCultureIgnoreCase);
            bool downloadGrch38 = string.Equals(reference, "GRCh38", StringComparison.CurrentCultureIgnoreCase);

            GenomeFastaPath = downloadGrch37 ?
                Path.Combine(targetDirectory, GRCh37PrimaryAssemblyFilename) :
                downloadGrch38 ?
                    Path.Combine(targetDirectory, GRCh38PrimaryAssemblyFilename) :
                    "";
            GtfGeneModelPath = downloadGrch37 ?
                Path.Combine(targetDirectory, GRCh37GtfGeneModelFilename) :
                downloadGrch38 ?
                    Path.Combine(targetDirectory, GRCh38GtfGeneModelFilename) :
                    "";
            Gff3GeneModelPath = downloadGrch37 ?
                GtfGeneModelPath :
                downloadGrch38 ?
                    Path.Combine(targetDirectory, GRCh38Gff3GeneModelFilename) :
                    "";
            ProteinFastaPath = downloadGrch37 ?
                Path.Combine(targetDirectory, GRCh37ProteinFastaFilename) :
                downloadGrch38 ?
                    Path.Combine(targetDirectory, GRCh38ProteinFastaFilename) :
                    "";

            if (!downloadGrch37 && !downloadGrch38 || dryRun)
                return;

            WrapperUtility.GenerateAndRunScript(WrapperUtility.GetAnalysisScriptPath(targetDirectory, "DownloadEnsemblReference.bash"), new List<string>
            {
                $"cd {WrapperUtility.ConvertWindowsPath(targetDirectory)}",
                $"if [ ! -f {Path.GetFileName(GenomeFastaPath)} ]; then wget -O - {(downloadGrch38 ? GRCh38PrimaryAssemblyUrl : GRCh37PrimaryAssemblyUrl)} | gunzip -c > {Path.GetFileName(GenomeFastaPath)}; fi",
                $"if [ ! -f {Path.GetFileName(GtfGeneModelPath)} ]; then wget -O - {(downloadGrch38 ? GRCh38GtfGeneModelUrl : GRCh37GtfGeneModelUrl)} | gunzip -c > {Path.GetFileName(GtfGeneModelPath)}; fi",
                $"if [ ! -f {Path.GetFileName(Gff3GeneModelPath)} ]; then wget -O - {(downloadGrch38 ? GRCh38Gff3GeneModelUrl : GRCh37GtfGeneModelUrl)} | gunzip -c > {Path.GetFileName(Gff3GeneModelPath)}; fi", // note GRCh37 calls the gtf url instead
                $"if [ ! -f {Path.GetFileName(ProteinFastaPath)} ]; then wget -O - {(downloadGrch38 ? GRCh38ProteinFastaUrl : GRCh37ProteinFastaUrl)} | gunzip -c > {Path.GetFileName(ProteinFastaPath)}; fi", // note GRCh37 calls the gtf url instead
            }).WaitForExit();

            //Genome.WriteFasta(new Genome(genomeFastaPath).KaryotypicOrder(), genomeFastaPath); // todo: try this for ordering contigs before alignments; does gtf then need to be reordered?
        }

        /// <summary>
        /// Converts UCSC chromosome names to Ensembl chromosome names.
        /// </summary>
        /// <param name="spritzDirectory"></param>
        /// <param name="reference"></param>
        /// <returns></returns>
        public static Dictionary<string, string> UCSC2EnsemblChromosomeMappings(string spritzDirectory, string reference)
        {
            bool useGrch37 = string.Equals(reference, "GRCh37", StringComparison.CurrentCultureIgnoreCase);
            bool useGrch38 = string.Equals(reference, "GRCh38", StringComparison.CurrentCultureIgnoreCase);
            Dictionary<string, string> chromMappings = File.ReadAllLines(useGrch37 ?
                Path.Combine(spritzDirectory, "Tools", "ChromosomeMappings", "GRCh37_UCSC2ensembl.txt") :
                Path.Combine(spritzDirectory, "Tools", "ChromosomeMappings", "GRCh38_UCSC2ensembl.txt"))
                .Select(line => line.Split('\t'))
                .Where(x => x.Length > 1)
                .ToDictionary(line => line[0], line => line[1]);
            return chromMappings;
        }

        /// <summary>
        /// Converts Ensembl chromosome names to UCSC chromosome names.
        /// </summary>
        /// <param name="spritzDirectory"></param>
        /// <param name="reference"></param>
        /// <returns></returns>
        public static Dictionary<string, string> Ensembl2UCSCChromosomeMappings(string spritzDirectory, string reference)
        {
            bool useGrch37 = string.Equals(reference, "GRCh37", StringComparison.CurrentCultureIgnoreCase);
            bool useGrch38 = string.Equals(reference, "GRCh38", StringComparison.CurrentCultureIgnoreCase);
            Dictionary<string, string> chromMappings = File.ReadAllLines(useGrch37 ?
                Path.Combine(spritzDirectory, "Tools", "ChromosomeMappings", "GRCh37_ensembl2UCSC.txt") :
                Path.Combine(spritzDirectory, "Tools", "ChromosomeMappings", "GRCh38_ensembl2UCSC.txt"))
                .Select(line => line.Split('\t'))
                .Where(x => x.Length > 1)
                .ToDictionary(line => line[0], line => line[1]);
            return chromMappings;
        }

        /// <summary>
        /// Converts the first column of a tab-separated file to UCSC chromosome names. Ignores rows starting with '#'. Ignores rows with UCSC accessions alredy.
        /// </summary>
        /// <param name="spritzDirectory"></param>
        /// <param name="reference"></param>
        /// <param name="inputPath"></param>
        /// <param name="outputPath"></param>
        public static string ConvertFirstColumnEnsembl2UCSC(string spritzDirectory, string reference, string inputPath)
        {
            var e2uMappings = Ensembl2UCSCChromosomeMappings(spritzDirectory, reference);
            var u2eMappings = UCSC2EnsemblChromosomeMappings(spritzDirectory, reference);
            string outputPath = Path.Combine(Path.GetDirectoryName(inputPath), Path.GetFileNameWithoutExtension(inputPath)) + ".ucsc" + Path.GetExtension(inputPath);
            using (StreamReader reader = new StreamReader(inputPath))
            using (StreamWriter writer = new StreamWriter(outputPath))
            {
                while (true)
                {
                    string line = reader.ReadLine();
                    if (line == null || line == "") { break; }
                    if (line.StartsWith("#")) { continue; }
                    string[] columns = line.Split('\t');
                    if (columns.Length == 0) { break; }
                    if (e2uMappings.TryGetValue(columns[0], out string ucscColumn) && ucscColumn != "") { columns[0] = ucscColumn; }
                    else if (u2eMappings.TryGetValue(columns[0], out string ensemblColumn) && ensemblColumn != "") { } // nothing to do, already UCSC
                    else { continue; } // did not recognize this chromosome name; filter it out
                    writer.WriteLine(string.Join("\t", columns));
                }
            }
            return outputPath;
        }

        /// <summary>
        /// Converts the first column of a tab-separated file to UCSC chromosome names. Ignores rows starting with '#'. Ignores rows with Ensembl accessions already.
        /// </summary>
        /// <param name="spritzDirectory"></param>
        /// <param name="reference"></param>
        /// <param name="inputPath"></param>
        /// <param name="outputPath"></param>
        public static string ConvertFirstColumnUCSC2Ensembl(string spritzDirectory, string reference, string inputPath)
        {
            var e2uMappings = Ensembl2UCSCChromosomeMappings(spritzDirectory, reference);
            var u2eMappings = UCSC2EnsemblChromosomeMappings(spritzDirectory, reference);
            string outputPath = Path.Combine(Path.GetDirectoryName(inputPath), Path.GetFileNameWithoutExtension(inputPath)) + ".ucsc" + Path.GetExtension(inputPath);
            using (StreamReader reader = new StreamReader(inputPath))
            using (StreamWriter writer = new StreamWriter(outputPath))
            {
                while (true)
                {
                    string line = reader.ReadLine();
                    if (line == null || line == "") { break; }
                    if (line.StartsWith("#")) { continue; }
                    string[] columns = line.Split('\t');
                    if (columns.Length == 0) { break; }
                    if (u2eMappings.TryGetValue(columns[0], out string ensemblColumn)) { columns[0] = ensemblColumn; }
                    else if (e2uMappings.TryGetValue(columns[0], out string ucscColumn)) { } // nothing to do, already Ensembl
                    else { continue; } // did not recognize this chromosome name; filter it out
                    writer.WriteLine(string.Join("\t", columns));
                }
            }
            return outputPath;
        }

        /// <summary>
        /// Ensembl coding domain sequences (CDS) sometimes don't have start or stop codons annotated.
        /// The only way I can figure out how to tell which they are is to read in the protein FASTA and find the ones starting with X's or containing a stop codon '*'
        /// </summary>
        /// <param name="spritzDirectory"></param>
        /// <param name="proteinFastaPath"></param>
        /// <returns></returns>
        public void GetImportantProteinAccessions(string spritzDirectory, string proteinFastaPath)
        {
            Regex transcriptAccession = new Regex(@"(transcript:)([A-Za-z0-9_.]+)"); // need to include transcript accessions for when a GTF file is used and transcript IDs become the protein IDs
            List<Protein> proteins = ProteinDbLoader.LoadProteinFasta(proteinFastaPath, true, DecoyType.None, false,
                ProteinDbLoader.EnsemblAccessionRegex, ProteinDbLoader.EnsemblFullNameRegex, ProteinDbLoader.EnsemblFullNameRegex, ProteinDbLoader.EnsemblGeneNameRegex, null, out List<string> errors);
            ProteinAccessionSequence = proteins.Select(p => new KeyValuePair<string, string>(p.Accession, p.BaseSequence))
                .Concat(proteins.Select(p => new KeyValuePair<string, string>(transcriptAccession.Match(p.FullName).Groups[2].Value, p.BaseSequence)))
                .ToDictionary(kv => kv.Key, kv => kv.Value);
            HashSet<string> badOnes = new HashSet<string>(proteins.Where(p => p.BaseSequence.Contains('X') || p.BaseSequence.Contains('*')).SelectMany(p => new string[] { p.Accession, transcriptAccession.Match(p.FullName).Groups[2].Value }));
            BadProteinAccessions = badOnes;
            SelenocysteineProteinAccessions = proteins.Where(p => !badOnes.Contains(p.Accession) && p.BaseSequence.Contains('U')).ToDictionary(p => p.Accession, p => p.BaseSequence);
        }

        public static void FilterGeneModel(string analysisDirectory, string geneModelGtfOrGff, Genome genome, out string filteredGeneModel)
        {
            string grepQuery = "\"^" + string.Join(@"\|^", genome.Chromosomes.Select(c => c.FriendlyName).Concat(new[] { "#" }).ToList()) + "\"";
            filteredGeneModel = Path.Combine(Path.GetDirectoryName(geneModelGtfOrGff), Path.GetFileNameWithoutExtension(geneModelGtfOrGff)) + ".filtered" + Path.GetExtension(geneModelGtfOrGff);
            WrapperUtility.GenerateAndRunScript(WrapperUtility.GetAnalysisScriptPath(analysisDirectory, "FilterGeneModel.bash"), new List<string>
            {
                "grep " + grepQuery + " " + WrapperUtility.ConvertWindowsPath(geneModelGtfOrGff) + " > " + WrapperUtility.ConvertWindowsPath(filteredGeneModel)
            }).WaitForExit();
        }

        /// <summary>
        /// Prepares an Ensembl genome fasta for alignment and all following analysis. The main issue is that Ensembl orders chromosomes lexigraphically, not karyotypically, like some software like GATK expects.
        /// </summary>
        /// <param name="genomeFasta"></param>
        /// <param name="ensemblGenome"></param>
        /// <param name="reorderedFasta"></param>
        public void PrepareEnsemblGenomeFasta(string analysisDirectory, string genomeFasta)
        {
            if (Path.GetExtension(genomeFasta) == ".gz" || Path.GetExtension(genomeFasta) == ".tgz")
            {
                WrapperUtility.GenerateAndRunScript(WrapperUtility.GetAnalysisScriptPath(analysisDirectory, "Gzippy.bash"), new List<string> { $"gunzip {WrapperUtility.ConvertWindowsPath(genomeFasta)}" }).WaitForExit();
                genomeFasta = Path.ChangeExtension(genomeFasta, null);
            }

            // We need to use the same fasta file throughout and have all the VCF and GTF chromosome reference IDs be the same as these.
            // Right now this is based on ensembl references, so those are the chromosome IDs I will be using throughout
            // TODO: try this with UCSC references to judge whether there's a difference in quality / yield / FDR etc in subsequent proteomics analysis
            // This file needs to be in karyotypic order; this allows us not to have to reorder it for GATK analysis
            ReorderedFastaPath = Path.Combine(Path.GetDirectoryName(genomeFasta), Path.GetFileNameWithoutExtension(genomeFasta) + ".karyotypic.fa");
            EnsemblGenome = new Genome(genomeFasta);
            if (!EnsemblGenome.IsKaryotypic())
            {
                EnsemblGenome.Chromosomes = EnsemblGenome.KaryotypicOrder();
                if (!File.Exists(ReorderedFastaPath)) { Genome.WriteFasta(EnsemblGenome.Chromosomes.Select(x => x.Sequence), ReorderedFastaPath); }
            }
            else
            {
                ReorderedFastaPath = genomeFasta;
            }
        }
    }
}