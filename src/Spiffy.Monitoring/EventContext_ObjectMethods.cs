using System.Linq;
using System.Reflection;

namespace Spiffy.Monitoring
{
    public partial class EventContext
    {
        public EventContext IncludeStructure(object structure, string keyPrefix = null, bool includeNullValues = true)
        {
            if (structure != null)
            {
                foreach (var property in structure.GetType().GetTypeInfo().DeclaredProperties.Where(p => p.CanRead))
                {
                    try
                    {
                        var val = property.GetValue(structure, null);
                        if (val == null && !includeNullValues)
                        {
                            continue;
                        }
                        string key = string.IsNullOrEmpty(keyPrefix)
                            ? property.Name
                            : string.Format("{0}_{1}", keyPrefix, property.Name);
                        this[key] = val;
                    }
                    // ReSharper disable once EmptyGeneralCatchClause -- intentionally squashed
                    catch
                    {
                    }
                }
            }

            return this;
        }
    }
}