using Newtonsoft.Json;
using SharpYaml.Serialization;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;

namespace PlainSequencer.YamlExtensions
{
    public static class YamlNodeExtensions
    {
        /// <summary>
        /// The raw YAML property name is passed in, and the function is expected to return the name of the resulting POCO property
        /// </summary>
        /// <param name="uncleanNameToBeCleaned">The raw YAML property name</param>
        /// <returns>The new property name, for the resulting POCO</returns>
        public delegate string CleanNameDelegate(string uncleanNameToBeCleaned);

        public static string ToJson(this YamlNode topNode, CleanNameDelegate cleanNameFunc = null)
        {
            var poco = ToPoco(topNode, cleanNameFunc);
            return JsonConvert.SerializeObject(poco);
        }

        public static T ToPoco<T>(this YamlNode topNode, CleanNameDelegate cleanNameFunc = null)
        {
            var poco = ToPoco(topNode, cleanNameFunc);
            //var json = ToJson(topNode, cleanNameFunc);
            //void errorFunc(object sender, ErrorEventArgs e) => throw new Exception(e.CurrentObject?.ToString() + " " + e.ErrorContext.Member?.ToString());
            //var jsonSettings = new JsonSerializerSettings { MissingMemberHandling = MissingMemberHandling.Error, Error = errorFunc };

            return JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(poco));//, jsonSettings));
        }



        public static dynamic ToPoco(this YamlNode topNode, CleanNameDelegate cleanNameFunc = null)
        {
            var expandoDict =
                new ExpandoObject()
                    as IDictionary<string, object>;

            switch (topNode)
            {
                case YamlMappingNode mapping:
                    ProcessMappingChildren(expandoDict, mapping);
                    break;

                case YamlSequenceNode sequence:
                    return sequence.Select(s => ToPoco(s, cleanNameFunc)).ToArray();

                case YamlScalarNode scalar:
                    var v = ProcessScalarNode(scalar);
                    return v;
            }

            return expandoDict;
        }

        private static void ProcessMappingChildren(IDictionary<string, object> expandoDict, YamlMappingNode mapping, CleanNameDelegate cleanNameFunc = null)
        {
            if (cleanNameFunc == null)
                cleanNameFunc = DefaultJsonToYamlCleanNameFunc;

            foreach (var entry in mapping.Children)
            {
                dynamic v;

                switch (entry.Value)
                {
                    case YamlMappingNode mappingNode:
                        v = ToPoco(mappingNode, cleanNameFunc);
                        break;

                    case YamlSequenceNode sequenceNode:
                        v = sequenceNode.Children.Select(s => ToPoco(s, cleanNameFunc)).ToArray();
                        break;

                    case YamlScalarNode scalerNode:
                        v = ProcessScalarNode(scalerNode);
                        break;

                    default:
                        v = "";
                        break;
                }
                var key = cleanNameFunc( ((YamlScalarNode)entry.Key).Value );
                expandoDict.Add(key, v);
            }
        }

        private static dynamic ProcessScalarNode(YamlScalarNode scalerNode)
        {
            dynamic v;
            if (int.TryParse(scalerNode.Value, out var i))
                v = i;
            else if (decimal.TryParse(scalerNode.Value, out var dec))
                v = dec;
            else if (bool.TryParse(scalerNode.Value, out var bo))
                v = bo;
            else
                v = scalerNode.Value;
            return v;
        }

        private static string DefaultJsonToYamlCleanNameFunc(string uncleanNameToBeCleaned)
        {
            if (uncleanNameToBeCleaned.Length == 0) throw new ArgumentException($"{nameof(uncleanNameToBeCleaned)} is empty");

            var semiClean
                = new string(
                    uncleanNameToBeCleaned.Select(s => char.IsLetterOrDigit(s) ? s : '_').ToArray()
                );

            var almostFinished = semiClean;
            var firstBit = char.IsLetter(almostFinished[0]) // Leading numbers are ok for json names but not C# properties so add junk to the start to make them legal property names :S
                ? ""
                : $"{MakeRandomJunk()}";
           
            return $"{firstBit}{almostFinished}".Trim();
        }

        private static string MakeRandomJunk(int wantedLengthOfJunk=5) 
        {
            var r = new Random();
            var sb = new StringBuilder();

            for (int x=0; x<wantedLengthOfJunk; x++)
                sb.Append(r.Next('a', 'Z'));

            return sb.ToString();
        }
    }
}
