﻿//  --------------------------------------------------------------------------------------------------------------------
//  <copyright file="MsTestScenarioOutlineExampleMatcher.cs" company="PicklesDoc">
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

using System.Linq;
using System.Xml.Linq;

using PicklesDoc.Pickles.ObjectModel;

namespace PicklesDoc.Pickles.TestFrameworks.MsTest
{
    public class MsTestScenarioExampleMatcher : IScenarioExampleMatcher
    {
        public bool IsMatch(Scenario scenario, string[] exampleValues, object scenarioElement)
        {
            var element = (XElement)scenarioElement;

            var valuesInScenario = element.DetermineValuesInScenario();

            var isMatch = exampleValues.OrderBy(e => e).SequenceEqual(valuesInScenario.OrderBy(v => v));
            return isMatch;
        }
    }
}