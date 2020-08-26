/*
dotNetRDF is free and open source software licensed under the MIT License

-----------------------------------------------------------------------------

Copyright (c) 2009-2012 dotNetRDF Project (dotnetrdf-developer@lists.sf.net)

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is furnished
to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;
using VDS.RDF.Configuration;
using VDS.RDF.Nodes;
using VDS.RDF.Parsing;
using VDS.RDF.Query.Operators;
using VDS.RDF.Query.Operators.DateTime;

namespace VDS.RDF.Configuration
{

    public class AutoConfigTests
    {
        [Fact]
        public void ConfigurationStaticOptionUri1()
        {
            var optionUri = new Uri("dotnetrdf-configure:VDS.RDF.Options#UsePLinqEvaluation");

            Assert.Equal("dotnetrdf-configure", optionUri.Scheme);
            Assert.Single(optionUri.Segments);
            Assert.Equal("VDS.RDF.Options", optionUri.Segments[0]);
            Assert.Equal("VDS.RDF.Options", optionUri.AbsolutePath);
            Assert.Equal("UsePLinqEvaluation", optionUri.Fragment.Substring(1));
        }

        [Fact]
        public void ConfigurationStaticOptionUri2()
        {
            var optionUri = new Uri("dotnetrdf-configure:VDS.RDF.Options,SomeAssembly#UsePLinqEvaluation");

            Assert.Equal("dotnetrdf-configure", optionUri.Scheme);
            Assert.Single(optionUri.Segments);
            Assert.Equal("VDS.RDF.Options,SomeAssembly", optionUri.Segments[0]);
            Assert.Equal("VDS.RDF.Options,SomeAssembly", optionUri.AbsolutePath);
            Assert.Equal("UsePLinqEvaluation", optionUri.Fragment.Substring(1));
        }

        private void ApplyStaticOptionsConfigure(Uri option, String value)
        {
            var g = new Graph();
            INode configure = g.CreateUriNode(UriFactory.Create(ConfigurationLoader.PropertyConfigure));
            g.Assert(g.CreateUriNode(option), configure, g.CreateLiteralNode(value));
            this.ApplyStaticOptionsConfigure(g);
        }

        private void ApplyStaticOptionsConfigure(IGraph g, Uri option, INode value)
        {
            INode configure = g.CreateUriNode(UriFactory.Create(ConfigurationLoader.PropertyConfigure));
            g.Assert(g.CreateUriNode(option), configure, value);
            this.ApplyStaticOptionsConfigure(g);
        }

        private void ApplyStaticOptionsConfigure(IGraph g)
        {
            ConfigurationLoader.AutoConfigureStaticOptions(g);
        }

        [Fact]
        public void ConfigurationStaticOptionsNoFragment()
        {
            var optionUri = new Uri("dotnetrdf-configure:VDS.RDF.Graph");

            Assert.Throws<DotNetRdfConfigurationException>(() => this.ApplyStaticOptionsConfigure(optionUri, ""));
        }

        [Fact]
        public void ConfigurationStaticOptionsBadClass()
        {
            var optionUri = new Uri("dotnetrdf-configure:VDS.RDF.NoSuchClass#Property");

            Assert.Throws<DotNetRdfConfigurationException>(() => this.ApplyStaticOptionsConfigure(optionUri, ""));
        }

        [Fact]
        public void ConfigurationStaticOptionsBadProperty()
        {
            var optionUri = new Uri("dotnetrdf-configure:VDS.RDF.Graph#NoSuchProperty");

            Assert.Throws<DotNetRdfConfigurationException>(() => this.ApplyStaticOptionsConfigure(optionUri, ""));
        }

        [Fact]
        public void ConfigurationStaticOptionsNonStaticProperty()
        {
            var optionUri = new Uri("dotnetrdf-configure:VDS.RDF.Graph#BaseUri");

            Assert.Throws<DotNetRdfConfigurationException>(() => this.ApplyStaticOptionsConfigure(optionUri, "http://example.org"));
        }

        [Fact]
        public void ConfigurationStaticOptionsEnumProperty()
        {
            var current = EqualityHelper.LiteralEqualityMode;
            try
            {
                Assert.Equal(current, EqualityHelper.LiteralEqualityMode);

                var optionUri = new Uri("dotnetrdf-configure:VDS.RDF.EqualityHelper#LiteralEqualityMode");
                this.ApplyStaticOptionsConfigure(optionUri, "Loose");

                Assert.Equal(LiteralEqualityMode.Loose, EqualityHelper.LiteralEqualityMode);
            }
            finally
            {
                EqualityHelper.LiteralEqualityMode = current;
            }
        }

        [Fact]
        public void ConfigurationStaticOptionsInt32Property()
        {
            var current = UriLoader.Timeout;
            try
            {
                Assert.Equal(current, UriLoader.Timeout);

                var optionUri = new Uri("dotnetrdf-configure:VDS.RDF.Parsing.UriLoader#Timeout");
                var g = new Graph();
                this.ApplyStaticOptionsConfigure(g, optionUri, (99).ToLiteral(g));

                Assert.Equal(99, UriLoader.Timeout);
            }
            finally
            {
                UriLoader.Timeout = current;
            }
        }


        [Fact]
        public void ConfigurationAutoOperators1()
        {
            try
            {
                var data = @"@prefix dnr: <http://www.dotnetrdf.org/configuration#> .
_:a a dnr:SparqlOperator ;
dnr:type """ + typeof(MockSparqlOperator).AssemblyQualifiedName + @""" .";

                var g = new Graph();
                g.LoadFromString(data);

                ConfigurationLoader.AutoConfigureSparqlOperators(g);

                SparqlOperators.TryGetOperator(SparqlOperatorType.Add, false, out var op, null);

                Assert.Equal(typeof(MockSparqlOperator), op.GetType());
                SparqlOperators.RemoveOperator(op);
            }
            finally
            {
                SparqlOperators.Reset();
            }
        }

        [Fact]
        public void ConfigurationAutoOperators2()
        {
            try
            {
                var data = @"@prefix dnr: <http://www.dotnetrdf.org/configuration#> .
_:a a dnr:SparqlOperator ;
dnr:type ""VDS.RDF.Query.Operators.DateTime.DateTimeAddition"" ;
dnr:enabled false .";

                var g = new Graph();
                g.LoadFromString(data);

                ConfigurationLoader.AutoConfigureSparqlOperators(g);

                Assert.False(SparqlOperators.IsRegistered(new DateTimeAddition()));
            }
            finally
            {
                SparqlOperators.Reset();
            }
        }
    }

    public class MockSparqlOperator
        : ISparqlOperator
    {

        #region ISparqlOperator Members

        public SparqlOperatorType Operator => SparqlOperatorType.Add;

        public bool IsExtension => true;

        public bool IsApplicable(params IValuedNode[] ns)
        {
            return true;
        }

        public IValuedNode Apply(params Nodes.IValuedNode[] ns)
        {
            return null;
        }

        #endregion
    }
}
