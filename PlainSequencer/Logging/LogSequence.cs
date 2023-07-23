using Newtonsoft.Json;
using PlainSequencer.SequenceItemActions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PlainSequencer.Logging
{
    public class LogSequence : ILogSequence
    {
        private string CleanName(string pname) => pname?.Trim()?.Replace("-", " ");

        public class SequenceArrow
        {
            public List<string> NotesFollowing { get; set; } = new List<string>();
            public string From { get; set; }
            public string ArrowNotation { get; set; } = "->";
            public string To { get; set; }
            public string Description { get; set; }
            public int JsonBytes { get; set; }
        }

        private Dictionary<int, SequenceArrow> sequenceArrows = new Dictionary<int, SequenceArrow>();

        public void Fail(SequenceItemAbstract item, string message)
        {
            var uniqueItemKey = item.FullAncestryWithPeerName.GetHashCode();
            sequenceArrows[uniqueItemKey].ArrowNotation = "-x";
            sequenceArrows[uniqueItemKey].NotesFollowing.Add($"<{SequenceProgressLogLevel.Brief}>Error: {message}");

            Console.WriteLine(message);
        }

        public void Fail(SequenceItemAbstract item, Exception e)
        {
            var uniqueItemKey = item.FullAncestryWithPeerName.GetHashCode();
            sequenceArrows[uniqueItemKey].ArrowNotation = "-x";
            sequenceArrows[uniqueItemKey].NotesFollowing.Add($"<{SequenceProgressLogLevel.Brief}>Error: {e.Message}");

            Console.WriteLine(e.Message);
        }

        public void Progress(SequenceItemAbstract item, string message, SequenceProgressLogLevel level)
        {
            var uniqueItemKey = item.FullAncestryWithPeerName.GetHashCode();
            sequenceArrows[uniqueItemKey].NotesFollowing.Add($"<{level}>{message}");

            Console.WriteLine(message);
        }

        public void DataOutProgress(SequenceItemAbstract item, string message, SequenceProgressLogLevel level)
        {
            var uniqueItemKey = item.FullAncestryWithPeerName.GetHashCode();
            sequenceArrows[uniqueItemKey].NotesFollowing.Add($"<{level}>{message}");

            Console.WriteLine(message);
        }

        public void DataInProgress(SequenceItemAbstract item, string message, SequenceProgressLogLevel level)
        {
            var uniqueItemKey = item.FullAncestryWithPeerName.GetHashCode();
            sequenceArrows[uniqueItemKey].NotesFollowing.Add($"<{level}>{message}");

            Console.WriteLine(message);
        }

        public void FinishedItem(SequenceItemAbstract item)
        {
        }

        public void StartItem(SequenceItemAbstract item)
        {
            Func<ISequenceItemActionHierarchy, ISequenceItemActionRun> castToIRun = (a) => (ISequenceItemActionRun)a;
            var sa = new SequenceArrow
            {
                From = CleanName(castToIRun(item.Parent)?.SequenceDiagramKey),
                To = CleanName(castToIRun(item).SequenceDiagramKey),
                Description = CleanModel(item.Model),
                JsonBytes = JsonConvert.SerializeObject(item.Model).Length
            };

            sequenceArrows.Add(item.FullAncestryWithPeerName.GetHashCode(), sa);
            
            if (item.SequenceItem.is_model_array)
                sa.NotesFollowing.Add($"<{SequenceProgressLogLevel.Diagnostic}>//Fan out {item.PeerIndex}//");

        }

        private static string CleanModel(object model)
        {
            Func<string, string> cleanFunc = (string s) => s.Replace("\n", "\\n")?.Replace("\r", "");

            if (model is null)
                return "null";

            if (model is string)
                return cleanFunc(model.ToString());

            return cleanFunc(JsonConvert.SerializeObject(model, Formatting.Indented));
        }

        public void SequenceComplete(bool isSuccess, object model)
        {
            var allColumns = sequenceArrows.Values
                .Select(item => item.From)
                .Union(sequenceArrows.Values.Select(item => item.To));

            var noFrom = sequenceArrows.Values
                .Select(item => item.To)
                .Except(sequenceArrows.Values.Select(item => item.From))
                .SingleOrDefault();

            if (noFrom is null)
                return;

            var noLineFromKeys = sequenceArrows
                .Where(item => noFrom.Contains(item.Value.To))
                .Select(item => item.Key)
                .ToArray();

            var newArrow = new SequenceArrow
            {
                To = "Result",
                From = noFrom,
                Description = CleanModel(model),
                JsonBytes = JsonConvert.SerializeObject(model).Length
            };
            newArrow.ArrowNotation = isSuccess ? newArrow.ArrowNotation : "-x";
            var guidHashcode = Guid.NewGuid().GetHashCode();
            sequenceArrows.Add(guidHashcode, newArrow);
        }

        public string GetSequenceDiagramNotation(string title, SequenceProgressLogLevel level = SequenceProgressLogLevel.Diagnostic)
        {
            var retval = new StringBuilder();

            if (title is not null)
                retval.AppendLine($"title {title}\n");

            var participantsOnly = sequenceArrows
                .Where(item => string.IsNullOrWhiteSpace(item.Value.To) || string.IsNullOrWhiteSpace(item.Value.From));

            Func<string, bool> LevelOK = (s) => level == SequenceProgressLogLevel.Diagnostic || s.StartsWith($"<{level}>");
            Func<string, int, string> StripLevel = (s, i) => s.Substring(s.IndexOf('>')+1);
            Func<string, int, SequenceProgressLogLevel, string> GetDesc = (s, b, l) => l == SequenceProgressLogLevel.Diagnostic ? s : $"//{b} bytes//";

            var sequenceNotation = new List<string>();
            foreach (var item in sequenceArrows.Select(preamble => preamble.Value))
            {
                if (!string.IsNullOrWhiteSpace(item.From))
                    sequenceNotation.Add($"{item.From}{item.ArrowNotation}{item.To}:{GetDesc(item.Description, item.JsonBytes, level)}");

                if (item.NotesFollowing.Where(LevelOK).Any())
                    sequenceNotation.Add($"note over {item.To}:{string.Join("\\n", item.NotesFollowing.Where(LevelOK).Select(StripLevel)).Replace("\n", "\\n").Replace("\r", "")}");
            }

            var participantsNotation = participantsOnly
                .Select(preamble => preamble.Value)
                .Select(item => $"participant {item.To}");

            retval.AppendJoin('\n', participantsNotation.Concat(sequenceNotation));

            return retval.ToString();
        }
    }
}
