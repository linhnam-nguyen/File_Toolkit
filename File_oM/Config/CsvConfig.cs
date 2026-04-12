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


using BH.oM.Adapter;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace BH.oM.Adapters.File
{
    public class CsvConfig : ActionConfig
    {
        [Description(" The delimiter to use in the CSV file. Common options are ',' for comma, ';' for semicolon, and '\\t' for tab by default.")]
        public string Delimiter { get; set; } = "\t";

        [Description(" Whether to include objects that do not have a string representation. If true, these objects will be included using their ToString() method or a placeholder if not available. If false, such objects will be skipped.")]
        public bool IncludeObjects { get; set; } = false;

        [Description(" If specified, the value of the property representing object will be serialized in the CSV file. If null, object type name will shown.")]
        public string PropertyName { get; set; } = null;

        [Description(" Whether to include a header row with column names in the CSV file. Default is true, meaning the first row will contain the property names.")]
        public bool IncludeHeader { get; set; } = true;

        [Description("Configuration for formatting datatype for each column. If null, default formatting will be applied based on the data type.")]
        public List<StringType?> ColumnDataFormats { get; set; } = null;

        [Description(" Whether to represent boolean values as numbers (1 for true, 0 for false) instead of text (true/false). Default is false, meaning booleans will be represented as text.")]
        public bool BooleanAsNumber { get; set; } = false;

        [Description(" The character to use as the decimal separator in numerical values. Common options are '.' for dot and ',' for comma. Default is '.'")]
        public string DecimalSeparator { get; set; } = ".";

        [Description(" If specified, numerical values will be rounded to this number of decimal places. If null, no rounding is applied.")]
        public double? Digit { get; set; } = null;

        [Description(" The format to use for date values. Options include ISO8601 (e.g., 2023-10-05T14:48:00Z), US (e.g., 10/05/2023), and EU (e.g., 05/10/2023). Default is ISO8601.")]
        public DateFormatOptions DateTimeFormat { get; set; } = DateFormatOptions.EU;

        [Description(" The text encoding to use when reading or writing the CSV file. Default is UTF-8.")]
        public Encodings Encoding { get; set; } = Encodings.UTF8;
    }
}





