/*
 
 Credit to Jaguar for contributing this class
 
 */

using System;
using System.Diagnostics;
using System.Net;
using System.Threading;

namespace XEMSign
{
    /// <summary>
    /// A custom implementation of IAsyncResult.
    /// </summary>
    /// <remarks>
    /// Used to track and transmit data between BeginX and EndX methods as well as any asynchronous operations assigned to ManualAsyncResult internally. When the AsyncWaitHandle.WaitOne function is used, it waits for all internal asynchronous operations to complete to prevent premeture thread release.
    /// </remarks>
    public class ManualAsyncResult2 : IAsyncResult
    {
        internal ManualAsyncResult2()
        {
            m_waitHandle = new EventWaitHandle(false, EventResetMode.ManualReset);
        }

        internal string Path { get; set; }
        internal string Query { get; set; }
        internal byte[] Bytes { get; set; }

        internal HttpWebRequest HttpWebRequest { get; set; }

        /// <summary>
        /// Gets a user-defined object that qualifies or contains information about an asynchronous operation.
        /// </summary>
        /// 
        internal WebResponse Response { get; set; }
        public object AsyncState { get; set; }
        /// <summary>
        /// Gets a <see cref="T:System.Threading.WaitHandle" /> that is used to wait for an asynchronous operation to complete.
        /// </summary>
        public WaitHandle AsyncWaitHandle => m_waitHandle;
        /// <summary>
        /// Gets a value that indicates whether the asynchronous operation completed synchronously.
        /// </summary>
        public bool CompletedSynchronously => false;
        /// <summary>
        /// Gets a value that indicates whether the asynchronous operation has completed.
        /// </summary>
        public bool IsCompleted => m_waitHandle.WaitOne(0);
        /// <summary>
        /// Gets the error.
        /// </summary>
        /// <value>
        /// The error.
        /// </value>
        public Exception Error { get; set; }

        internal AsyncCallback WrapHandler(AsyncCallback callback)
        {
            Interlocked.Increment(ref m_outstandingCallbacks);

            return ar => {
                try
                {
                    callback(ar);


                    if (0 == Interlocked.Decrement(ref m_outstandingCallbacks))
                    {
                        m_waitHandle.Set();
                    }
                }
                catch (Exception ex)
                {
                    Error = ex;

                    m_waitHandle.Set();
                }
            };
        }

        internal long TimeOutWait()
        {
            var watch = new Stopwatch();

            watch.Start();

            AsyncWaitHandle.WaitOne(5000);

            watch.Stop();

            if (watch.ElapsedMilliseconds >= 5000)
            {
                HttpWebRequest.Abort();

                throw new Exception("Timed Out");
            }

            return watch.ElapsedMilliseconds;
        }

        internal EventWaitHandle m_waitHandle;

        internal int m_outstandingCallbacks;
    }
}
