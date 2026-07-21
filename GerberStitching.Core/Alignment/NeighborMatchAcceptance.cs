using System;
using System.Reflection;

namespace GerberViewer.Stitching.Alignment
{
    public static class NeighborMatchAcceptance
    {
        /// <summary>Migration guard: a neighbor match is accepted only when pairResult?.Eval?.IsMatch == true; Eval presence alone is never sufficient.</summary>
        public static bool IsAccepted(object pairResult)
        {
            if (pairResult == null) return false;
            var eval = pairResult.GetType().GetProperty("Eval", BindingFlags.Instance | BindingFlags.Public)?.GetValue(pairResult, null);
            if (eval == null) return false;
            var isMatch = eval.GetType().GetProperty("IsMatch", BindingFlags.Instance | BindingFlags.Public)?.GetValue(eval, null);
            return isMatch is bool && (bool)isMatch;
        }
    }
}
