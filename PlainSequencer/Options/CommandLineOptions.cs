using CommandLine;
using PlainSequencer.Script;
using PlainSequencer.SequenceItemActions;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;

namespace PlainSequencer.Options
{
    public class CommandLineOptions : ICommandLineOptions
    {
        public static CommandLineOptions CreateCommandLineOptions(string[] args)
        {
            var sb = new StringBuilder();
            var retval = new CommandLineOptions();
            var parsedOptions = Parser.Default.ParseArguments<CommandLineOptions>(args);
            var options = parsedOptions.MapResult((o) => o, (ohh) =>
            {
                //foreach (var err in ohh)
                //{
                //    if (err is not UnknownOptionError)
                //        continue;

                //    var extra = err as UnknownOptionError;
                //    var newVarSplit = extra.Token.IndexOf(':');
                //    if (newVarSplit == -1)
                //        return null;

                //    retval.Args.Add(
                //        extra.Token.Substring(0, newVarSplit), 
                //        SequenceItemStatic.GetResponseItems(null, null, extra.Token.Substring(newVarSplit+1)));
                //}
                //return retval;
                return null as CommandLineOptions;
            });
            return options;
        }

        [Option('y', "yaml", Group = "Input", HelpText = "Yaml sequence script filename")]
        public string YamlFile { get; set; }

        [Option('j', "json", Group = "Input", HelpText = "Json sequence script filename")]
        public string JsonFile { get; set; }

        [Option('s', "stdin", Group = "Input", HelpText = "Pass to use std (autodetects yaml/json)")]
        public bool IsStdIn { get; set; }

        [Option('d', "direct", Group = "Input", HelpText = "For unit tests but it might possibly work for the command line; it resolves the value of this variable to a SequenceScript complex object")]
        public SequenceScript Direct { get; set; }

        [Option('a', "args", Required = false, HelpText = "Key/pair values for the sequence script's arguments, e.g. -a \"blah=dev\",\"blah2=server3\" then reference {{args.blah}} and {{args.blah2}} within the script")]
        public IEnumerable<string> Args { get; set; }

        public static IEnumerable<string> GetCommands()
        {
            return typeof(CommandLineOptions).GetProperties()
                .Where(prop => prop.IsDefined(typeof(OptionAttribute), false))
                .Select(s => s.Name)
                .ToArray();
        }
    }
}