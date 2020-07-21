using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace AMLCore.Misc
{
    public static class JsonSerialization
    {
        private static JsonSerializer _serializer = JsonSerializer.Create(new JsonSerializerSettings { Formatting = Formatting.Indented });

        public static T Deserialize<T>(string data)
        {
            using (var r = new StringReader(data))
            {
                return (T)_serializer.Deserialize(r, typeof(T));
            }
        }

        public static string Serialize<T>(T obj)
        {
            //return JSONWriter.ToJson(obj);
            using (var w = new StringWriter())
            {
                _serializer.Serialize(w, obj);
                return w.ToString();
            }
        }
    }
}
