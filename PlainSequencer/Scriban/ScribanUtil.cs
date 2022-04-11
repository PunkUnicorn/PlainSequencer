using Newtonsoft.Json;
using Scriban;
using Scriban.Runtime;
using System;

namespace PlainSequencer.Scriban
{
    public static class ScribanUtil
    {
		private static string ScribanCleanName(dynamic m) { return m.Name; }
		public static string ScribanParse(string template, object model, MemberRenamerDelegate f = null)
		{
			try
			{
				return ScribanRawParse(template, model, f);
			}
			catch (Exception e)
			{
				var modelStr = "unserializable";
				try
				{
					modelStr = JsonConvert.SerializeObject(model, Formatting.Indented);
				}
				catch (Exception) {/*eat*/}

				var msg = $"Template:\n{template}\nModel:\n{modelStr}\n\n{e.Message}";
				throw new InvalidOperationException(msg, e.InnerException);

			};
		}

		private static string ScribanRawParse(string template, object model, MemberRenamerDelegate f = null) => Template.Parse(template).Render(model, f ?? ScribanCleanName);

		private static LogMessageBag ScribanErrors(string template, object model, MemberRenamerDelegate f = null) => Template.Parse(template).Messages;

	}
}
