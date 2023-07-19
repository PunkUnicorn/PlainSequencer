using PlainSequencer.Options;
using PlainSequencer.Scriban;
using PlainSequencer.Stuff;
using System.Collections.Generic;

namespace PlainSequencer.Script
{
    public class SequenceScript
	{
		//public KeyVlueList header { get; set; }
		public int? client_timeout_seconds { get; set; }
		public string run_id { get; set; }
		public List<SequenceItem> sequence_items { get; set; }
	}

	//[Flags]
	//public enum LoggerOutput
	//{
	//	Console,
	//	NLog,
	//	All = Console | NLog,
	//}

	//public class Logger
	//{
	//	/// <summary>
	//	/// NLog, Console, All
	//	/// </summary>
	//	public LoggerOutput type { get; set; }
	//	public string on_start_sequence { get; set; }
	//	public string on_error { get; set; }
	//	public string on_success { get; set; }
	//}


	public class SequenceItem
	{
		public string name { get; set; }


		///// <summary>
		///// A scriban template for the breadcrumb base
		///// </summary>
		//public string start_breadcrumb { get; set; } = "{{run_id}} - {{sequence_item.command}} - Started";
		//public string breadcrumb { get; set; } = "{{run_id}} - {{sequence_item.command}}";
		//public string fail_breadcrumb { get; set; } = "{{run_id}} - {{sequence_item.command}} - Failed";
		//public string success_breadcrumb { get; set; } = "{{run_id}} - {{sequence_item.command}} - Success";


		/// <summary>
		/// Number of retrys to do instantly after the fail
		/// </summary>
		public int? max_retries;

		/// <summary>
		/// slow-backoff
		/// fast-backoff
		/// auto/null (slow backoff if 5 retries or less, else fast retry)
		/// </summary>
		public string retry_type;
        		
		/// <summary>
		/// Set to true to treat the model as an array, where each array item fans out
		/// Set to false to treat the model as a single object
		/// </summary>
		public bool is_model_array { get; set; }
		public int? take_only_n { get; set; }
		public bool is_continue_on_failure { get; set; }


		/* Mutually exclusive group start */
		/* 𝘐𝘧 𝘮𝘰𝘳𝘦 𝘵𝘩𝘢𝘯 𝘰𝘯𝘦 𝘰𝘧 𝘵𝘩𝘦𝘴𝘦 𝘰𝘣𝘫𝘦𝘤𝘵𝘴 𝘪𝘴 𝘱𝘰𝘱𝘶𝘭𝘢𝘵𝘦𝘥 𝘵𝘩𝘦𝘯 𝘵𝘩𝘦 𝘰𝘳𝘥𝘦𝘳 𝘰𝘧 𝘱𝘳𝘦𝘤𝘦𝘥𝘦𝘯𝘤𝘦 𝘪𝘴 𝘧𝘪𝘳𝘴𝘵 𝘧𝘳𝘰𝘮 𝘵𝘰𝘱 𝘵𝘰 𝘣𝘰𝘵𝘵𝘰𝘮 𝘰𝘧 𝘵𝘩𝘦 𝘰𝘳𝘥𝘦𝘳 𝘥𝘦𝘧𝘪𝘯𝘦𝘥 𝘩𝘦𝘳𝘦 */
		public Http http { get; set; }
		public Run run { get; set; }
		public Load load { get; set; }
		public Check check { get; set; }
		public Transform transform { get; set; }
		public Fork fork { get; set; }
		/* Mutually exclusive group end */


		/// <summary>
		/// Scriban parsed name and value to add new variables after this step.
		/// Exsisting variables of the same name are overwritten.
		/// </summary>
		public NamedStringList new_variables { get; set;}
	}

	public class Transform
    {
        /// <summary>
        /// A scriban template to allow transformations of the model to a new format
        /// </summary>
        public string new_model_template { get; set; }
		//public string OldValue(dynamic model) => model;
		//public string NewValue(dynamic model) => string.IsNullOrWhiteSpace(new_model_template)
		//	? ""
		//	: ScribanUtil.ScribanParse(new_model_template, model);

		// add OldValue and NewValue to the model
		//public string breadcrumb { get; set; } = "{{sequence_item.transform.new_model_template}}";
	}

	// TODO: model stays as it was as it comes out the other side. Meanwhile the cloned model is passed into a new process
	public class Fork
    {
		public List<SequenceItem> sequence_items { get; set; }
		/// <summary>
		/// Yaml or json filenames passed into <see cref="command_line_options"/> overrides anything passed into <seealso cref="sequence_items"/>
		/// </summary>
		public CommandLineOptions command_line_options { get; set; }
	}

    public class Check
    {
		public string pass_template { get; set; }
		public bool IsPass(object model)
        {
			var result = bool.Parse(ScribanUtil.ScribanParse(pass_template, model));
			return result;
		}

		// combine with sequence_item.breadcrumb, which is its breadcrumb base
		// add IsPass and FailMessage to the template model
		public string breadcrumb { get; set; } = "{{sequence_item.check.pass_template}}{{if sequence_item.check.IsPass}}Pass{{else}}{sequence_item.check.FailMessage}{{end}}";
		public string fail_info_template { get; set; }
		public string FailMessage(object model) => string.IsNullOrWhiteSpace(fail_info_template)
			? ""
			: ScribanUtil.ScribanParse(fail_info_template, model);
	}

    public class Save
	{
		public string folder { get; set; }
		public string model_before_filename { get; set; }
		public string model_after_filename { get; set; }
		public string breadcrumb { get; set; } = "{{sequence_item.save.response_filename}}";
	}

	public class Run
	{
		public string exec { get; set; }
		public string args { get; set; }
		public bool use_shell_execute { get; set; }
		public string breadcrumb { get; set; } = "{{sequence_item.run.exec}}";

		public bool is_ignore_exitcode { get; set; }
		public Save save { get; set; }
	}

	public class Load
    {
		/// <summary>
		/// Csv filename to load
		/// </summary>
		public string csv { get; set; }
		public string breadcrumb { get; set; } = "{{sequence_item.load.csv}}";
	}

	public class HttpSave : Save
	{
        public string response_info_filename;

        public string request_content_filename { get; set; }
		public bool request_content_is_binary { get; set; }
		public string response_content_filename { get; set; }
		public bool response_content_is_binary { get; set; }
	}

	public class Http
	{
		public string breadcrumb { get; set; } = "{{sequence_item.send.url}}";
		public string method { get; set; }
		public string base_url { get; set; }
		public string url { get; set; }
		public string content_type { get; set; }
		public NamedStringList query { get; set; }
		public NamedStringList header { get; set; }
		public string body { get; set; }
		public HttpSave save { get; set; }

	}

	//public class NextSequenceItemOptions
	//{
	//	/// <summary>
	//	/// Set this to the max number of items to pass to the next command
	//	/// </summary>
	//	public int? stop_after_nth_item { get; set; }
	//	public bool abort_on_exception { get; set; }
	//	public string command { get; set; }
	//	public dynamic replace_response_with { get; set; }
	//	public int? parallel_batch_size { get; set; }
	//}
}
