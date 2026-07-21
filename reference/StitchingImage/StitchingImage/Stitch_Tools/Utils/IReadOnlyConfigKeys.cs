using System.Collections.Generic;

namespace StitchingImage.Stitch_Tools.Utils
{
    public interface IReadOnlyConfigKeys
    {
        IEnumerable<string> GetReadOnlyKeys();
    }
}
