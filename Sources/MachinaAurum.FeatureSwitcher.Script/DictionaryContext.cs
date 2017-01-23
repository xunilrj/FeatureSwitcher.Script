using Contexteer;
using System.Collections.Generic;

namespace MachinaAurum.FeatureSwitcher.Script
{
    public class DictionaryContext : Dictionary<string, object>, IContext
    {
        public DictionaryContext()
        {
        }
    }
}
