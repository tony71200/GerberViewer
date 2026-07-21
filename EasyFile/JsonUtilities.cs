using System.IO;
using Newtonsoft.Json;

namespace EasyFile
{
    public class JsonConveter
    {
        public T JsonToClass<T>(string Json)
        {
            return JsonConvert.DeserializeObject<T>(Json);
        }

        public string ClassToJson(object _Jclass)
        {
            return JsonConvert.SerializeObject(_Jclass);
        }
    }

    public class JFile
    {
        public T ReadJsonFile<T>(string path)
        {
            using (var streamReader = new StreamReader(path))
            using (var jsonReader = new JsonTextReader(streamReader))
            {
                var serializer = new JsonSerializer();
                return serializer.Deserialize<T>(jsonReader);
            }
        }

        public void WriteJsonFile(string path, object _Jclass, bool NeedFormatting)
        {
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            using (var streamWriter = new StreamWriter(path))
            using (var jsonWriter = new JsonTextWriter(streamWriter))
            {
                jsonWriter.Formatting = NeedFormatting ? Formatting.Indented : Formatting.None;
                var serializer = new JsonSerializer();
                serializer.Serialize(jsonWriter, _Jclass);
            }
        }
    }
}
