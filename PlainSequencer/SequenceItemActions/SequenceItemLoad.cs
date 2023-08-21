using CsvHelper;
using Newtonsoft.Json;
using PlainSequencer.Logging;
using PlainSequencer.Options;
using PlainSequencer.Scriban;
using PlainSequencer.Script;
using PlainSequencer.SequenceItemSupport;
using Polly;
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

        protected override async Task<object> ActionAsyncInternal(CancellationToken cancelToken)
        {
            return await FailableRun(logProgress, this, async delegate
            {
                if (this.sequenceItem.load == null)
                    throw new NullReferenceException($"{nameof(this.sequenceItem)}.{nameof(this.sequenceItem.load)} missing");

                var w = Policy.Handle<Exception>()
                    .WaitAndRetryAsync(this.SequenceItem.max_retries ?? 0, (i) => TimeSpan.FromSeconds(1));

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
                return await w.ExecuteAsync(async () =>
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
                {
                    ++this.ActionExecuteCount;

                    var scribanModel = MakeScribanModel();

                    string defaultVariableName = null;
                    string stringContent = null;
                    if (!string.IsNullOrWhiteSpace(this.sequenceItem.load.csv))
                    {
                        stringContent = LoadCsv(scribanModel);
                        defaultVariableName = "csv";
                    }
                    else if (!string.IsNullOrWhiteSpace(this.sequenceItem.load.json))
                    {
                        stringContent = LoadJson(scribanModel);
                        defaultVariableName = "json";
                    }
                    else
                        throw new InvalidOperationException("Neither load sections (csv, json) are populated.");

                    LiteralResponse = stringContent;

                    dynamic passData = SequenceItemStatic.GetResponseItems(this.logProgress, this, stringContent);

                    ActionResult = this.model;

                    await DoInlineSaveAsync(ActionResult, scribanModel, sequenceItem.load.save, sequenceItem.load.saves);

                    NewVariables.Add(this.sequenceItem.load.variable_name ?? defaultVariableName, passData);

                    return ActionResult;
                });
            });
		}

        private string LoadJson(object scribanModel)
        {
            var loadfile = ScribanUtil.ScribanParse(this.sequenceItem?.load?.json ?? "", scribanModel);

            var retval = File.ReadAllText(loadfile);
            
            this.logProgress?.DataInProgress(this, $"Loaded {retval.Length} bytes from json '{loadfile}'...", SequenceProgressLogLevel.Brief);
            
            return retval;
        }

        private string LoadCsv(object scribanModel)
        {
            var loadfile = ScribanUtil.ScribanParse(this.sequenceItem?.load?.csv ?? "", scribanModel);

            var typed = LoadCsvFile(this.sequenceItem.load, loadfile);

            var stringContent = JsonConvert.SerializeObject(typed);
            return stringContent;
        }

        private List<IDictionary<string, object>> LoadCsvFile(Load load, string filename)
		{
            var readData = File.ReadAllText(filename);
            this.logProgress?.DataInProgress(this, $"Loaded {readData.Length} bytes from csv '{filename}'...", SequenceProgressLogLevel.Brief);

            List<IDictionary<string, object>> csvRows = null;

            int headerCount = 0;
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

                if (headerCount == 0 && csvRows.Count > 0)
                    headerCount = csvRows.First().Keys.Count;

            }

            var commonDetail = $" found {csvRows.Count} rows with {headerCount} columns...";
            string moreDetail = csvRows.Count > 0
                ? $" {string.Join(", ", csvRows.First().Keys)} x {csvRows.Count} rows..."
                : null;

            this.logProgress?.DataInProgress(this, string.Join("\n", new[] { commonDetail, moreDetail }.Where(w=>w is not null)), SequenceProgressLogLevel.Diagnostic);

            return csvRows;
        }
    }
}