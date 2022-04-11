using PlainSequencer.Script;

namespace PlainSequencer.SequenceScriptLoader
{
    public interface ILoadScript
    {
        SequenceScript ConvertUnknownContents(string contents);
        SequenceScript LoadJsonFile(string filename);
        SequenceScript LoadYamlFile(string filename);
    }
}