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
		public SequenceItemLoad(ILogSequence logProgress, ISequenceSession session, ICommandLineOptions commandLineOptions, ISequenceItemActionBuilderFactory itemActionBuilderFactory, SequenceItemCreateParams @params)
            : base(logProgress, session, commandLineOptions, itemActionBuilderFactory, @params) { }

        public IEnumerable<string> Compile(SequenceItem sequenceItem)
		{
			return new string[] { };
		}

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        protected override async Task<object> ActionAsyncInternal(CancellationToken cancelToken)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
			return await FailableRun<object>(logProgress, this, async delegate {
				++this.ActionExecuteCount;

				if (this.sequenceItem.load == null)
					throw new NullReferenceException($"{nameof(this.sequenceItem)}.{nameof(this.sequenceItem.load)} missing");

				this.logProgress?.Progress(this, $"Loading {this.sequenceItem.load.csv}...", SequenceProgressLogLevel.Brief);

                var scribanModel = MakeScribanModel();

                var loadfile = ScribanUtil.ScribanParse(this.sequenceItem?.load?.csv ?? "", scribanModel);

				var csvRows = LoadCsvFile(this.sequenceItem.load, loadfile);

				var noOfRows = csvRows?.Count() ?? 0;

                var stringContent = JsonConvert.SerializeObject(csvRows);
                LiteralResponse = stringContent;

                var responseModel = SequenceItemStatic.GetResponseItems(this.sequenceItem, stringContent);// csvRows.ToList<dynamic>());
                ActionResult = responseModel;
				return ActionResult;
			});
		}

		private List<IDictionary<string, object>> LoadCsvFile(Load load, string filename)
		{
            var readData = File.ReadAllText(filename);
            this.logProgress?.DataInProgress(this, $"Loaded {readData.Length} bytes from {filename}...", SequenceProgressLogLevel.Brief);

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