/*
 * Author: James Boyd, Description: A library to simplify concatenation of multiple PDFs into a single document and/or create a PDF document from an image.
 * Copyright (C) 2016 Brock & Scott, PLLC
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Affero General Public License as
 * published by the Free Software Foundation, either version 3 of the
 * License, or (at your option) any later version.

 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using System.IO;
using System.Linq;
using BAS.Utilities;
using NUnit.Framework;

namespace BAS.Tests
{
    [TestFixture]
    public class PDFParserTestfixture
    {
        private const string REGEX_TO_TEST = @"(?<pre>(\w)+)\s(?<fox>quick brown fox)[\w\s]+";
        private byte[] pdfBytes;
        [SetUp]
        public void Setup()
        {
            pdfBytes = File.ReadAllBytes(@"C:\Temp\TEST_BROWN_FOX.pdf");
        }

        [TestCase(Description = "Initialization of PDFParser should not throw an exception")]
        public void InitializationTest()
        {
            Assert.DoesNotThrow(() =>
           {
               PDFParser parser = new PDFParser(pdfBytes);
           });
            Assert.DoesNotThrow(() =>
           {
               PDFParser parser = new PDFParser(pdfBytes, @".+quick.+");
           });
        }
        
        [TestCase(Description = "Regular expressions should be matched correctly")]
        public void RegexTest()
        {
            PDFParser parser = new PDFParser(pdfBytes, REGEX_TO_TEST);
            Assert.That(() =>
           {
               return parser["pre"].Contains("The") && parser["fox"].Contains("quick brown fox");
           });
        }


    }
}
