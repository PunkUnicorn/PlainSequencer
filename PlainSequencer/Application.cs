using Newtonsoft.Json;
using PlainSequencer.Options;
using PlainSequencer.Script;
using PlainSequencer.SequenceItemActions;
using PlainSequencer.SequenceItemSupport;
using PlainSequencer.SequenceScriptLoader;
using PlainSequencer.Stuff;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PlainSequencer
{

    public class Application : IApplication, ISequenceSession
    {
        private readonly ICommandLineOptions commandLineOptions;
        private readonly ILoadScript scriptLoader;
        private readonly ISequenceItemActionBuilderFactory itemActionBuilderFactory;
        private readonly IHttpClientProvider httpClientProvider;
        private SequenceScript script;

        public Application(ICommandLineOptions commandLineOptions, ILoadScript scriptLoader, ISequenceItemActionBuilderFactory itemActionBuilderFactory, IHttpClientProvider httpClientProvider)
        {
            this.commandLineOptions = commandLineOptions;
            this.scriptLoader = scriptLoader;
            this.itemActionBuilderFactory = itemActionBuilderFactory;
            this.httpClientProvider = httpClientProvider;
        }

        private int uniqueNo = 0;

        public string RunId { get; set; }
        public ISequenceItemActionHierarchy Top { get; set; }

        public int UniqueNo => ++uniqueNo;

        public SequenceScript Script { get; set; }

        public IHttpClientProvider HttpClientProvider => httpClientProvider;

        public async Task<bool> RunAsync(object startModel)
        {
            this.script = LoadScript();

            if (script == null)
                return false;

            var cancellationToken = new CancellationToken();
            var firstSequenceItem = script.sequence_items.FirstOrDefault();
            var nextSequenceItems = script.sequence_items.Skip(1).ToArray();

            Script = this.script;
            RunId = script.run_id;
            var runItem = itemActionBuilderFactory.Fetch(null, startModel, firstSequenceItem, nextSequenceItems);
            Top = (ISequenceItemActionHierarchy)runItem;

            var model = await runItem.ActionAsync(cancellationToken);
            var result = (ISequenceItemResult)runItem;

            var isContinue = result.IsFail
                ? Top.SequenceItem.is_continue_on_failure
                : true;

            if (isContinue)
                Console.WriteLine(JsonConvert.SerializeObject(model, Formatting.Indented));

            return !result.IsFail;
        }

        public static object TurnStringResponseIntoModel(string content)
        {
            // Note: instead of nested try try, could use switch statement pattern matching to solve this problem
            //       deserialising to an object then a switch pattern match from that
            object responseModel = null;
            bool resolved = false;
            try
            {
                try
                {
                    responseModel = JsonConvert.DeserializeObject<List<IDictionary<string, object>>>(content);
                    resolved = true;
                }
                catch { }

                if (!resolved) try
                    {
                        responseModel = JsonConvert.DeserializeObject<List<object>>(content);
                        resolved = true;
                    }
                    catch { }

                if (!resolved)
                    responseModel = JsonConvert.DeserializeObject<object>(content);
            }
            catch
            {
                responseModel = content;
            }
            return responseModel;
        }

        private SequenceScript LoadScript()
        {
            SequenceScript script = null;

            if (commandLineOptions.IsStdIn)
                throw new NotImplementedException();
            else if (!string.IsNullOrWhiteSpace(commandLineOptions.YamlFile))
                script = scriptLoader.LoadYamlFile(commandLineOptions.YamlFile);
            else if (!string.IsNullOrWhiteSpace(commandLineOptions.JsonFile))
                script = scriptLoader.LoadJsonFile(commandLineOptions.JsonFile);
            else if (commandLineOptions.Direct != null)
                script = commandLineOptions.Direct;

            return script;
        }
    }
}
