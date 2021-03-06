//---------------------------------------------------------------------------
//
// Copyright (c) Microsoft Corporation.  All rights reserved.
//
// Description: Implementation of the class DashStyle
//
// History: 11\15\2004: Michka - Created it
//
//---------------------------------------------------------------------------

using System;
using MS.Internal;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Diagnostics;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Media;
using System.Windows.Media.Composition;
using System.Windows;
using System.Text.RegularExpressions;
using System.Windows.Media.Animation;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;

using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;

namespace System.Windows.Media 
{
    #region DashStyle
    /// <summary>
    /// This class captures the array of dashe and gap lengths and the dash offset.
    /// </summary>
    [Localizability(LocalizationCategory.None, Readability = Readability.Unreadable)]
    public partial class DashStyle : Animatable, DUCE.IResource
    {
        #region Constructors

        /// <summary>
        ///     Default constructor
        /// </summary>
        public DashStyle()
        {
        }

        /// <summary>
        ///     Constructor from an array and offset
        /// <param name="dashes">
        ///     The array of lengths of dashes and gaps, measured in Thickness units.
        ///     If the value of dashes is null then the style will be solid
        /// </param>
        /// <param name="offset">
        ///     Determines where in the dash sequence the stroke will start
        /// </param>
        /// </summary>
        public DashStyle(IEnumerable<double> dashes, Double offset)
        {
            Offset = offset;

            if (dashes != null)
            {
                Dashes = new DoubleCollection(dashes);
            }
        }


        #endregion Constructors

 
        #region Internal Methods
        /// <summary>
        /// Returns the dashes information.
        /// </summary>
        /// <SecurityNote>
        ///     Critical: Returns a pointer and manipulates unsafe code.This data
        ///     is safe to expose, it is the pointer manipulation that makes it risky.
        /// </SecurityNote>
        [SecurityCritical]
        internal unsafe void GetDashData(MIL_PEN_DATA* pData, out double[] dashArray)
        {
            DoubleCollection vDashes = Dashes;
            int count = 0;
                                         
            if (vDashes != null)
            {
                count = vDashes.Count;
            }

            unsafe
            {
                pData->DashArraySize = (UInt32)count * sizeof(double);
                pData->DashOffset = Offset;
            }

            if (count > 0)
            {
                dashArray = vDashes._collection.ToArray();
            }
            else
            {
                dashArray = null;
            }
        }
        #endregion Internal Methods
    }
    #endregion
}
