using System.Collections.Generic;
using System.Dynamic;

namespace PlainSequencer
{
    public class ServiceGlobalData : IServiceGlobalData
    {
        private ExpandoObject globalData = new ExpandoObject();
        private IDictionary<string, object> AsDict => globalData;
        public void UpsertData(string key, object value) => AsDict.Add(key, value);

        public ExpandoObject GetData() => globalData;
    }
}
