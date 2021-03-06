﻿using CMD;
using Nett;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace SpritzGUI
{
    public class EverythingRunnerEngine
    {
        private readonly List<Tuple<string, Options>> taskList;
        private string outputFolder;

        public EverythingRunnerEngine(List<Tuple<string, Options>> taskList, string outputFolder)
        {
            this.taskList = taskList;
            this.outputFolder = outputFolder;
        }

        public string Arguments { get; set; }
        public string StdErr { get; set; }
        public static string SpritzDirectory { get; set; } = Environment.CurrentDirectory;

        public void Run()
        {
            //var startTimeForAllFilenames = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss", CultureInfo.InvariantCulture);

            //outputFolder = outputFolder.Replace("$DATETIME", startTimeForAllFilenames);

            for (int i = 0; i < taskList.Count; i++)
            {
                var ok = taskList[i];

                var tomlFileName = Path.Combine(outputFolder, i.ToString() + "_Parameters.toml");
                Toml.WriteFile(ok.Item2, tomlFileName);

                //Put it into a function
                var arguments = GenerateArguments(ok.Item2);
                Arguments = string.Join(" ", arguments);

                //Spritz.Main(commands); // this doesn't work in releases, unfortunately

                Process proc = new Process();
                proc.StartInfo.FileName = "CMD.exe";
                proc.StartInfo.Arguments = string.Join(" ", arguments);
                proc.StartInfo.CreateNoWindow = true;
                proc.StartInfo.UseShellExecute = false; // don't fire up a shell for the CMD.exe process
                proc.StartInfo.RedirectStandardError = true;
                proc.Start();
                proc.WaitForExit();
                StdErr = proc.StandardError.ReadToEnd();
            }
        }

        public IEnumerable<string> GenerateCommandsDry()
        {
            for (int i = 0; i < taskList.Count; i++)
            {
                var ok = taskList[i];
                var arguments = GenerateArguments(ok.Item2);
                yield return string.Join(" ", arguments);
            }
        }

        private string[] GenerateArguments(Options options)
        {
            List<string> commands = new List<string> { "-c", options.Command };
            if (options.SpritzDirectory != null && options.SpritzDirectory != "")
            {
                commands.AddRange(new[] { "-b", AddQuotes(options.SpritzDirectory) });
            }
            if (options.AnalysisDirectory != null && options.AnalysisDirectory != "")
            {
                commands.AddRange(new[] { "-a", AddQuotes(options.AnalysisDirectory)});
            }
            if (options.Fastq1 != null && options.Fastq1 != "")
            {
                commands.AddRange(new[] { "--fq1", AddQuotes(options.Fastq1) });
            }
            if (options.Fastq2 != null && options.Fastq2 != "")
            {
                commands.AddRange(new[] { "--fq2", AddQuotes(options.Fastq2) });
            }
            if (options.ExperimentType != null && options.ExperimentType != "")
            {
                commands.AddRange(new[] { "-e", options.ExperimentType });
            }
            if (options.SraAccession != null && options.SraAccession != "")
            {
                commands.AddRange(new[] { "-s", options.SraAccession });
            }
            if (options.Threads > 0 && options.Threads <= Environment.ProcessorCount)
            {
                commands.AddRange(new[] { "-t", options.Threads.ToString() });
            }
            if (options.GenomeStarIndexDirectory != null && options.GenomeStarIndexDirectory != "")
            {
                commands.AddRange(new[] { "-d", AddQuotes(options.GenomeStarIndexDirectory) });
            }
            if (options.GenomeFasta != null && options.GenomeFasta != "")
            {
                commands.AddRange(new[] { "-f", AddQuotes(options.GenomeFasta) });
            }
            if (options.GeneModelGtfOrGff != null && options.GeneModelGtfOrGff != "")
            {
                commands.AddRange(new[] { "-g", AddQuotes(options.GeneModelGtfOrGff) });
            }
            if (options.NewGeneModelGtfOrGff != null && options.NewGeneModelGtfOrGff != "")
            {
                commands.AddRange(new[] { "-h", AddQuotes(options.NewGeneModelGtfOrGff) });
            }
            if (options.ReferenceVcf != null && options.ReferenceVcf != "")
            {
                commands.AddRange(new[] { "-v", AddQuotes(options.ReferenceVcf) });
            }
            if (options.Reference != null && options.Reference != "")
            {
                commands.AddRange(new[] { "-r", options.Reference });
            }
            if (options.UniProtXml != null && options.UniProtXml != "")
            {
                commands.AddRange(new[] { "-x", AddQuotes(options.UniProtXml) });
            }
            if (options.UniProtXml != null && options.UniProtXml != "")
            {
                commands.AddRange(new[] { "--indelFinder", options.IndelFinder });
            }
            if (options.OverwriteStarAlignments)
            {
                commands.Add("--overwriteStarAlignments");
            }
            if (options.StrandSpecific)
            {
                commands.Add("--strandSpecific");
            }
            if (options.InferStrandSpecificity)
            {
                commands.Add("--inferStrandedness");
            }
            if (options.DoTranscriptIsoformAnalysis)
            {
                commands.Add("--doTranscriptIsoformAnalysis");
            }
            if (options.DoFusionAnalysis)
            {
                commands.Add("--doGeneFusionAnalysis");
            }
            if (options.SkipVariantAnalysis)
            {
                commands.Add("--skipVariantAnalysis");
            }
            if (options.VariantCallingWorkers > 0 && options.VariantCallingWorkers <= Environment.ProcessorCount)
            {
                commands.AddRange(new[] { "--variantCallingWorkers", options.VariantCallingWorkers.ToString() });
            }
            return commands.ToArray();
        }

        /// <summary>
        /// Needed for paths that have spaces in them to pass through properly
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private static string AddQuotes(string path)
        {
            if (path.StartsWith("\"") && path.EndsWith("\""))
                return path;
            else
                return $"\"{path}\"";
        }
    }
}