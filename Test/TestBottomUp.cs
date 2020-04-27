﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using ProteoformSuiteInternal;
using System.IO;

namespace Test
{
    [TestFixture]
    class TestBottomUp
    {
        [Test]
        public void test_bottomUp_shared_protein()
        {
            Sweet.lollipop = new Lollipop();
            Sweet.lollipop.neucode_labeled = false;
            Sweet.lollipop.carbamidomethylation = false;
            Sweet.lollipop.clear_td();
            Sweet.lollipop.enter_input_files(new string[] { Path.Combine(TestContext.CurrentContext.TestDirectory, "testBottomUp.psmtsv") }, Lollipop.acceptable_extensions[7], Lollipop.file_types[7], Sweet.lollipop.input_files, false);
            Sweet.lollipop.enter_input_files(new string[] { Path.Combine(TestContext.CurrentContext.TestDirectory, "uniprot_yeast_test_12entries.xml") }, Lollipop.acceptable_extensions[2], Lollipop.file_types[2], Sweet.lollipop.input_files, false);
            Sweet.lollipop.theoretical_database.get_theoretical_proteoforms(TestContext.CurrentContext.TestDirectory);
            var bottom_up_psms = Sweet.lollipop.bottomupReader.ReadTDFile(Sweet.lollipop.input_files.Where(b => b.purpose == Purpose.BottomUp).First());
            Assert.AreEqual(2, bottom_up_psms.Count(p => p.shared_protein));
        }

        [Test]
        public void test_get_bottom_up_evidence_for_all_PTMs()
        {
            Sweet.lollipop = new Lollipop();
            Sweet.lollipop.enter_input_files(new string[] { Path.Combine(TestContext.CurrentContext.TestDirectory, "uniprot_yeast_test_12entries.xml") }, Lollipop.acceptable_extensions[2], Lollipop.file_types[2], Sweet.lollipop.input_files, false);
            Sweet.lollipop.theoretical_database.get_theoretical_proteoforms(TestContext.CurrentContext.TestDirectory);
            var mod = Sweet.lollipop.theoretical_database.all_mods_with_mass.Where(m => m.OriginalId == "Acetylation").First();

            //not ptm specific
            var peptide = ConstructorsForTesting.SpectrumMatch("A", 1000, 10, 1, 10);
            peptide.ptm_list.Add(new Ptm(2, mod));
            bool BU_evidence_for_all_PTMs = Proteoform.get_bottom_up_evidence_for_all_PTMs(new List<SpectrumMatch>() { peptide }, new PtmSet(new List<Ptm>() { new Ptm(3, mod) }), false);
            Assert.IsTrue(BU_evidence_for_all_PTMs);
            BU_evidence_for_all_PTMs = Proteoform.get_bottom_up_evidence_for_all_PTMs(new List<SpectrumMatch>() { peptide }, new PtmSet(new List<Ptm>() { new Ptm(3, mod)}), true);
            Assert.IsFalse(BU_evidence_for_all_PTMs);
            BU_evidence_for_all_PTMs = Proteoform.get_bottom_up_evidence_for_all_PTMs(new List<SpectrumMatch>() { peptide }, new PtmSet(new List<Ptm>() { new Ptm(2, mod) }), true);
            Assert.IsTrue(BU_evidence_for_all_PTMs);

        }

    }
}
