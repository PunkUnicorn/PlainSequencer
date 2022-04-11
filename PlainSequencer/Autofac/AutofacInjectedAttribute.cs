using System;
using System.Linq;
using System.Reflection;

namespace PlainSequencer.Autofac
{
    public class AutofacInjectedAttribute : Attribute
    {
        public static bool AutofacInjectedAttributeOnly(PropertyInfo pi, object arg2)
        {
            return pi.GetCustomAttributes().OfType<AutofacInjectedAttribute>().Any();
        }
    }

}