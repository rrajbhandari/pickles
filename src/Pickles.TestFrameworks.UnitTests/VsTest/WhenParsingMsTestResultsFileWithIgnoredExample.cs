﻿//  --------------------------------------------------------------------------------------------------------------------
//  <copyright file="WhenParsingMsTestResultsFileWithEmptyExampleValues.cs" company="PicklesDoc">
//  Copyright 2011 Jeffrey Cameron
//  Copyright 2012-present PicklesDoc team and community contributors
//
//
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.
//  </copyright>
//  --------------------------------------------------------------------------------------------------------------------


using NUnit.Framework;
using PicklesDoc.Pickles.ObjectModel;
using System.Collections.Generic;
using NFluent;

using PicklesDoc.Pickles.TestFrameworks.VsTest;

namespace PicklesDoc.Pickles.TestFrameworks.UnitTests.VsTest
{
    [TestFixture]
    public class WhenParsingVsTestResultsFileWithIgnoredExample : StandardTestSuite<VsTestResults>
    {
        public WhenParsingVsTestResultsFileWithIgnoredExample()
            : base("VsTest." + "results-example-vstest-ignoredexample.trx")
        {
        }

        [Test]
        public void ThenIgnoredScenarioIsSetToInconclusive()
        {
            var results = ParseResultsFile();

            var feature = new Feature { Name = "Example With Ignored Scenario Outline" };
            var scenario = new Scenario { Name = "Add two numbers", Feature = feature };
            scenario.Steps = new List<Step>();

            var examples = new ExampleTable();
            examples.HeaderRow = new TableRow();
            examples.HeaderRow.Cells.Add("TestCase");
            var row = new TableRowWithTestResult();
            row.Cells.Add("1");
            examples.DataRows = new List<TableRow>();
            examples.DataRows.Add(row);

            scenario.Examples = new List<Example>();
            scenario.Examples.Add(new Example() { TableArgument = examples });

            var matchedExampleResult = results.GetExampleResult(scenario, new string[] { "1" });
            Check.That(matchedExampleResult).IsEqualTo(TestResult.Inconclusive);
        }
    }
}
