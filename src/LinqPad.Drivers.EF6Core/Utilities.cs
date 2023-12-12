using System;
using System.Collections;
using System.Diagnostics;
using LINQPad;

namespace CloudNimble.LinqPad.Drivers.EF6Core
{

    /// <summary>
    /// Decompiled from LINQPad.Util class of LINQPad 5 by Joseph Albahari
    /// </summary>
    public static class Utilities
    {

        /// <summary>
        /// 
        /// </summary>
        [Conditional("DEBUG")]
        public static void Debug()
        {
            if (!Debugger.IsAttached)
                Debugger.Launch();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="description"></param>
        /// <param name="funcToEvalAndDump"></param>
        /// <param name="runOnNewThread"></param>
        /// <param name="isCollection"></param>
        /// <returns></returns>
        internal static DumpContainer OnDemand<T>(string description, Func<T> funcToEvalAndDump, bool runOnNewThread, bool isCollection)
        {
            var dc = new DumpContainer();
            var hyperlinq = new Hyperlinq(delegate
            {
                dc.Content = "Executing...";
                dc.Content = funcToEvalAndDump();
            }, description, runOnNewThread);

            if (isCollection || typeof(IEnumerable).IsAssignableFrom(typeof(T)))
            {
                hyperlinq.CssClass = "collection";
            }

            dc.Content = hyperlinq;
            return dc;
        }


    }

}
