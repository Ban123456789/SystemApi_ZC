using System.Reflection;

namespace Accura_MES.Utilities
{
    public static class ModelUtils
    {
        /// <summary>
        /// Converts any model object to a Dictionary using its public properties.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static Dictionary<string, object?> ToDictionary(object model)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));

            var dict = new Dictionary<string, object?>();
            var props = model.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var prop in props)
            {
                dict[prop.Name] = prop.GetValue(model);
            }
            return dict;
        }

        public static Dictionary<string, object?> ToDictionaryExcluding(object model, HashSet<string> excludingProps)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));

            var dict = new Dictionary<string, object?>();
            var props = model.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var prop in props)
            {
                if (excludingProps.Contains(prop.Name)) continue;

                dict[prop.Name] = prop.GetValue(model);
            }
            return dict;
        }
    }
}
