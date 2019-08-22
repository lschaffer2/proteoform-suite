﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace ProteoformSuiteInternal
{
    public class TopDownProteoform : ExperimentalProteoform
    {
        public string uniprot_id { get; set; }
        public string pfr_accession { get; set; }
        public string name { get; set; }
        public string sequence { get; set; }
        public int topdown_begin { get; set; } //position one based
        public int topdown_end { get; set; } //position one based
        public double theoretical_mass { get; set; }
        public List<SpectrumMatch> topdown_hits;
        private PtmSet _topdown_ptm_set = new PtmSet(new List<Ptm>());

        public PtmSet topdown_ptm_set //the ptmset read in with td data
        {
            get
            {
                return _topdown_ptm_set;
            }

            set
            {
                _topdown_ptm_set = value;
                topdown_ptm_description = _topdown_ptm_set == null || _topdown_ptm_set.ptm_combination == null ?
                    "Unknown" :
                    _topdown_ptm_set.ptm_combination.Count == 0 ?
                        "Unmodified" :
                    string.Join("; ", _topdown_ptm_set.ptm_combination.Select(ptm => ptm.position > 0 ? ptm.modification.OriginalId + "@" + ptm.position : UnlocalizedModification.LookUpId(ptm.modification)).ToList());
            }
        }
        public GeneName topdown_geneName { get; set; }
        public string topdown_ptm_description { get; set; }
        public ExperimentalProteoform matching_experimental { get; set; } //corresponding experimental
        public bool correct_id { get; set; } //true if the ID given by ProteoformSuite matches ID from topdown
        public List<SpectrumMatch> ambiguous_topdown_hits = new List<SpectrumMatch>();
        public int topdown_level;

        public TopDownProteoform(string accession, List<SpectrumMatch> hits) : base(accession, null, true)
        {
            SpectrumMatch root = hits[0];
            this.name = root.name;
            this.pfr_accession = root.pfr_accession;
            this.topdown_ptm_set = new PtmSet(root.ptm_list);
            this.uniprot_id = root.uniprot_id;
            this.sequence = root.sequence;
            this.topdown_begin = root.begin;
            this.theoretical_mass = root.theoretical_mass;
            this.topdown_end = root.end;
            this.topdown_hits = hits;
            this.calculate_td_properties();
            this.accession = accession + "_" + topdown_begin + "to" + topdown_end + "_1TD";
            this.lysine_count = sequence.Count(s => s == 'K');
            this.topdown_id = true;
        }

        public TopDownProteoform(TopDownProteoform t) : base(t.accession, null, true)
        {
            this.root = t.root;
            this.name = t.name;
            this.ptm_set = new PtmSet(t.ptm_set.ptm_combination);
            this.topdown_ptm_set = new PtmSet(t.topdown_ptm_set.ptm_combination);
            this.uniprot_id = t.uniprot_id;
            this.sequence = t.sequence;
            this.begin = t.begin;
            this.theoretical_mass = t.theoretical_mass;
            this.end = t.end;
            this.topdown_hits = t.topdown_hits.ToList();
            this.modified_mass = t.modified_mass;
            this.theoretical_mass = t.theoretical_mass;
            this.matching_experimental = t.matching_experimental;
            this.accession = t.accession;
            this.agg_rt = t.agg_rt;
            this.lysine_count = t.lysine_count;
            this.accepted = t.accepted;
            this.uniprot_id = t.uniprot_id;
            this.pfr_accession = t.pfr_accession;
            this.agg_intensity = t.agg_intensity;
            this.topdown_id = t.topdown_id;
            this.agg_mass = t.agg_mass;
            this.is_target = t.is_target;
            this.topdown_end = t.topdown_end;
            this.topdown_begin = t.topdown_begin;
            this.topdown_geneName = t.topdown_geneName;
        }

        public void calculate_td_properties()
        {
            //correct here for missed monoisotopic mass...
            this.agg_mass = topdown_hits.Select(h => (h.reported_mass- Math.Round(h.reported_mass - h.theoretical_mass, 0) * Lollipop.MONOISOTOPIC_UNIT_MASS)).Average();
            this.modified_mass = this.agg_mass;
            this.agg_rt = topdown_hits.Select(h => h.ms2_retention_time).Average();
            foreach (var ambiguous_id in topdown_hits.SelectMany(h => h.ambiguous_matches))
            {
                if (this.pfr_accession == ambiguous_id.pfr_accession || ambiguous_topdown_hits.Select(h => h.pfr_accession).Contains(ambiguous_id.pfr_accession))
                {
                    continue;
                }
                ambiguous_topdown_hits.Add(ambiguous_id);
            }
            calculate_topdown_level();
        }

        private void calculate_topdown_level()
        {
            if (ambiguous_topdown_hits.Count == 0)
            {
                topdown_level = 1;
            }
            else
            {
                var unique_accessions = new List<string>() { this.accession.Split('_')[0].Split('-')[0] }.Concat(ambiguous_topdown_hits.Select(a => a.accession.Split('_')[0].Split('-')[0])).Distinct();
                var unique_sequences = new List<string>() { sequence }.Concat(ambiguous_topdown_hits.Select(a => sequence)).Distinct();
                var unique_PTM_locations = new List<string>() { string.Join(",", topdown_ptm_set.ptm_combination.Select(p => p.position).OrderBy(n => n)) }.Concat(ambiguous_topdown_hits.Select(h => string.Join(",", h.ptm_list.Select(p => p.position).OrderBy(n => n)))).Distinct();
                var unique_PTM_IDs = new List<string>() { string.Join(",", topdown_ptm_set.ptm_combination.Select(p => UnlocalizedModification.LookUpId(p.modification)).OrderBy(n => n)) }.Concat(ambiguous_topdown_hits.Select(h => string.Join(",", h.ptm_list.Select(p => UnlocalizedModification.LookUpId(p.modification)).OrderBy(n => n)))).Distinct();

                int gene_ambiguity = unique_accessions.Count() > 1 ? 1 : 0;
                int sequence_ambiguity = unique_sequences.Count() > 1 ? 1 : 0;
                int PTM_ambiguity = unique_PTM_IDs.Count() > 1 ? 1 : 0;
                int PTM_location = unique_PTM_locations.Count() > 1 ? 1 : 0;

                topdown_level = 1 + gene_ambiguity + sequence_ambiguity + PTM_ambiguity + PTM_location;
            }
        }

        public void set_correct_id()
        {
            if (linked_proteoform_references == null) correct_id = false;
            else
            {
                TheoreticalProteoform t = linked_proteoform_references.First() as TheoreticalProteoform;
                bool matching_accession = t.ExpandedProteinList.SelectMany(p => p.AccessionList).Select(a => a.Split('_')[0]).Contains(accession.Split('_')[0].Split('-')[0]);
                bool same_begin_and_end = begin == topdown_begin && end == topdown_end;
                bool same_ptm_set = topdown_ptm_set.same_ptmset(ptm_set, true);
                correct_id = matching_accession && same_ptm_set && same_begin_and_end;
            }
        }
    }
}