using Newtonsoft.Json;
using PlainSequencer.Logging;
using PlainSequencer.Options;
using PlainSequencer.Scriban;
using PlainSequencer.Script;
using PlainSequencer.SequenceItemSupport;
using Polly;
using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static PlainSequencer.SequenceItemActions.SequenceItemStatic;

namespace PlainSequencer.SequenceItemActions
{
    public class SequenceItemRunException : Exception
	{
		public SequenceItemRunException(string message) : base(message) { }
		public SequenceItemRunException(string message, Exception innerException) : base(message, innerException) { }
	}

	public class SequenceItemRun : SequenceItemAbstract, ISequenceItemAction, ISequenceItemActionRun, ISequenceItemActionHierarchy
	{
		public SequenceItemRun(ILogSequence logProgress, ISequenceSession session, ICommandLineOptions commandLineOptions, ISequenceItemActionBuilderFactory itemActionBuilderFactory, SequenceItemCreateParams @params)
			: base(logProgress, session, commandLineOptions, itemActionBuilderFactory, @params) { }

		//public IEnumerable<string> Compile(SequenceItem sequenceItem)
		//{
		//	return new string[] { };
		//}

		protected override async Task<object> ActionAsyncInternal(CancellationToken cancelToken)
        {
			return await FailableRun<object>(logProgress, this, async delegate 
			{
				var w = Policy.Handle<Exception>()
					.WaitAndRetryAsync(this.SequenceItem.max_retries ?? 0, (i) => TimeSpan.FromSeconds(1));

				if (this.sequenceItem.run == null)
					throw new NullReferenceException($"{nameof(this.sequenceItem)}.{nameof(this.sequenceItem.run)} missing");

				var scribanModel = MakeScribanModel();

				// Exec run external program
				var workingExec = ScribanUtil.ScribanParse(this.sequenceItem?.run?.exec ?? "", scribanModel);
				var workingArgs = ScribanUtil.ScribanParse(this.sequenceItem?.run?.args ?? "", scribanModel);

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
				return await w.ExecuteAsync(async () =>
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
				{
					++this.ActionExecuteCount;

					this.logProgress?.Progress(this, $"Running exec '{workingExec}', with args '{workingArgs}'...", SequenceProgressLogLevel.Brief);

					var itsStandardInput = (model != null)
						? JsonConvert.SerializeObject(model)
						: "";

					var execReturn = ProcessExecute(this.sequenceItem.run, workingExec, workingArgs, itsStandardInput);
					var responseContentLength = execReturn?.Length ?? 0;
					var responseContent = execReturn;
					LiteralResponse = execReturn;

					var responseModel = SequenceItemStatic.GetResponseItems(this.logProgress, this, execReturn);
					ActionResult = responseModel;

					await DoInlineSaveAsync(ActionResult, scribanModel, sequenceItem.run.save, sequenceItem.run.saves);

					return ActionResult;
				});
			});
		}

		/// <summary>
		/// Execute an external program. It gets the model through stdin, and gives its output through stdout, and errors through stderr
		/// </summary>
		/// <param name="run">The yaml run: block</param>
		/// <param name="workingExec">Name of the executable to run</param>
		/// <param name="workingArgs">The executables command arguments</param>
		/// <param name="inputmodel">Data to pass through to the running program as its stdin</param>
		/// <returns>The contents of the executables stdout</returns>
		private string ProcessExecute(Run run, string workingExec, string workingArgs, string inputmodel)
		{
			var outputBuilder = new StringBuilder();
			var errorOutputBuilder = new StringBuilder();

			var processStartInfo = new ProcessStartInfo();
			processStartInfo.CreateNoWindow = true;
			processStartInfo.RedirectStandardOutput = true;
			processStartInfo.RedirectStandardInput = true;
			processStartInfo.RedirectStandardError = true;

			processStartInfo.UseShellExecute = run.use_shell_execute;
			processStartInfo.Arguments = workingArgs;
			processStartInfo.FileName = workingExec;

			var process = new Process();
			process.StartInfo = processStartInfo;
			process.EnableRaisingEvents = true;
			process.OutputDataReceived += new DataReceivedEventHandler
			(
				delegate (object sender, DataReceivedEventArgs e) { outputBuilder.Append(e.Data); }
			);
			process.ErrorDataReceived += new DataReceivedEventHandler
			(
				delegate (object sender, DataReceivedEventArgs e) { errorOutputBuilder.Append(e.Data); }
			);

			process.Start();

			using (var myStreamWriter = process.StandardInput)
			{
				// Start the process

				process.BeginOutputReadLine();
				process.BeginErrorReadLine();

				myStreamWriter.Write(inputmodel);
				myStreamWriter.Flush();
			}

			process.WaitForExit();
			process.CancelOutputRead();
			process.CancelErrorRead();

			var exitCode = process.ExitCode;

			// Use the output, and propagate errors
			string output = outputBuilder.ToString();
			if (errorOutputBuilder.Length > 0)
			{
				var errorDetail = errorOutputBuilder.ToString();
				throw new SequenceItemRunException(errorDetail);
			}

			if (!sequenceItem.run.is_ignore_exitcode && exitCode != 0)
			{
				var errorMsg = $"Failed due to '{workingExec}' returning non-zero exitcode: {exitCode}";
				throw new SequenceItemRunException(errorMsg);
			}
			return output;
		}
	}
}