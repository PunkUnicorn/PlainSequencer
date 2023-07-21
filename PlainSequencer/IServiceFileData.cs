using System.Collections.Generic;
using System.Dynamic;

namespace PlainSequencer
{
    public interface IServiceFileData
    {
        IEnumerable<string> GetFileKeys();

        object GetFileData(string filekey);

        void UpsertFileData(string filekey, object data);
    }
}