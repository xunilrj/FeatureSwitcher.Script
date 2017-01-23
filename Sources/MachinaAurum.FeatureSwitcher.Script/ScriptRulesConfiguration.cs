using System;

namespace MachinaAurum.FeatureSwitcher.Script
{
    public class ScriptRulesConfiguration
    {
        public string ContextVariableName { get; set; }
        public Func<string, bool> OnRuleNotFound = null;
        public Func<Exception, bool> OnScriptError = null;
        public string JSEngineName = null;
    }
}
