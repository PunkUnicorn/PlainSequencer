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
			return await FailableRun(logProgress, this, async delegate
            {
                ++this.ActionExecuteCount;

                if (this.sequenceItem.load == null)
                    throw new NullReferenceException($"{nameof(this.sequenceItem)}.{nameof(this.sequenceItem.load)} missing");

                var scribanModel = MakeScribanModel();

                string stringContent = null;
                if (!string.IsNullOrWhiteSpace(this.sequenceItem.load.csv))
                    stringContent = LoadCsv(scribanModel);
                else if (!string.IsNullOrWhiteSpace(this.sequenceItem.load.json))
                    stringContent = LoadJson(scribanModel);
                else
                    throw new InvalidOperationException("Neither load sections (csv, json) are populated.");

                LiteralResponse = stringContent;

                var responseModel = SequenceItemStatic.GetResponseItems(this.logProgress, this, stringContent);// csvRows.ToList<dynamic>());
                ActionResult = responseModel;
                return ActionResult;
            });
		}

        private string LoadJson(object scribanModel)
        {
            this.logProgress?.Progress(this, $"Loading json {this.sequenceItem.load.json}...", SequenceProgressLogLevel.Brief);

            var loadfile = ScribanUtil.ScribanParse(this.sequenceItem?.load?.json ?? "", scribanModel);

            var retval = File.ReadAllText(loadfile);
            
            this.logProgress?.DataInProgress(this, $"Loaded json {retval.Length} bytes from {loadfile}...", SequenceProgressLogLevel.Brief);
            
            return retval;
        }

        private string LoadCsv(object scribanModel)
        {
            this.logProgress?.Progress(this, $"Loading {this.sequenceItem.load.csv}...", SequenceProgressLogLevel.Brief);

            var loadfile = ScribanUtil.ScribanParse(this.sequenceItem?.load?.csv ?? "", scribanModel);

            var csvRows = LoadCsvFile(this.sequenceItem.load, loadfile);

            var stringContent = JsonConvert.SerializeObject(csvRows);
            return stringContent;
        }

        private List<IDictionary<string, object>> LoadCsvFile(Load load, string filename)
		{
            var readData = File.ReadAllText(filename);
            this.logProgress?.DataInProgress(this, $"Loaded csv {readData.Length} bytes from {filename}...", SequenceProgressLogLevel.Brief);

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
            this.logProgress?.DataInProgress(this, $" found {csvRows.Count} rows with {headerCount} columns...", SequenceProgressLogLevel.Brief);
            return csvRows;
        }
    }
}