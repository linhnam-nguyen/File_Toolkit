/*
 * This file is part of the Buildings and Habitats object Model (BHoM)
 * Copyright (c) 2015 - 2025, the respective contributors. All rights reserved.
 *
 * Each contributor holds copyright over their respective contributions.
 * The project versioning (Git) records all such contribution source information.
 *                                           
 *                                                                              
 * The BHoM is free software: you can redistribute it and/or modify         
 * it under the terms of the GNU Lesser General Public License as published by  
 * the Free Software Foundation, either version 3.0 of the License, or          
 * (at your option) any later version.                                          
 *                                                                              
 * The BHoM is distributed in the hope that it will be useful,              
 * but WITHOUT ANY WARRANTY; without even the implied warranty of               
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the                 
 * GNU Lesser General Public License for more details.                          
 *                                                                            
 * You should have received a copy of the GNU Lesser General Public License     
 * along with this code. If not, see <https://www.gnu.org/licenses/lgpl-3.0.html>.      
 */

using BH.oM.Adapters.File;
using BH.oM.Base.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;

namespace BH.Engine.Adapters.File
{
    public static partial class Compute
    {
        /***************************************************/
        /**** Public Methods                            ****/
        /***************************************************/

        [Description("Read a CSV file into a 2D object array, parsing each column using CsvConfig.ColumnDataFormats when provided.")]
        [Input("filePath", "Path to the CSV file.")]
        [Input("settings", "CSV settings including delimiter, decimal separator, and per-column formats. If null, defaults are used.")]
        [Input("active", "Boolean used to trigger the function.")]
        public static IEnumerable<IEnumerable<object>> ReadFromCsvFile(string filePath, CsvConfig settings = null, bool active = false)
        {
            if (!active)
                return Array.Empty<IEnumerable<object>>();

            if (string.IsNullOrWhiteSpace(filePath))
            {
                BH.Engine.Base.Compute.RecordError("The file path must not be empty.");
                return Array.Empty<IEnumerable<object>>();
            }

            if (!System.IO.File.Exists(filePath))
            {
                BH.Engine.Base.Compute.RecordError($"The file `{filePath}` does not exist.");
                return Array.Empty<IEnumerable<object>>();
            }

            if (settings == null)
                settings = new CsvConfig();

            var delim = settings.Delimiter ?? "\t";
            string[] lines;
            try
            {
                lines = System.IO.File.ReadAllLines(filePath);
            }
            catch (Exception e)
            {
                BH.Engine.Base.Compute.RecordError($"Error reading file:\n\t{e}");
                return Array.Empty<IEnumerable<object>>();
            }

            if (lines.Length == 0)
                return Array.Empty<IEnumerable<object>>();

            // 1) Parse lines into raw string rows (CSV rules: quotes + escaped quotes)
            var rawRows = new List<string[]>();
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                if (string.IsNullOrEmpty(line))
                    continue;

                rawRows.Add(SplitCsvLine(line, delim));
            }

            if (rawRows.Count == 0)
                return Array.Empty<IEnumerable<object>>();

            // 2) Normalize columns (pad ragged rows to max width)
            int rAll = rawRows.Count;
            int c = 0;
            for (int i = 0; i < rAll; i++)
                if (rawRows[i].Length > c) c = rawRows[i].Length;

            if (c == 0)
                return Array.Empty<IEnumerable<object>>();

            // 3) Decide data start index based on IncludeHeader
               var result = new List<List<object>>(rAll);

            for (int i = 0; i < rAll; i++)
            {
                bool isHeader = settings.IncludeHeader && i == 0;

                var src = rawRows[i];
                var row = new List<object>(c);

                for (int j = 0; j < c; j++)
                {
                    string cell = j < src.Length ? src[j] : null;
                    row.Add(ParseCell(cell, j, settings, isHeader));
                }

                result.Add(row);
            }

            return result;
        }

        /***************************************************/
        /**** Private Helpers                           ****/
        /***************************************************/

        private static string[] SplitCsvLine(string line, string delimiter)
        {
            if (string.IsNullOrEmpty(line))
                return Array.Empty<string>();

            var cells = new List<string>();
            var current = new System.Text.StringBuilder();
            bool inQuotes = false;
            int i = 0;
            int n = line.Length;
            int dlen = string.IsNullOrEmpty(delimiter) ? 0 : delimiter.Length;

            while (i < n)
            {
                char ch = line[i];

                if (ch == '"')
                {
                    if (inQuotes && i + 1 < n && line[i + 1] == '"')
                    {
                        current.Append('"'); // Escaped quote
                        i += 2;
                        continue;
                    }
                    inQuotes = !inQuotes;
                    i++;
                    continue;
                }

                if (!inQuotes && dlen > 0 && i + dlen <= n &&
                    string.CompareOrdinal(line, i, delimiter, 0, dlen) == 0)
                {
                    cells.Add(current.ToString());
                    current.Length = 0;
                    i += dlen;
                    continue;
                }

                current.Append(ch);
                i++;
            }

            cells.Add(current.ToString());
            return cells.ToArray();
        }

        /***************************************************/

        private static object ParseCell(string raw, int columnIndex, CsvConfig settings, bool isHeader = false)
        {
            if (string.IsNullOrEmpty(raw))
                return null;

            if (isHeader)
                return raw;

            bool hasColumnFormat =
                settings.ColumnDataFormats != null &&
                columnIndex >= 0 &&
                columnIndex < settings.ColumnDataFormats.Count;

            if (hasColumnFormat)
            {
                switch (settings.ColumnDataFormats[columnIndex])
                {
                    case StringType.Boolean:
                        {
                            var b = ParseBool(raw, settings);
                            return b.HasValue ? (object)b.Value : null;
                        }

                    case StringType.Numeric:
                        {
                            var num = ParseNumeric(raw, settings.DecimalSeparator);
                            return num.HasValue ? (object)num.Value : null;
                        }

                    case StringType.Date:
                        {
                            var dt = ParseDate(raw, settings.DateTimeFormat);
                            if (dt.HasValue) return dt.Value;

                            // OA serial fallback (Excel serial date)
                            var serial = ParseNumeric(raw, settings.DecimalSeparator);
                            if (serial.HasValue)
                            {
                                try { return DateTime.FromOADate(serial.Value); }
                                catch { /* ignore */ }
                            }
                            return null;
                        }

                    case StringType.Text:
                        return raw;
                }
            }

            // Try Bool
            var bParsed = ParseBool(raw, settings);
            if (bParsed.HasValue) return bParsed.Value;

            // Try Number
            var d = ParseNumeric(raw, settings.DecimalSeparator);
            if (d.HasValue) return d.Value;

            // Try Date
            var when = ParseDate(raw, settings.DateTimeFormat);
            if (when.HasValue) return when.Value;

            // OA serial fallback
            var serial2 = ParseNumeric(raw, settings.DecimalSeparator);
            if (serial2.HasValue)
            {
                try { return DateTime.FromOADate(serial2.Value); }
                catch { /* ignore */ }
            }

            // Text as-is
            return raw;
        }

        /***************************************************/

        private static bool? ParseBool(string raw, CsvConfig settings)
        {
            if (string.Equals(raw, "true", StringComparison.OrdinalIgnoreCase)) return true;
            if (string.Equals(raw, "false", StringComparison.OrdinalIgnoreCase)) return false;

            if (settings.BooleanAsNumber)
            {
                if (raw == "1") return true;
                if (raw == "0") return false;
            }
            return null;
        }

        /***************************************************/

        private static double? ParseNumeric(string raw, string decimalSeparator)
        {
            if (string.IsNullOrEmpty(raw))
                return null;

            var norm = string.IsNullOrEmpty(decimalSeparator) || decimalSeparator == "."
                       ? raw
                       : raw.Replace(decimalSeparator, ".");

            if (double.TryParse(norm, NumberStyles.Any, CultureInfo.InvariantCulture, out var value))
                return value;

            return null;
        }

        /***************************************************/

        private static DateTime? ParseDate(string raw, DateFormatOptions option)
        {
            if (string.IsNullOrEmpty(raw))
                return null;

            string[] patterns;

            switch (option)
            {
                case DateFormatOptions.ISO8601:
                    patterns = new[]
                    {
                        "o",
                        "yyyy-MM-ddTHH:mm:ss",
                        "yyyy-MM-ddTHH:mm:ss.FFFFFFFK",
                        "yyyy-MM-dd"
                    };
                    break;

                case DateFormatOptions.US:
                    patterns = new[]
                    {
                        "MM/dd/yyyy",
                        "M/d/yyyy",
                        "MM/dd/yy"
                    };
                    break;

                case DateFormatOptions.EU:
                default:
                    patterns = new[]
                    {
                        "dd/MM/yyyy",
                        "d/M/yyyy",
                        "dd/MM/yy"
                    };
                    break;
            }

            for (int i = 0; i < patterns.Length; i++)
            {
                DateTime tmp;
                if (DateTime.TryParseExact(raw, patterns[i], CultureInfo.InvariantCulture,
                                           DateTimeStyles.None, out tmp))
                    return tmp;
            }

            DateTime any;
            if (DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.None, out any))
                return any;

            return null;
        }

        /***************************************************/
    }
}