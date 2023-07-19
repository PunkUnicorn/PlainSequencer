using System.Collections.Generic;

namespace PlainSequencer.Stuff
{
	/// <summary>
	/// public class NamedStringList : List<KeyValuePair<string /*name*/, string /*value*/>> ...
	/// </summary>
	public class NamedStringList : List<KeyValuePair<string /*name*/, string /*value*/>> 
	{
		public NamedStringList() { }

		public NamedStringList(List<KeyValuePair<string, string>> toAdd) { this.AddRange(toAdd); }
	}
}
