using System;
using System.Collections.Generic;
using System.Linq;

namespace Mcma.Core.Serialization
{
    public static class McmaTypes
    {
        private static List<Type> Types { get; } = new List<Type>();

        public static void Add<T>()
        {
            Console.WriteLine("Adding MCMA type " + typeof(T).Name);
            if (!Types.Contains(typeof(T)))
                Types.Add(typeof(T));
        }

        public static Type FindType(string typeString)
        {
            // check for match in explicitly-provided type collection
            var objectType = Types.FirstOrDefault(t => t.Name.Equals(typeString, StringComparison.OrdinalIgnoreCase));
            if (objectType == null)
            {
                // check for match in core types
                objectType = Type.GetType(typeof(IMcmaObject).AssemblyQualifiedName.Replace(nameof(IMcmaObject), typeString));
            }

            return objectType;
        }
    }
}