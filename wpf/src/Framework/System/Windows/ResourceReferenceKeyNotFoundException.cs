//---------------------------------------------------------------------------
//
// File: ResourceReferenceExpression.cs
//
// Description:
//   A resource could not be found
//
// Copyright (C) 2005 by Microsoft Corporation.  All rights reserved.
// 
//---------------------------------------------------------------------------

using System;
using System.Runtime.Serialization;
using System.Security;
using System.Security.Permissions;

namespace System.Windows
{
    ///<summary>Exception class for resource reference</summary>
    [Serializable]
    public class ResourceReferenceKeyNotFoundException: InvalidOperationException
    {
        ///<summary>
        /// Constructor
        ///</summary>
        public ResourceReferenceKeyNotFoundException()
        {
            _resourceKey = null;
        }
        
        ///<summary>
        /// Constructor
        ///</summary>
        public ResourceReferenceKeyNotFoundException(string message, object resourceKey) 
                        : base(message)
        {
            _resourceKey = resourceKey;
        }

        ///<summary>
        /// Constructor (required for Xml web service)
        ///</summary>
        protected  ResourceReferenceKeyNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            _resourceKey = info.GetValue("Key", typeof(object));
        }

        ///<summary>
        /// LineNumber that the exception occured on.
        ///</summary>
        public object Key
        {
            get { return _resourceKey; }
        }


        /// <summary>
        /// Populates a SerializationInfo with the data needed to serialize the target object.
        /// </summary>
        /// <param name="info">
        /// The SerializationInfo to populate with data.
        /// </param>
        /// <param name="context">
        /// The destination for this serialization.
        /// </param>
        ///<SecurityNote>
        ///     Critical: calls Exception.GetObjectData which LinkDemands
        ///     PublicOK: a demand exists here
        ///</SecurityNote>
        [SecurityCritical]
        [SecurityPermissionAttribute(SecurityAction.Demand,SerializationFormatter=true)]
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("Key", _resourceKey);
        }

        private object _resourceKey;
    }

    
}


