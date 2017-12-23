﻿using System.Collections.Generic;
using System.Linq;

namespace Proteogenomics
{
    /// <summary>
    /// Specifications are described here: http://snpeff.sourceforge.net/VCFannotationformat_v1.0.pdf
    /// </summary>
    public class SnpEffAnnotation
    {

        #region Annotation Properties

        public string Allele { get; set; }
        public string[] Effects { get; set; }
        public string PutativeImpact { get; set; }
        public string GeneName { get; set; }
        public string GeneID { get; set; }

        /// <summary>
        /// It looks like these are sometimes domains, like the ones annotated in UniProt,
        /// Otherwise, this tends to just be "transcript"
        /// 
        /// Some examples:
        /// sequence_feature: can be initiator-methionine:Removed ... maybe not too helpful for proteomics, since this is assumed
        /// sequence_feature: helix:combinatorial_evidence_used_in_manual_assertion
        /// sequence_feature: nucleotide-phosphate-binding-region:ATP
        /// sequence_feature: domain:EGF-like_2
        /// sequence_feature: transmembrane-region:Transmembrane_region
        /// sequence_feature: topological-domain:Extracellular
        /// sequence_feature: modified-residue:phosphoserine
        /// </summary>
        public string FeatureType { get; set; }

        /// <summary>
        /// Always seems to be the transcriptID
        /// </summary>
        public string FeatureID { get; set; }
        public string TranscriptBiotype { get; set; }
        public int ExonIntronRank { get; set; }
        public int ExonIntronTotal { get; set; }
        public string HGVSNotationDnaLevel { get; set; } // kind of bad for ins and del because they notation aligns to most 3' coordinate, rather than leftmost
        public string HGVSNotationProteinLevel { get; set; }
        public int OneBasedTranscriptCDNAPosition { get; set; }
        public int TranscriptCDNALength { get; set; }
        public int OneBasedCodingDomainSequencePosition { get; set; }
        public int CodingDomainSequenceLengthIncludingStopCodon { get; set; }
        public int OneBasedProteinPosition { get; set; }
        public int ProteinLength { get; set; }

        /// <summary>
        /// up/downstream: distance to first / last codon
        /// intergenic: distance to closest gene
        /// exonic: distance to closest intron boundary (+ is upstream, - is downstream)
        /// intronic: distance to closest exon boundary (+ is upstream, - is downstream)
        /// motif: distance to first base in MOTIF
        /// miRNA: distance to first base in miRNA
        /// splice_site: distance to exon-intron boundary
        /// splice_region: distance to exon-intron boundary
        /// chip seq peak: distance to summit or peak center
        /// histone mark/state: distance to summit or peak center
        /// </summary>
        public int DistanceToFeature { get; set; }
        public string[] Warnings { get; set; }

        #endregion Annotation Properties

        #region Interpreted Properties

        public bool Synonymous { get; set; }
        public bool FrameshiftVariant { get; set; }
        public bool BadTranscript { get; set; }

        #endregion Interpreted Properties

        #region Constructor

        public SnpEffAnnotation(string annotation)
        {
            string[] a = annotation.Split('|');
            Allele = a[0];
            Effects = a[1].Split('&');
            PutativeImpact = a[2];
            GeneName = a[3];
            GeneID = a[4];
            FeatureType = a[5];
            FeatureID = a[6];
            TranscriptBiotype = a[7];
            if (a[8].Split('/').Length > 0 && int.TryParse(a[8].Split('/')[0], out int x)) ExonIntronRank = x;
            if (a[8].Split('/').Length > 1 && int.TryParse(a[8].Split('/')[1], out int y)) ExonIntronTotal = y;
            HGVSNotationDnaLevel = a[9];
            if (a[10].Split('/').Length > 0 && int.TryParse(a[10].Split('/')[0], out x)) OneBasedTranscriptCDNAPosition = x;
            if (a[10].Split('/').Length > 1 && int.TryParse(a[10].Split('/')[1], out y)) TranscriptCDNALength = y;
            if (a[11].Split('/').Length > 0 && int.TryParse(a[11].Split('/')[0], out x)) OneBasedCodingDomainSequencePosition = x;
            if (a[11].Split('/').Length > 1 && int.TryParse(a[11].Split('/')[1], out y)) CodingDomainSequenceLengthIncludingStopCodon = y;
            if (a[12].Split('/').Length > 0 && int.TryParse(a[12].Split('/')[0], out x)) OneBasedProteinPosition = x;
            if (a[12].Split('/').Length > 1 && int.TryParse(a[12].Split('/')[1], out y)) ProteinLength = y;
            if (int.TryParse(a[13], out y)) DistanceToFeature = y;
            Warnings = a[14].Split('&');

            Synonymous = Effects.Any(eff => NonSynonymousVariations.Contains(eff));
            FrameshiftVariant = Effects.Contains("frameshift_variant");
            BadTranscript = Warnings.Any(w => BadTranscriptWarnings.Contains(w));
        }

        #endregion Constructor

        #region Private Fields

        private string[] HighPutativeImpactEffects = new string[]
        {
            "chromosome_number_variation", // rare...
            "exon_loss_variant", //
            "frameshift_variant",
            "rare_amino_acid_variant",
            "splice_acceptor_variant", // often with intron_variant, sometimes with splice_donor_variant
            "splice_donor_variant", // often with intron_variant, sometimes with splice_acceptor_variant
            "start_lost",
            "stop_gained",
            "stop_lost",
            "transcript_ablation",
        };

        private string[] ModeratePutativeImpactEffects = new string[]
        {
            "3_prime_UTR_truncation", "exon_loss", // appear together
            "5_prime_UTR_truncation", "exon_loss_variant", // appear together
            "coding_sequence_variant", // not seen much? Probably because missense is used more often.
            "conservative_inframe_insertion",
            "conservative_inframe_deletion",
            "disruptive_inframe_deletion",
            "disruptive_inframe_insertion",
            "inframe_deletion", // not common, in favor of more specific terms above
            "inframe_insertion", // not common, in favor of more specific terms above
            "missense_variant",
            "regulatory_region_ablation", // not common?
            "splice_region_variant", // often combined with intron_variant and non_coding_transcript_exon_variant
            "TFBS_ablation", // not common?
        };

        private string[] NonSynonymousVariations = new string[]
        {
            "exon_loss_variant", //
            "frameshift_variant",
            "rare_amino_acid_variant",
            "start_lost",
            "stop_gained",
            "stop_lost",
            "conservative_inframe_insertion",
            "conservative_inframe_deletion",
            "disruptive_inframe_deletion",
            "disruptive_inframe_insertion",
            "inframe_deletion", // not common, in favor of more specific terms above
            "inframe_insertion", // not common, in favor of more specific terms above
            "missense_variant",
        };

        private string[] LowPutativeImpactEffects = new string[]
        {
            "5_prime_UTR_premature_start_codon_gain_variant",
            "initiator_codon_variant",
            "splice_region_variant",
            "start_retained", // not used in human, with only one canonical start codon
            "stop_retained_variant", // fairly common
            "synonymous_variant",
            "sequence_feature"
        };

        private string[] ModifierEffects = new string[]
        {
            "3_prime_UTR_variant",
            "5_prime_UTR_variant",
            "coding_sequence_variant",
            "conserved_intergenic_variant",
            "conserved_intron_variant",
            "downstream_gene_variant",
            "exon_variant",
            "feature_elongation",
            "feature_truncation",
            "gene_variant",
            "intergenic_region",
            "intragenic_variant",
            "intron_variant",
            "mature_miRNA_variant",
            "miRNA",
            "NMD_transcript_variant",
            "non_coding_transcript_exon_variant",
            "non_coding_transcript_variant",
            "regulatory_region_amplification",
            "regulatory_region_variant",
            "TF_binding_site_variant",
            "TFBS_amplification",
            "transcript_amplification",
            "transcript_variant",
            "upstream_gene_variant"
        };

        private string[] BadTranscriptWarnings = new string[]
        {
            "WARNING_TRANSCRIPT_INCOMPLETE",
            "WARNING_TRANSCRIPT_MULTIPLE_STOP_CODONS",
            "WARNING_TRANSCRIPT_NO_STOP_CODON",
            "WARNING_TRANSCRIPT_NO_START_CODON"
        };

        #endregion Private Fields

        #region Public Fields

        /// <summary>
        /// It looks like WARNING_TRANSCRIPT_INCOMPLETE, WARNING_TRANSCRIPT_MULTIPLE_STOP_CODONS, 
        /// WARNING_TRANSCRIPT_NO_STOP_CODON, and WARNING_TRANSCRIPT_NO_START_CODON are relevant to this program.
        /// 
        /// These are the ones that I shouldn't be translating.
        /// 
        /// Could also be used for error messages regarding certain transcripts.
        /// </summary>
        public Dictionary<string, string> SnpEffWarningDescriptions = new Dictionary<string, string>
        {
            { "ERROR_CHROMOSOME_NOT_FOUND", "Chromosome does not exists in reference genome database." },
            { "ERROR_OUT_OF_CHROMOSOME_RANGE", "The variant’s genomic coordinate is greater than chromosome's length." },
            { "WARNING_REF_DOES_NOT_MATCH_GENOME", "This means that the ‘REF’ field in the input VCF file does not match the reference genome." },
            { "WARNING_SEQUENCE_NOT_AVAILABLE", "Reference sequence is not available, thus no inference could be performed." },
            { "WARNING_TRANSCRIPT_INCOMPLETE", "A protein coding transcript having a non­multiple of 3 length, indicating that the reference genome has missing information about this trancript." },
            { "WARNING_TRANSCRIPT_MULTIPLE_STOP_CODONS", "A protein coding transcript has two or more STOP codons in the middle of the coding sequence (CDS). This should not happen and it usually means the reference genome may have an error in this transcript." },
            { "WARNING_TRANSCRIPT_NO_START_CODON", "A protein coding transcript does not have a proper START codon. It is rare that a real transcript does not have a START codon, so this probably indicates an error or missing information in the reference genome." },
            { "WARNING_TRANSCRIPT_NO_STOP_CODON", "A protein coding transcript does not have a proper STOP codon. It is rare that a real transcript does not have a STOP codon, so this probably indicates an error or missing information in the reference genome." },
            { "INFO_REALIGN_3_PRIME", "Variant has been realigned to the most 3­-prime position within the transcript. This is usually done to to comply with HGVS specification to always report the most 3-­prime annotation." },
            { "INFO_COMPOUND_ANNOTATION", "This effect is a result of combining more than one variants." },
            { "INFO_NON_REFERENCE_ANNOTATION", "An alternative reference sequence was used to calculate this annotation." },
        };

        #endregion Public Fields
    }
}
