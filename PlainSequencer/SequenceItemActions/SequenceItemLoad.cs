using CsvHelper;
using Newtonsoft.Json;
using PlainSequencer.Logging;
using PlainSequencer.Options;
using PlainSequencer.Scriban;
using PlainSequencer.Script;
using PlainSequencer.SequenceItemSupport;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static PlainSequencer.SequenceItemActions.SequenceItemStatic;

namespace PlainSequencer.SequenceItemActions
{
    public class SequenceItemLoad : SequenceItemAbstract, ISequenceItemAction, ISequenceItemActionRun, ISequenceItemActionHierarchy
    {
		public SequenceItemLoad(IProgressLogger logProgress, ISequenceSession session, ICommandLineOptions commandLineOptions, ISequenceItemActionBuilderFactory itemActionBuilderFactory, SequenceItemCreateParams @params)
            : base(logProgress, session, commandLineOptions, itemActionBuilderFactory, @params) { }

        public IEnumerable<string> Compile(SequenceItem sequenceItem)
		{
			return new string[] { };
		}

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        protected override async Task<object> ActionAsyncInternal(CancellationToken cancelToken)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
			return await FailableRun<object>(this, async delegate {
				++this.ActionExecuteCount;

				if (this.sequenceItem.load == null)
					throw new NullReferenceException($"{nameof(this.sequenceItem)}.{nameof(this.sequenceItem.load)} missing");


				this.logProgress?.Progress(this, $"Loading {this.sequenceItem.load.csv}...");

				//var scribanModel = new { now = $"{DateTime.Now.ToShortDateString()} {DateTime.Now.ToShortTimeString()}", run_id = this.session.RunId, command_args = this.commandLineOptions, this.model, sequence_item = this.sequenceItem, unique_no = this.session.UniqueNo};
                var scribanModel = MakeScribanModel();

                var loadfile = ScribanUtil.ScribanParse(this.sequenceItem?.load?.csv ?? "", scribanModel);

				var csvRows = LoadFile(this.sequenceItem.load, loadfile);

				var noOfRows = csvRows?.Count() ?? 0;

                var stringContent = JsonConvert.SerializeObject(csvRows);
                LiteralResponse = stringContent;

                var responseModel = SequenceItemStatic.GetResponseItems(this.sequenceItem, stringContent);// csvRows.ToList<dynamic>());
                ActionResult = responseModel;
				return ActionResult;
			});
		}

		private List<IDictionary<string, object>> LoadFile(Load load, string filename)
		{
            var readData = File.ReadAllText(filename);

            List<IDictionary<string, object>> csvRows = null;

            using (var reader = new StringReader(readData))
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                var records = csv.Read();
                var header = csv.ReadHeader();
                while (records = csv.Read())
                {
                    dynamic stuff = csv.GetRecord<dynamic>();
                    if (csvRows == null)
                        csvRows = new List<IDictionary<string, object>>();

                    csvRows.Add((stuff as IDictionary<string, object>).ToDictionary(
                        k => k.Key,
                        v => v.Value));
                }
            }
            return csvRows;
        }
    }
}