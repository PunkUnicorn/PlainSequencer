using PlainSequencer.SequenceItemActions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PlainSequencer.Logging
{
    public class SequenceLogger : ISequenceLogger
    {
        private string CleanName(string pname) => pname?.Trim()?.Replace("-", " ");

        public class SequenceArrow
        {
            public List<string> PreceedingANotes { get; set; } = new List<string>();
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
            sequenceArrows[uniqueItemKey].PreceedingANotes.Add($"<{SequenceProgressLogLevel.Brief}>Error: {message}");

            Console.WriteLine(message);
        }

        public void Fail(SequenceItemAbstract item, Exception e)
        {
            var uniqueItemKey = item.PeerUniqueFullName.GetHashCode();
            sequenceArrows[uniqueItemKey].ArrowNotation = "-x";
            sequenceArrows[uniqueItemKey].PreceedingANotes.Add($"<{SequenceProgressLogLevel.Brief}>Error: {e.Message}");

            Console.WriteLine(e.Message);
        }

        public void Progress(SequenceItemAbstract item, string message, SequenceProgressLogLevel level)
        {
            var uniqueItemKey = item.PeerUniqueFullName.GetHashCode();
            sequenceArrows[uniqueItemKey].PreceedingANotes.Add($"<{level}>{message}");

            Console.WriteLine(message);
        }

        public void DataOutProgress(SequenceItemAbstract item, string message, SequenceProgressLogLevel level)
        {
            var uniqueItemKey = item.PeerUniqueFullName.GetHashCode();
            sequenceArrows[uniqueItemKey].PreceedingANotes.Add($"<{level}>{message}");

            Console.WriteLine(message);
        }

        public void DataInProgress(SequenceItemAbstract item, string message, SequenceProgressLogLevel level)
        {
            var uniqueItemKey = item.PeerUniqueFullName.GetHashCode();
            sequenceArrows[uniqueItemKey].PreceedingANotes.Add($"<{level}>{message}");

            Console.WriteLine(message);
        }

        public void Finished(SequenceItemAbstract item)
        {
        }

        public void Starting(SequenceItemAbstract item)
        {
            var sa = new SequenceArrow 
            { 
                From = CleanName(item.Parent?.Name),
                To = CleanName(item.Name),
                Description = item.Model?.ToString()?.Replace("\n", "\\n")?.Replace("\r", "")
            };

            sequenceArrows.Add(item.PeerUniqueFullName.GetHashCode(), sa);
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

                sequenceNotation.Add($"note over {item.To}:{string.Join("\\n", item.PreceedingANotes.Where(LevelOK).Select(StripLevel)).Replace("\n", "\\n").Replace("\r", "")}");
            }

            var participantsNotation = participantsOnly
                .Select(preamble => preamble.Value)
                .Select(item => $"participant {item.To}");

            retval.AppendJoin('\n', participantsNotation.Concat(sequenceNotation));

            return retval.ToString();
        }
    }
}
