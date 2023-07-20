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
        }

        private List<string> notation = new List<string>();
        private Dictionary<int, SequenceArrow> sequenceArrows = new Dictionary<int, SequenceArrow>();

        public void Fail(SequenceItemAbstract item, string message)
        {
            var uniqueItemKey = item.PeerUniqueFullName.GetHashCode();
            sequenceArrows[uniqueItemKey].ArrowNotation = "-x";
            sequenceArrows[uniqueItemKey].NotesFollowing.Add($"<{SequenceProgressLogLevel.Brief}>Error: {message}");

            Console.WriteLine(message);
        }

        public void Fail(SequenceItemAbstract item, Exception e)
        {
            var uniqueItemKey = item.PeerUniqueFullName.GetHashCode();
            sequenceArrows[uniqueItemKey].ArrowNotation = "-x";
            sequenceArrows[uniqueItemKey].NotesFollowing.Add($"<{SequenceProgressLogLevel.Brief}>Error: {e.Message}");

            Console.WriteLine(e.Message);
        }

        public void Progress(SequenceItemAbstract item, string message, SequenceProgressLogLevel level)
        {
            var uniqueItemKey = item.PeerUniqueFullName.GetHashCode();
            sequenceArrows[uniqueItemKey].NotesFollowing.Add($"<{level}>{message}");

            Console.WriteLine(message);
        }

        public void DataOutProgress(SequenceItemAbstract item, string message, SequenceProgressLogLevel level)
        {
            var uniqueItemKey = item.PeerUniqueFullName.GetHashCode();
            sequenceArrows[uniqueItemKey].NotesFollowing.Add($"<{level}>{message}");

            Console.WriteLine(message);
        }

        public void DataInProgress(SequenceItemAbstract item, string message, SequenceProgressLogLevel level)
        {
            var uniqueItemKey = item.PeerUniqueFullName.GetHashCode();
            sequenceArrows[uniqueItemKey].NotesFollowing.Add($"<{level}>{message}");

            Console.WriteLine(message);
        }

        public void FinishedItem(SequenceItemAbstract item)
        {
        }

        public void StartItem(SequenceItemAbstract item)
        {
            var sa = new SequenceArrow
            {
                From = CleanName(item.Parent?.Name),
                To = CleanName(item.Name),
                Description = CleanModel(item.Model?.ToString())
            };

            sequenceArrows.Add(item.PeerUniqueFullName.GetHashCode(), sa);
        }

        private static string CleanModel(string model) => model?.ToString()?.Replace("\n", "\\n")?.Replace("\r", "") ?? "...";

        public void SequenceComplete(bool isSuccess, string model)
        {
            var allColumns = sequenceArrows.Values
                .Select(item => item.From)
                .Union(sequenceArrows.Values.Select(item => item.To));

            var noFrom = sequenceArrows.Values
                .Select(item => item.To)
                .Except(sequenceArrows.Values.Select(item => item.From))
                .Single();

            var noLineFromKeys = sequenceArrows
                .Where(item => noFrom.Contains(item.Value.To))
                .Select(item => item.Key)
                .ToArray();

            //foreach (var key in noLineFromKeys)
            //{
            var newArrow = new SequenceArrow
            {
                To = "Result",
                From = noFrom,//sequenceArrows[key].To,
                Description = CleanModel(model)// sequenceArrows[key].Description,
            };
            newArrow.ArrowNotation = isSuccess ? newArrow.ArrowNotation : "-x";

            //if (noLineFromKeys.Last() == key)
            //{
            //    newArrow.NotesFollowing.Add($"<{SequenceProgressLogLevel.Brief}>{model}");
            //}
            var guidHashcode = Guid.NewGuid().GetHashCode();
            //var insertAfter = sequenceArrows[key]
            sequenceArrows.Add(guidHashcode, newArrow);
            //}
        }

        public string GetSequenceDiagramNotation(string title, SequenceProgressLogLevel level = SequenceProgressLogLevel.Diagnostic)
        {
            var retval = new StringBuilder($"title {title}\n");

            var participantsOnly = sequenceArrows
                .Where(item => string.IsNullOrWhiteSpace(item.Value.To) || string.IsNullOrWhiteSpace(item.Value.From));

            Func<string, bool> LevelOK = (s) => level == SequenceProgressLogLevel.Diagnostic || s.StartsWith($"<{level}>");
            Func<string, int, string> StripLevel = (s, i) => s.Substring(s.IndexOf('>')+1);

            var sequenceNotation = new List<string>();
            foreach (var item in sequenceArrows.Select(preamble => preamble.Value))
            {
                if (!string.IsNullOrWhiteSpace(item.From))
                    sequenceNotation.Add($"{item.From}{item.ArrowNotation}{item.To}:{item.Description}");

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
