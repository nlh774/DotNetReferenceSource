//------------------------------------------------------------------------------
// <copyright file="XmlTokenizedType.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
// <owner current="true" primary="true">[....]</owner>
//------------------------------------------------------------------------------

 
namespace System.Xml {

    // NOTE: Absolute numbering is utilized in DtdParser. -[....]
    public enum XmlTokenizedType {
        CDATA       = 0,
        ID          = 1,
        IDREF       = 2,
        IDREFS      = 3,
        ENTITY      = 4,
        ENTITIES    = 5,
        NMTOKEN     = 6,
        NMTOKENS    = 7,
        NOTATION    = 8,
        ENUMERATION = 9,
        QName       = 10,
        NCName      = 11,
        None        = 12
    }
}
