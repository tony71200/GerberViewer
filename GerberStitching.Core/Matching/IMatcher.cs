using System;
using System.Threading;

namespace GerberViewer.Stitching.Matching
{
    public interface IMatcher : IDisposable
    {
        string MatcherName { get; }
        MatchResult Match(MatchRequest request, CancellationToken cancellationToken);
    }
}
