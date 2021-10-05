//  --------------------------------------------------------------------------------------------------------------------
//  <copyright file="HtmlScenarioOutlineFormatterTests.cs" company="PicklesDoc">
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

using System;
using System.Collections.Generic;
using System.Xml.Linq;

using Autofac;
using NFluent;
using NSubstitute;
using NUnit.Framework;

using PicklesDoc.Pickles.DocumentationBuilders.Html;
using PicklesDoc.Pickles.ObjectModel;

namespace PicklesDoc.Pickles.Test.Formatters
{
    [TestFixture]
    public class HtmlScenarioFormatterTests : BaseFixture
    {
        #region Setup/Teardown

        [SetUp]
        public void Setup()
        {
            var fakeTestResults = Substitute.For<ITestResults>();

            this.formatter = new HtmlScenarioFormatter(
                Container.Resolve<HtmlStepFormatter>(),
                Container.Resolve<HtmlDescriptionFormatter>(),
                Container.Resolve<HtmlTableFormatter>(),
                Container.Resolve<HtmlImageResultFormatter>(),
                fakeTestResults,
                Container.Resolve<ILanguageServicesRegistry>());
        }

        #endregion

        private HtmlScenarioFormatter formatter;

        private static Scenario BuildMinimalScenario()
        {
            var examples = new List<Example>();
            examples.Add(new Example
            {
                Description = "My Example Description",
                TableArgument = new ExampleTable
                {
                    HeaderRow = new TableRow("Cell1"),
                    DataRows =
                        new List<TableRow>(
                            new[]
                            {
                                new TableRowWithTestResult("Value1")
                            })
                },
            });
            var scenario = new Scenario
            {
                Description = "My Outline Description",
                Examples = examples,
                Steps = new List<Step>
                {
                    new Step
                    {
                        NativeKeyword = "Given",
                        Name = "My Step Name",
                        TableArgument = new Table
                        {
                            HeaderRow =
                                new TableRow("Cell1"),
                            DataRows =
                                new List<TableRow>(
                                    new[]
                                    {
                                        new TableRow("Value1")
                                    })
                        },
                    }
                }
            };
            return scenario;
        }

        [Test]
        public void Li_Element_Must_Not_Have_Id_Attribute()
        {
            Scenario scenario = BuildMinimalScenario();

            XElement li = this.formatter.Format(scenario, 1);

            XAttribute idAttribute = li.Attribute("id");

            Check.That(idAttribute).IsNull();
        }
    }
}
