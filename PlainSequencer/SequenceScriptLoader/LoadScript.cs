using Newtonsoft.Json;
using PlainSequencer.Script;
using System.IO;

namespace PlainSequencer.SequenceScriptLoader
{
    public class LoadScript : ILoadScript
    {
        public SequenceScript LoadYamlFile(string filename) => YamlLoad.Load<SequenceScript>(filename);

        public SequenceScript LoadJsonFile(string filename)
        {
            var contents = File.ReadAllText(filename);
            return JsonConvert.DeserializeObject<SequenceScript>(contents);
        }

        public SequenceScript ConvertUnknownContents(string contents)
        {
            var judge = contents.Substring(0, 100).TrimStart();
            if (judge.StartsWith('[') || judge.StartsWith("{"))
                return JsonConvert.DeserializeObject<SequenceScript>(contents);

            return YamlLoad.LoadYamlContentsUnguarded<SequenceScript>(contents);
        }
    }
}
