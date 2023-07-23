//using System.Collections.Generic;
//using System.Dynamic;

//namespace PlainSequencer
//{
//    public class ServiceFileData : IServiceFileData
//    {
//        private Dictionary<string, object> files = new Dictionary<string, object>();

//        public object GetFileData(string filekey) => files[filekey];

//        public IEnumerable<string> GetFileKeys() => files.Keys;

//        public void UpsertFileData(string filekey, object data)
//        {
//            if (files.ContainsKey(filekey))
//                files[filekey] = data;
//            else
//                files.Add(filekey, data);
//        }
//    }
//}
