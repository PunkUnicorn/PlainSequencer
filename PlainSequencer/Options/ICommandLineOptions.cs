﻿using PlainSequencer.Script;
using System.Collections.Generic;
using System.Dynamic;

namespace PlainSequencer.Options
{
    public interface ICommandLineOptions
    {
        SequenceScript Direct { get; set; }
        bool IsStdIn { get; set; }
        string JsonFile { get; set; }
        //string Variables { get; set; }
        string YamlFile { get; set; }

        public IEnumerable<string> Args { get; }
    }
}