﻿using PlainSequencer.Scriban;
using PlainSequencer.Stuff;
using System.Collections.Generic;

namespace PlainSequencer.Script
{
    public class SequenceScript
	{
		public string name;

		/// <summary>
		/// False to return nothing on a failure, True to get all available results despite failures
		/// </summary>
        public bool output_after_failure;

        //public KeyVlueList header { get; set; }
        public int? client_timeout_seconds { get; set; }
		public string working_directory { get; set; }

		public class FailHole
		{
			public bool disable_stderr { get; set; }
			public string stderr_template { get; set; } = @"expandable+ {{sequence_item_node.Name}} {{if sequence_item.is_model_array;}}FanOut#{{sequence_item_node.PeerIndex; end}}
{{sequence_item_run.SequenceDiagramNotation}}
end";
			public bool fail_to_sequence { get; set; }
			public List<SequenceItem> sequence { get; set; }
        }
		public FailHole fail_hole { get; set; }
		public string run_id { get; set; }
		public List<SequenceItem> sequence { get; set; }
	}

	/*
		Scriban substitute strings have access to this model:
            now = $"{DateTime.Now.ToShortDateString()} {DateTime.Now.ToShortTimeString()}";
            run_id = guid string
            command_args 
				.IsStdIn
				.JsonFile
				//.Variables
				.YamlFile
				.Direct
            model = model from the previous step
            sequence_item = this yaml sequence item
            peerIndex = fan out index to separate many instances of the same sequence item
            prev_sequence_item = sequence item before us
            next_sequence_items = array of sequence items in front of us
            unique_no = unique number
	*/


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
		public Save save { get; set; }
		//public Fork fork { get; set; }
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
		/// Set to true for the result to be plain text, otherwise it will be deserialised into a data object
		/// </summary>
		public bool new_model_is_plain_text { get; set; }
		/// <summary>
		/// A scriban template to allow transformations of the model to a new format
		/// </summary>
		public string new_model_template { get; set; }
		public Save save { get; set; }
		public List<Save> saves { get; set; }
	}

	//// TODO: model stays as it was as it comes out the other side. Meanwhile the cloned model is passed into a new process
	//public class Fork
 //   {
	//	public List<SequenceItem> sequence_items { get; set; }
	//	/// <summary>
	//	/// Yaml or json filenames passed into <see cref="command_line_options"/> overrides anything passed into <seealso cref="sequence_items"/>
	//	/// </summary>
	//	public CommandLineOptions command_line_options { get; set; }
	//}

	public class Save
	{
		public string working_directory { get; set; }
		public string filename { get; set; }
		public string content_template { get; set; } = "{{model}}";
		public bool is_content_binary { get; set; }
		public string breadcrumb { get; set; } = "{{sequence_item.save.filename}}";
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
		public Save save { get; set; }
		public List<Save> saves { get; set; }
	}

	public class Run
	{
		public string exec { get; set; }
		public string args { get; set; }
		public bool use_shell_execute { get; set; }
		public string breadcrumb { get; set; } = "{{sequence_item.run.exec}}";
		public bool is_ignore_exitcode { get; set; }
		public Save save { get; set; }
		public List<Save> saves { get; set; }
	}

	public class Load
    {
		/// <summary>
		/// if null or blank then the default variable_name is either "csv" or "json". Existing variable with the same name are overwritten by newer ones.
		/// </summary>
		public string variable_name;

        /* Mutually exclusive block */
        /* 𝘐𝘧 𝘮𝘰𝘳𝘦 𝘵𝘩𝘢𝘯 𝘰𝘯𝘦 𝘰𝘧 𝘵𝘩𝘦𝘴𝘦 𝘰𝘣𝘫𝘦𝘤𝘵𝘴 𝘪𝘴 𝘱𝘰𝘱𝘶𝘭𝘢𝘵𝘦𝘥 𝘵𝘩𝘦𝘯 𝘵𝘩𝘦 𝘰𝘳𝘥𝘦𝘳 𝘰𝘧 𝘱𝘳𝘦𝘤𝘦𝘥𝘦𝘯𝘤𝘦 𝘪𝘴 𝘧𝘪𝘳𝘴𝘵 𝘧𝘳𝘰𝘮 𝘵𝘰𝘱 𝘵𝘰 𝘣𝘰𝘵𝘵𝘰𝘮 𝘰𝘧 𝘵𝘩𝘦 𝘰𝘳𝘥𝘦𝘳 𝘥𝘦𝘧𝘪𝘯𝘦𝘥 𝘩𝘦𝘳𝘦 */
        /// <summary>
        /// Csv filename to load
        /// </summary>
        public string csv { get; set; }
		/// <summary>
		/// JSON file to load
		/// </summary>
		public string json { get; set; }
		public string text { get; set; }
		public string binary { get; set; }
		/* End of mutually exclusive block */

		public string breadcrumb { get; set; } = "{{sequence_item.load.csv}}{{sequence_item.load.json}}";
		public Save save { get; set; }
		public List<Save> saves { get; set; }
	}

	//public class HttpSave : Save
	//{
	//	public Save request { get; set; }
	//	public Save response { get; set; }


	//	//public string response_info_filename;

	// //      public string request_content_filename { get; set; }
	//	//public bool request_content_is_binary { get; set; }
	//	//public string response_content_filename { get; set; }
	//	//public bool response_content_is_binary { get; set; }
	//}

	public class Http
	{
		public string breadcrumb { get; set; } = "{{sequence_item.send.url}}";
		public string method { get; set; }
		//public string base_url { get; set; }
		public string url { get; set; }
		public string content_type { get; set; }
		public NamedStringList query { get; set; }
		public NamedStringList header { get; set; }
		public string body { get; set; }
		// save is processed then also so are saves if any or all are populated.
		// Scriban model contains additional:
		//   request # - A dotnet HttpRequestMessage object
		//	 request_content_as_text # - The request body as text
		//   request_content_as_binary # - The request body as binary
		public Save save { get; set; }
		public List<Save> saves { get; set; }
	}
}
