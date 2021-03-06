//------------------------------------------------------------------------------
// <copyright file="CounterSetInstanceCounterDataSet.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace System.Diagnostics.PerformanceData {
    using System;
    using System.Threading;
    using System.Runtime.InteropServices;
    using System.ComponentModel;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Security;
    using Microsoft.Win32;

    /// <summary>
    /// CounterData class is used to store actual raw counter data. It is the value element within
    /// CounterSetInstanceCounterDataSet, which is part of CounterSetInstance.
    /// </summary>
    [System.Security.Permissions.HostProtection(MayLeakOnAbort = true)]
    public sealed class CounterData {
        [SecurityCritical]
        unsafe private Int64 * m_offset;

        /// <summary>
        /// CounterData constructor
        /// </summary>
        /// <param name="counterId"> counterId would come from CounterSet::AddCounter() parameter </param>
        /// <param name="pCounterData"> The memory location to store raw counter data </param>
        [System.Security.SecurityCritical]
        unsafe internal CounterData(Int64 * pCounterData) {
            m_offset    = pCounterData;
            * m_offset  = 0;
        }

        /// <summary>
        /// Value property it used to query/update actual raw counter data.
        /// </summary>
        public Int64 Value {
            [System.Security.SecurityCritical]
            get {
                unsafe {
                    return Interlocked.Read(ref (* m_offset));
                }
            }
            [System.Security.SecurityCritical]
            set {
                unsafe {
                    Interlocked.Exchange(ref (* m_offset), value);
                }
            }
        }

        [System.Security.SecurityCritical]
        public void Increment() {
            unsafe {
                Interlocked.Increment (ref (*m_offset));
            }   
        }

        [System.Security.SecurityCritical]
        public void Decrement() {
            unsafe {
                Interlocked.Decrement (ref (*m_offset));
            }   
        }

        [System.Security.SecurityCritical]
        public void IncrementBy(Int64 value) {
            unsafe {
                Interlocked.Add (ref (*m_offset), value);
            }   
        }

        /// <summary>
        /// RawValue property it used to query/update actual raw counter data.
        /// This property is not thread-safe and should only be used 
        /// for performance-critical single-threaded access.
        /// </summary>
        public Int64 RawValue {
            [System.Security.SecurityCritical]
            get {
                unsafe {
                    return (* m_offset);
                }
            }
            [System.Security.SecurityCritical]
            set {
                unsafe {
                    * m_offset = value;
                }
            }
        }

        }

    /// <summary>
    /// CounterSetInstanceCounterDataSet is part of CounterSetInstance class, and is used to store raw counter data
    /// for all counters added in CounterSet.
    /// </summary>
    [System.Security.Permissions.HostProtection(MayLeakOnAbort = true)]
    public sealed class CounterSetInstanceCounterDataSet : IDisposable {
        internal        CounterSetInstance             m_instance;
        private         Dictionary<Int32, CounterData> m_counters;
        private         Int32                          m_disposed;
        [SecurityCritical]
        unsafe internal byte *                         m_dataBlock;

        [System.Security.SecurityCritical]
        [SuppressMessage("Microsoft.Usage", "CA2208:InstantiateArgumentExceptionsCorrectly")]
        internal CounterSetInstanceCounterDataSet(CounterSetInstance thisInst) {
            m_instance = thisInst;
            m_counters = new Dictionary<Int32, CounterData>();

            unsafe {
                if (m_instance.m_counterSet.m_provider == null) {
                    throw new ArgumentException(SR.GetString(SR.Perflib_Argument_ProviderNotFound, m_instance.m_counterSet.m_providerGuid), "ProviderGuid");
                }
                if (m_instance.m_counterSet.m_provider.m_hProvider.IsInvalid) {
                    throw new InvalidOperationException(SR.GetString(SR.Perflib_InvalidOperation_NoActiveProvider, m_instance.m_counterSet.m_providerGuid));
                }

                m_dataBlock = (byte *) Marshal.AllocHGlobal(m_instance.m_counterSet.m_idToCounter.Count * sizeof(Int64));
                if (m_dataBlock == null) {
                    throw new InsufficientMemoryException(SR.GetString(SR.Perflib_InsufficientMemory_InstanceCounterBlock, m_instance.m_counterSet.m_counterSet, m_instance.m_instName));
                }

                Int32 CounterOffset = 0;

                foreach (KeyValuePair<Int32, CounterType> CounterDef in m_instance.m_counterSet.m_idToCounter) {
                    CounterData thisCounterData = new CounterData((Int64 *) (m_dataBlock + CounterOffset * sizeof(Int64)));

                    m_counters.Add(CounterDef.Key, thisCounterData);
                    // ArgumentNullException - CounterName is NULL
                    // ArgumentException - CounterName already exists.

                    uint Status = UnsafeNativeMethods.PerfSetCounterRefValue(
                                    m_instance.m_counterSet.m_provider.m_hProvider,
                                    m_instance.m_nativeInst,
                                    (uint) CounterDef.Key,
                                    (void *) (m_dataBlock + CounterOffset * sizeof(Int64)));
                    if (Status != (uint) UnsafeNativeMethods.ERROR_SUCCESS) {
                        Dispose(true);

                        // ERROR_INVALID_PARAMETER or ERROR_NOT_FOUND
                        switch (Status) {
                            case (uint) UnsafeNativeMethods.ERROR_NOT_FOUND:
                                throw new InvalidOperationException(SR.GetString(SR.Perflib_InvalidOperation_CounterRefValue, m_instance.m_counterSet.m_counterSet, CounterDef.Key, m_instance.m_instName));

                            default:
                                throw new Win32Exception((int) Status);
                        }
                    }
                    CounterOffset ++;
                }
            }
        }

        [System.Security.SecurityCritical]
        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        [System.Security.SecurityCritical]
        ~CounterSetInstanceCounterDataSet() {
            Dispose(false);
        }
        [System.Security.SecurityCritical]
        private void Dispose(bool disposing) {
            if (Interlocked.Exchange(ref m_disposed, 1) == 0) {
                unsafe {
                    if (m_dataBlock != null) {
                        // Need to free allocated heap memory that is used to store all raw counter data.
                        Marshal.FreeHGlobal((System.IntPtr)m_dataBlock);
                        m_dataBlock = null;
                    }
                }
            }
        }

        /// <summary>
        /// CounterId indexer to access specific CounterData object.
        /// </summary>
        /// <param name="counterId">CounterId that matches one CounterSet::AddCounter()call</param>
        /// <returns>CounterData object with matched counterId</returns>
        public CounterData this[Int32 counterId] {
            get {
                if (m_disposed != 0) {
                    return null;
                }

                try {
                    return m_counters[counterId];
                }
                catch (KeyNotFoundException) {
                    return null;
                }
                catch {
                    throw;
                }
            }
        }

        /// <summary>
        /// CounterName indexer to access specific CounterData object.
        /// </summary>
        /// <param name="counterName">CounterName that matches one CounterSet::AddCounter() call</param>
        /// <returns>CounterData object with matched counterName</returns>
        [SuppressMessage("Microsoft.Usage", "CA2208:InstantiateArgumentExceptionsCorrectly")]
        public CounterData this[String counterName] {
            get {
                if (counterName == null) {
                    throw new ArgumentNullException("CounterName");
                }
                if (counterName.Length == 0) {
                    throw new ArgumentNullException("CounterName");
                }
                if (m_disposed != 0) {
                    return null;
                }

                try {
                    Int32 CounterId = m_instance.m_counterSet.m_stringToId[counterName];
                    try {
                        return m_counters[CounterId];
                    }
                    catch (KeyNotFoundException) {
                        return null;
                    }
                    catch {
                        throw;
                    }
                }
                catch (KeyNotFoundException) {
                    return null;
                }
                catch {
                    throw;
                }
            }
        }
    }
}
