/*
 * This file is part of the Buildings and Habitats object Model (BHoM)
 * Copyright (c) 2015 - 2026, the respective contributors. All rights reserved.
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
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BH.Engine.Adapters.File
{
    public static partial class Query
    {
        /***************************************************/
        /*** Methods                                     ***/
        /***************************************************/

        [Description("Try to get the encoding of the file.")]
        [Input("file", "The file to get the encoding of.")]
        [Output("The encoding of the file if it can be discovered, null if unknown.")]
        public static Encoding Encoding(this FSFile file)
        {
            byte[] contents = file.ContentsAsByteArray();
            if (contents == null) return null;
            foreach (EncodingInfo candidate in System.Text.Encoding.GetEncodings())
            {
                byte[] pre = candidate.GetEncoding().GetPreamble();
                int len = pre.Length;
                if (len == 0) continue;
                byte[] check = new byte[len];
                Array.ConstrainedCopy(contents, 0, check, 0, len);
                if (pre.SequenceEqual(check))
                {
                    return candidate.GetEncoding();
                }
            }
            return null;
        }

        /***************************************************/


        public static Encoding FromEnum(Encodings encodingEnumValue)
        {
            switch (encodingEnumValue)
            {
                case Encodings.FromFile: return null;
                case Encodings.ASCII: return System.Text.Encoding.ASCII;
                case Encodings.BigEndianUnicode: return System.Text.Encoding.BigEndianUnicode;
                case Encodings.Unicode: return System.Text.Encoding.Unicode;
                case Encodings.UTF32: return System.Text.Encoding.UTF32;
                case Encodings.UTF7: return System.Text.Encoding.UTF7;
                case Encodings.UTF8: return System.Text.Encoding.UTF8;

                default: return System.Text.Encoding.UTF8;
            }
        }
    }
}






