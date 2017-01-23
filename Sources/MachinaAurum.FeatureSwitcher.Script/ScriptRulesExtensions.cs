using Contexteer;
using MachinaAurum.FeatureSwitcher.Script;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FeatureSwitcher.Configuration
{
    public static class JavascriptRulesExtensions
    {
        public static IConfigureFeaturesFor<TContext> ScriptRules<TContext>(this IConfigureBehaviorFor<TContext> context, IDictionary<string, string> rules, ScriptRulesConfiguration config = null) where TContext : IContext
        {
            if (config == null)
            {
                config = new ScriptRulesConfiguration();
                config.OnRuleNotFound = config.OnRuleNotFound ?? new Func<string, bool>(x => false);
                config.OnScriptError = config.OnScriptError ?? new Func<Exception, bool>(x => false);
            }

            if (string.IsNullOrEmpty(config.JSEngineName))
            {
                var jintFound = AppDomain.CurrentDomain.GetAssemblies().Where(x => x.GetName().Name == "jint").Any();

                if (jintFound)
                {
                    config.JSEngineName = "jint";
                }
            }

            config.JSEngineName = config.JSEngineName.ToLower();

            if (config.JSEngineName == "jint")
            {
                return context.Custom((ctx) =>
                {
                    return new Feature.Behavior[] { new Feature.Behavior(name => RunJint(name, ctx, rules, config.ContextVariableName, config.OnRuleNotFound, config.OnScriptError)) };
                });
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(config.JSEngineName), $"Unknow script engine: {config.JSEngineName}");
            }
        }

        private static bool RunJint(Feature.Name name, IContext ctx, IDictionary<string, string> rules, string contextVariable, Func<string, bool> onNotFound, Func<Exception, bool> onJavascriptError)
        {
            var engineType = Type.GetType("Jint.Engine, jint");
            dynamic engine = Activator.CreateInstance(engineType);

            FillContext(ctx, contextVariable, engine);

            var rule = string.Empty;

            if (rules.TryGetValue(name.Value, out rule) == false)
            {
                return onNotFound(name.Value);
            }

            try
            {
                var value = engine
                    .Execute(rule)
                    .GetCompletionValue();

                if (value.IsString())
                {
                    bool possibleBool = false;
                    if (bool.TryParse(value.AsString(), out possibleBool))
                    {
                        return possibleBool;
                    }
                }

                if (value.IsBoolean() == false)
                {
                    return false;
                }

                return value.AsBoolean();
            }
            catch (Exception ex) when (ex.GetType().Name == "JavaScriptException")
            {
                return onJavascriptError(ex);
            }
        }

        private static void FillContext(IContext ctx, string contextVariable, dynamic engine)
        {
            if (string.IsNullOrEmpty(contextVariable))
            {
                if (ctx is IDictionary<string, object>)
                {
                    FillContextDictionary(ctx, engine);
                }
                else
                {
                    FillContextReflection(ctx, engine);
                }
            }
            else
            {
                engine.SetValue(contextVariable, ctx);
            }
        }

        private static void FillContextDictionary(IContext ctx, dynamic engine)
        {
            var ctxAsDictionary = ctx as IDictionary<string, object>;
            foreach (var kv in ctxAsDictionary)
            {
                engine.SetValue(kv.Key, kv.Value);
            }
        }

        private static void FillContextReflection(IContext ctx, dynamic engine)
        {
            var contextType = ctx.GetType();
            var contextProperties = contextType.GetProperties();
            foreach (var property in contextProperties)
            {
                if (property.CanRead)
                {
                    var value = property.GetValue(ctx);
                    engine.SetValue(property.Name, value);
                }
            }
        }
    }
}
