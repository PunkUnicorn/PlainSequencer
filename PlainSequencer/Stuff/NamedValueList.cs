using System.Collections.Generic;

namespace PlainSequencer
{
    public class NamedValueList : List<KeyValuePair<string /*name*/, object /*value*/>>
    {
        public NamedValueList() { }

        public NamedValueList(List<KeyValuePair<string, object>> toAdd) { this.AddRange(toAdd); }
    }
}
