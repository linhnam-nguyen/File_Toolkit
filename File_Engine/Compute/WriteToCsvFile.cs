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

using BH.Engine.Base;
using BH.oM.Adapters.File;
using BH.oM.Base.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;


namespace BH.Engine.Adapters.File
{
    public static partial class Compute
    {
        /***************************************************/
        /**** Public Methods                            ****/
        /***************************************************/

        [Description("Write a CSV file with the input data or objects.")]
        [Input("data", "Data to write to the file.")]
        [Input("filePath", "Path to the file.")]
        [Input("settings", "Settings to use when writing the CSV file. If null, default settings will be used.")]
        [Input("replace", "If the file exists, you need to set this to true in order to allow overwriting it.")]
        [Input("active", "Boolean used to trigger the function.")]
        public static bool WriteToCsvFile(object data, string filePath, CsvConfig settings = null, bool replace = false, bool active = false)
        {
            if (!active || data == null)
                return false;

            if (string.IsNullOrWhiteSpace(filePath))
            {
                BH.Engine.Base.Compute.RecordError($"The filePath `{filePath}` must not be empty.");
                return false;
            }

            // Make sure no invalid chars are present.
            filePath = Path.Combine(Path.GetDirectoryName(filePath), Path.GetFileName(filePath));

            // If the file exists already, stop execution if `replace` is not true.
            bool fileExisted = System.IO.File.Exists(filePath);
            if (!replace && fileExisted)
            {
                BH.Engine.Base.Compute.RecordWarning($"The file `{filePath}` exists already. To replace its content, set `{nameof(replace)}` to true.");
                return false;
            }

            // Serialise to csv and create the file and directory.
            string table = FromObject(data,settings);
            System.IO.FileInfo fileInfo = new System.IO.FileInfo(filePath);
            fileInfo.Directory.Create(); // If the directory already exists, this method does nothing.

            try
            {
                System.IO.File.WriteAllText(filePath, table, Query.FromEnum(settings.Encoding));
            }
            catch (Exception e)
            {
                BH.Engine.Base.Compute.RecordError($"Error writing to file:\n\t{e.ToString()}");
                return false;
            }

            return true;
        }

        /***************************************************/

        public static string FromObject(this object obj, CsvConfig settings = null)
        {
            if (settings == null)
                settings = new CsvConfig();

            object[,] flatten;

            if (obj is string || obj.GetType().IsPrimitive || obj is Enum || obj is IFormattable)
            {
                return FormatCell(obj, settings, null);
            }
            // Shape normalisation (priority to arrays)
            else if (obj is object[,] rect)
            {
                flatten = rect;
            }
            else if (obj is object[][] jagged)
            {
                flatten = ToRect(jagged);
            }
            else if (obj is IEnumerable<IEnumerable<object>> nested)
            {
                flatten = ToRect(nested);
            }
            else if (obj is IEnumerable<object> list && !(obj is string))
            {
                flatten = ToRect(list);
            }
            else
            {
                BH.Engine.Base.Compute.RecordError(
                    $"The input data of type `{obj?.GetType().Name ?? "null"}` is not supported. ");
                return string.Empty;
            }

            // Convert cells to string array with formatting rules
            string[,] table = ToStringTable(flatten, settings);

            // Serialize to CSV
            var delim = settings.Delimiter ?? "\t";
            int r = table.GetLength(0);
            int c = table.GetLength(1);
            var sb = new StringBuilder(r * Math.Max(1, c) * 4);

            for (int i = 0; i < r; i++)
            {
                var encoded = Enumerable.Range(0, c).Select(j => EscapeCsv(table[i, j] ?? string.Empty, delim));
                sb.Append(string.Join(delim, encoded));
                if (i < r - 1) sb.Append('\n');
            }

            return sb.ToString();
        }
        /*******************************************/
        /**** Private Methods                  *****/
        /*******************************************/

        private static object[,] ToRect(object[][] obj)
        {
            if (obj == null || obj.Length == 0)
                return new object[0, 0];
            int r = obj.Length;
            int c = obj.Max(a => a?.Length ?? 0);
            var result = new object[r, c];
            for (int i = 0; i < r; i++)
            {
                var row = obj[i] ?? new object[0];
                for (int j = 0; j < c; j++)
                {
                    result[i, j] = j < row.Length ? row[j] : null;
                }
            }
            return result;
        }

        /***************************************************/

        private static object[,] ToRect(IEnumerable<IEnumerable<object>> obj)
        {
            if (obj == null)
                return new object[0, 0];
            var list = obj.ToList();
            if (list.Count == 0)
                return new object[0, 0];
            int r = list.Count;
            int c = list.Max(a => a?.Count() ?? 0);
            var result = new object[r, c];
            for (int i = 0; i < r; i++)
            {
                var row = list[i]?.ToList() ?? new List<object>();
                for (int j = 0; j < c; j++)
                {
                    result[i, j] = j < row.Count ? row[j] : null;
                }
            }
            return result;
        }

        /***************************************************/

        private static object[,] ToRect(IEnumerable<object> obj)
        {
            if (obj == null)
                return new object[0, 0];

            var list = obj.ToList();
            if (list.Count == 0)
                return new object[0, 0];

            int r = list.Count;
            int c = 0;
            var result = new object[r, c];
            for (int i = 0; i < r; i++)
            {
                result[i, 0] = list[i];
            }
            return result;
        }

        /***************************************************/

        private static string[,] ToStringTable(object[,] obj, CsvConfig settings)
        {
            if (obj == null)
                return new string[0, 0];

            int r = obj.GetLength(0);
            int c = obj.GetLength(1);
            var result = new string[r, c];

            for (int i = 0; i < r; i++)
            {
                bool isHeader = (i == 0);
                for (int j = 0; j < c; j++)
                    result[i, j] = FormatCell(obj[i, j], settings, j, isHeader);
            }

            return result;
        }

        /***************************************************/

        private static string FormatCell(object value, CsvConfig settings, int? column, bool isHeader = false)
        {
            if (settings.ColumnDataFormats != null 
                && column.HasValue 
                && column.Value < settings.ColumnDataFormats.Count
                && ((settings.IncludeHeader && !isHeader) || !settings.IncludeHeader))
            {
                StringType? format = settings.ColumnDataFormats[column.Value];

                if (format.HasValue)
                {
                    switch (format)
                    {
                        case StringType.Boolean:
                            if(value is bool boolean)
                                return boolean.FormatBool(settings.BooleanAsNumber);
                            else
                                return string.Empty;

                        case StringType.Date:
                            if (value is IFormattable date)
                                return date.FormatDate(settings.DateTimeFormat);
                            else
                                return string.Empty;

                        case StringType.Numeric:
                            // Accept only IFormattable numerics here; else fall through to generic branch below
                            var fnum = value as IFormattable;
                            return fnum != null
                                 ? fnum.FormatNumeric((int?)settings.Digit, settings.DecimalSeparator)
                                 : string.Empty;

                        case StringType.Text:
                            // Re-run without ColumnDataFormats influence
                            var noColumnFormats = new CsvConfig
                            {
                                IncludeHeader = settings.IncludeHeader,
                                Digit = settings.Digit,
                                DecimalSeparator = settings.DecimalSeparator,
                                DateTimeFormat = settings.DateTimeFormat,
                                Delimiter = settings.Delimiter,
                                IncludeObjects = settings.IncludeObjects,
                                ColumnDataFormats = null
                            };
                            return FormatCell(value, noColumnFormats, null, isHeader);
                    }
                }
            }

            // numeric (round if Digit set)
            if (value.GetType().IsNumeric(false))
            {
                double d = System.Convert.ToDouble(value, CultureInfo.InvariantCulture);
                return d.FormatNumeric((int?)settings.Digit, settings.DecimalSeparator);
            }
            // Date/Time
            if (value is DateTime dt)
                return dt.FormatDate(settings.DateTimeFormat);

            // Bool
            if (value is bool b)
                return b.FormatBool(settings.BooleanAsNumber);

            // IFormattable (decimal, Guid, etc.)
            var formattable = value as IFormattable;
            if (formattable != null)
                return formattable.FormatIFormattable();

            // Enum
            if (value.GetType().IsEnum)
                return (value as Enum).FormatEnum();

            // Other unformattable types
            if (settings.IncludeObjects)
                return value.FormatObject();

            //String
            if (value is string s)
                return s;

            return string.Empty;
        }

        /***************************************************/

        private static string FormatDate(this IFormattable date, DateFormatOptions option)
        {
            if (date == null)
                return string.Empty;

            // Handle Excel-style serial number (double)
            if (date is double serial)
            {
                try
                {
                    var dt = DateTime.FromOADate(serial);
                    return dt.FormatDate(option); 
                }
                catch
                {
                    return serial.ToString(CultureInfo.InvariantCulture);
                }
            }

            // Handle DateTimeOffset
            if (date is DateTime dt2)
            {
                switch (option)
                {
                    case DateFormatOptions.ISO8601:
                        return dt2.ToString("o", CultureInfo.InvariantCulture);
                    case DateFormatOptions.US:
                        return dt2.ToString("MM/dd/yyyy", CultureInfo.InvariantCulture);
                    case DateFormatOptions.EU:
                    default:
                        return dt2.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
                }
            }

            return date.ToString(null, CultureInfo.InvariantCulture);
        }

        /***************************************************/

        private static string FormatNumeric(this IFormattable number, int? digits, string decimalSeparator)
        {
            if (number == null)
                return string.Empty;
            string raw;
            if (digits.HasValue)
            {
                int d = Math.Max(0, digits.Value);
                var format = d > 0 ? "0." + new string('#', d) : "0";
                raw = number.ToString(format, CultureInfo.InvariantCulture);
            }
            else
            {
                raw = number.ToString("G", CultureInfo.InvariantCulture);
            }
            // Apply custom decimal separator if needed
            if (decimalSeparator != "." && raw.Contains("."))
            {
                raw = raw.Replace(".", decimalSeparator);
            }
            return raw;
        }

        /***************************************************/

        private static string FormatBool(this bool b, bool asNumber)
        {
            return asNumber ? (b ? "1" : "0") : (b ? "true" : "false");
        }

        /***************************************************/

        private static string FormatEnum(this Enum e)
        {
            return System.Convert.ToString(e, CultureInfo.InvariantCulture);
        }

        /***************************************************/

        private static string FormatIFormattable(this IFormattable f)
        {
            return f.ToString(null, CultureInfo.InvariantCulture);
        }

        /***************************************************/

        private static string FormatObject(this object obj, string propName = null)
        {
            if (obj == null)
                return string.Empty;

            if (propName != null)
                return obj.GetType().GetProperty(propName)?.GetValue(obj).ToString();

            var toString = obj.ToString();
            if (toString != null && toString != obj.GetType().FullName)
                return toString;

            return $"<{obj.GetType().Name}>";
        }

        /***************************************************/

        private static string EscapeCsv(string input, string delimiter)
        {
            if (input == null) return string.Empty;

            bool mustQuote =
                input.IndexOf('"') >= 0 ||
                input.IndexOf('\n') >= 0 ||
                input.IndexOf('\r') >= 0 ||
                (!string.IsNullOrEmpty(delimiter) && input.IndexOf(delimiter, StringComparison.Ordinal) >= 0);

            if (!mustQuote)
                return input;

            var doubled = input.Replace("\"", "\"\"");
            return "\"" + doubled + "\"";
        }

        /***************************************************/

    }
}
