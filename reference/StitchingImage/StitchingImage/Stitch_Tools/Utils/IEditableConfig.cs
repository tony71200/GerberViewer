using System.Collections.Generic;

namespace StitchingImage.Stitch_Tools.Utils
{
    public interface IEditableConfig
    {
        IEnumerable<string> GetKeys();
        object GetValue(string key);
        bool UpdateValue(string key, string rawValue, out string error);
    }
}
