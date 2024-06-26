﻿//  --------------------------------------------------------------------------------------------------------------------
//  <copyright file="HtmlScenarioFormatter.cs" company="PicklesDoc">
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
using System.Linq;
using System.Xml.Linq;

using PicklesDoc.Pickles.ObjectModel;

namespace PicklesDoc.Pickles.DocumentationBuilders.Html
{
    public class HtmlScenarioFormatter
    {
        private readonly HtmlDescriptionFormatter htmlDescriptionFormatter;
        private readonly HtmlImageResultFormatter htmlImageResultFormatter;
        private readonly HtmlStepFormatter htmlStepFormatter;
        private readonly HtmlTableFormatter htmlTableFormatter;
        private readonly XNamespace xmlns;
        private readonly ITestResults testResults;
        private readonly ILanguageServicesRegistry languageServicesRegistry;

        public HtmlScenarioFormatter(
            HtmlStepFormatter htmlStepFormatter,
            HtmlDescriptionFormatter htmlDescriptionFormatter,
            HtmlTableFormatter htmlTableFormatter,
            HtmlImageResultFormatter htmlImageResultFormatter,
            ITestResults testResults,
            ILanguageServicesRegistry languageServicesRegistry)
        {
            this.htmlStepFormatter = htmlStepFormatter;
            this.htmlDescriptionFormatter = htmlDescriptionFormatter;
            this.htmlTableFormatter = htmlTableFormatter;
            this.htmlImageResultFormatter = htmlImageResultFormatter;
            this.testResults = testResults;
            this.languageServicesRegistry = languageServicesRegistry;
            this.xmlns = HtmlNamespace.Xhtml;
        }

        private XElement FormatHeading(Scenario scenario)
        {
            if (string.IsNullOrEmpty(scenario.Name))
            {
                return null;
            }

            var result = new XElement(
                this.xmlns + "div",
                new XAttribute("class", "scenario-heading"),
                string.IsNullOrEmpty(scenario.Slug) ? null : new XAttribute("id", scenario.Slug)
                );

            var tags = RetrieveTags(scenario);
            if (tags.Length > 0)
            {
                var paragraph = new XElement(this.xmlns + "p", HtmlScenarioFormatter.CreateTagElements(tags.OrderBy(t => t).ToArray(), this.xmlns));
                paragraph.Add(new XAttribute("class", "tags"));
                result.Add(paragraph);
            }

            result.Add(new XElement(this.xmlns + "h2", scenario.Name));

            result.Add(this.htmlDescriptionFormatter.Format(scenario.Description));

            return result;
        }

        private XElement FormatSteps(Scenario scenario)
        {
            if (scenario.Steps == null)
            {
                return null;
            }

            return new XElement(
                this.xmlns + "div",
                new XAttribute("class", "steps"),
                new XElement(
                    this.xmlns + "ul",
                    scenario.Steps.Select(
                        step => this.htmlStepFormatter.Format(step))));
        }

        private XElement FormatLinkButton(Scenario scenario)
        {
            if (string.IsNullOrEmpty(scenario.Slug))
            {
                return null;
            }

            return new XElement(
                this.xmlns + "a",
                new XAttribute("class", "scenario-link"),
                new XAttribute("href", $"javascript:showImageLink('{scenario.Slug}')"),
                new XAttribute("title", "Copy scenario link to clipboard."),
                new XElement(
                    this.xmlns + "i",
                    new XAttribute("class", "icon-link"),
                    " "));
        }

        private XElement FormatExamples(Scenario scenario)
        {
            var exampleDiv = new XElement(this.xmlns + "div");

            var languageServices = this.languageServicesRegistry.GetLanguageServicesForLanguage(scenario.Feature?.Language);

            foreach (var example in scenario.Examples)
            {
                exampleDiv.Add(
                    new XElement(
                        this.xmlns + "div",
                        new XAttribute("class", "examples"),
                        (example.Tags == null || example.Tags.Count == 0) ? null : new XElement(this.xmlns + "p", new XAttribute("class", "tags"), HtmlScenarioFormatter.CreateTagElements(example.Tags.OrderBy(t => t).ToArray(), this.xmlns)),
                        new XElement(this.xmlns + "h3", languageServices.ExamplesKeywords[0] + ": " + example.Name),
                        this.htmlDescriptionFormatter.Format(example.Description),
                        (example.TableArgument == null) ? null : this.htmlTableFormatter.Format(example.TableArgument, scenario)));
            }

            return exampleDiv;
        }

        public XElement Format(Scenario scenario, int id)
        {
            return new XElement(
                this.xmlns + "li",
                new XAttribute("class", "scenario"),
                this.htmlImageResultFormatter.Format(scenario),
                this.FormatHeading(scenario),
                this.FormatSteps(scenario),
                this.FormatLinkButton(scenario),
                (scenario.Examples == null || !scenario.Examples.Any())
                    ? null
                    : this.FormatExamples(scenario));
        }

        internal static XNode[] CreateTagElements(string[] tags, XNamespace xNamespace)
        {
            List<XNode> result = new List<XNode>();

            result.Add(new XText("Tags: "));
            result.Add(new XElement(xNamespace + "span", tags.First()));

            foreach (var tag in tags.Skip(1))
            {
                result.Add(new XText(", "));
                result.Add(new XElement(xNamespace + "span", tag));
            }

            return result.ToArray();
        }

        private static string[] RetrieveTags(Scenario scenario)
        {
            if (scenario == null)
            {
                return new string[0];
            }

            if (scenario.Feature == null)
            {
                return scenario.Tags.ToArray();
            }

            return scenario.Feature.Tags.Concat(scenario.Tags).ToArray();
        }
    }
}
