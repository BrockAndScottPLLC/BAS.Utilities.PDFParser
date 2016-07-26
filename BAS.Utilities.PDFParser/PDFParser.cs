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

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;

namespace BAS.Utilities
{
    public class PDFParser : IDictionary<string, string[]>
    {
        #region Protected Variables
        protected readonly byte[] _pdfBytes;
        protected Regex _regex;
        protected Task _getPageText;
        protected Task _getRegexMatches;
        protected string _pageText;
        protected Dictionary<string, string[]> _results = null;
        protected bool _includesRegex;
        #endregion
        #region Constants
        protected const string FILE_ACCESS_ERROR = "Unable to access file: {0}";
        protected const string REGEX_COMPILIATION_ERROR = "Invalid regular expression provided: {0}";
        protected const string PDF_DOCUMENT_ERROR = "Provided byte[] is not a valid PDF document";
        protected const string READONLY_ERROR = "Collection is read-only";
        protected const string REGEX_NOT_DONE = "No regular expression was provided on initialization";
        protected const string LICENSE = @"BAS.Utilities.PDFConcatenator: Copyright (C) 2016 Brock & Scott, PLLC
This program is free software: you can redistribute it and/or modify
it under the terms of the GNU Affero General Public License as
published by the Free Software Foundation, either version 3 of the
License, or(at your option) any later version.
Please see https://www.gnu.org/licenses/agpl-3.0.txt or LICENSE.txt for the full license.";
        #endregion
        #region Public Properties
        public string FullText
        {
            get
            {
                _getPageText.Wait();
                return _pageText;
            }
        }

        public bool IncludesRegex
        {
            get
            {
                return _includesRegex;
            }
        }
        #endregion
        #region Constructor
        public PDFParser(string path, string regex = null)
        {
            Console.WriteLine(LICENSE);
            if (!File.Exists(path) || !TryAccessFile(path))
                throw new IOException(string.Format(FILE_ACCESS_ERROR, path));
            _pdfBytes = File.ReadAllBytes(path);
            ValidatePdfDocument();
            InitRegex(regex);
            SetupPageText();
            SetupTextParse();
        }

        public PDFParser(byte[] pdfBytes, string regex = null)
        {
            Console.WriteLine(LICENSE);
            if (!VerifyDocumentIsPdf(pdfBytes))
                throw new ArgumentException(PDF_DOCUMENT_ERROR);
            _pdfBytes = pdfBytes;
            InitRegex(regex);
            SetupPageText();
            SetupTextParse();
        }
        #endregion
        #region IDictionary Implementation
        public string[] this[string key]
        {
            get
            {
                if (!_includesRegex)
                    throw new InvalidOperationException(REGEX_NOT_DONE);
                _getRegexMatches.Wait();
                return _results[key];
            }
            set
            {
                throw new InvalidOperationException(READONLY_ERROR);
            }
        }

        public int Count
        {
            get
            {
                if (_includesRegex)
                    throw new InvalidOperationException(REGEX_NOT_DONE);
                _getRegexMatches.Wait();
                return _results.Count;
            }
        }

        public bool IsReadOnly
        {
            get
            {
                return true;
            }
        }

        public ICollection<string> Keys
        {
            get
            {
                if (_includesRegex)
                    throw new InvalidOperationException(REGEX_NOT_DONE);
                _getRegexMatches.Wait();
                return _results.Keys;
            }
        }

        public ICollection<string[]> Values
        {
            get
            {
                if (_includesRegex)
                    throw new InvalidOperationException(REGEX_NOT_DONE);
                _getRegexMatches.Wait();
                return _results.Values;
            }
        }

        public void Add(KeyValuePair<string, string[]> item)
        {
            throw new InvalidOperationException(READONLY_ERROR);
        }

        public void Add(string key, string[] value)
        {
            throw new InvalidOperationException(READONLY_ERROR);
        }

        public void Clear()
        {
            throw new InvalidOperationException(READONLY_ERROR);
        }

        public bool Contains(KeyValuePair<string, string[]> item)
        {
            if (_includesRegex)
                throw new InvalidOperationException(REGEX_NOT_DONE);
            _getRegexMatches.Wait();
            return _results.ContainsKey(item.Key) && _results[item.Key] == item.Value;
        }

        public bool ContainsKey(string key)
        {
            if (_includesRegex)
                throw new InvalidOperationException(REGEX_NOT_DONE);
            _getRegexMatches.Wait();
            return _results.ContainsKey(key);
        }

        public void CopyTo(KeyValuePair<string, string[]>[] array, int arrayIndex)
        {
            if (_includesRegex)
                throw new InvalidOperationException(REGEX_NOT_DONE);
            _getRegexMatches.Wait();
            var tmpArray = _results.ToArray();
            for (int i = arrayIndex, j = 0; j < tmpArray.Length && arrayIndex < array.Length; i++, j++)
                array[i] = tmpArray[j];
        }

        public IEnumerator<KeyValuePair<string, string[]>> GetEnumerator()
        {
            if (_includesRegex)
                throw new InvalidOperationException(REGEX_NOT_DONE);
            _getRegexMatches.Wait();
            return _results.GetEnumerator();
        }

        public bool Remove(KeyValuePair<string, string[]> item)
        {
            throw new InvalidOperationException(READONLY_ERROR);
        }

        public bool Remove(string key)
        {
            throw new InvalidOperationException(READONLY_ERROR);
        }

        public bool TryGetValue(string key, out string[] value)
        {
            if (_includesRegex)
                throw new InvalidOperationException(REGEX_NOT_DONE);
            _getRegexMatches.Wait();
            return _results.TryGetValue(key, out value);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            if (_includesRegex)
                throw new InvalidOperationException(REGEX_NOT_DONE);
            _getRegexMatches.Wait();
            return _results.GetEnumerator();
        }
        #endregion
        #region Protected Methods
        protected bool TryAccessFile(string path)
        {
            try
            {
                using (var fs = File.OpenRead(path))
                {
                    fs.ReadByte();
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        protected bool VerifyRegularExpression(string regex)
        {
            if (string.IsNullOrEmpty(regex))
                return false;
            else
            {
                try
                {
                    Regex.Match("", regex);
                }
                catch (ArgumentException)
                {
                    return false;
                }
                return true;
            }
        }

        protected bool VerifyDocumentIsPdf(byte[] pdfBytes)
        {
            try
            {
                using (var reader = new PdfReader(pdfBytes))
                {
                    var tmp = PdfTextExtractor.GetTextFromPage(reader, 1);
                }
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }

        protected string GetPdfPageText(byte[] pdfBytes)
        {
            StringBuilder pageText = new StringBuilder();
            if (pdfBytes == null)
                throw new ArgumentException(PDF_DOCUMENT_ERROR);
            using(var reader = new PdfReader(_pdfBytes))
            {
                for (int i = 1; i <= reader.NumberOfPages; i++)
                    pageText.Append(PdfTextExtractor.GetTextFromPage(reader, i));
            }
            return pageText.ToString();
        }

        protected void SetupPageText()
        {
            _getPageText = Task.Factory.StartNew(() =>
            {
                _pageText = GetPdfPageText(_pdfBytes);
            });
        }

        protected void SetupTextParse()
        {
            _getPageText.Wait();
            if (!_includesRegex)
                return;
            _getRegexMatches = Task.Factory.StartNew(() =>
            {
                if (!_regex.Match(_pageText).Success)
                    return;
                {
                    foreach(var i in _regex.GetGroupNames())
                    {
                        List<string> captures = new List<string>();
                        foreach (Match x in _regex.Matches(_pageText))
                            foreach (Capture c in x.Groups[i].Captures)
                                captures.Add(c.Value);
                        _results.Add(i, captures.ToArray());
                    }
                }
            });
        }

        protected void InitRegex(string regex)
        {
            _includesRegex = regex == null ? false : true;
            if (_includesRegex)
            {
                if (!VerifyRegularExpression(regex))
                    throw new ArgumentException(string.Format(REGEX_COMPILIATION_ERROR, regex));
                _results = new Dictionary<string, string[]>();
                _regex = new Regex(regex, RegexOptions.Compiled);
            }
        }

        protected void ValidatePdfDocument()
        {
            if (!VerifyDocumentIsPdf(_pdfBytes))
                throw new ArgumentException(PDF_DOCUMENT_ERROR);
        }
        #endregion
    }
}
