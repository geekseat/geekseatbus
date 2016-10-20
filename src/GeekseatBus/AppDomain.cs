using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Extensions.DependencyModel;

namespace GeekseatBus
{
    public class AppDomain
    {
        public static AppDomain CurrentDomain { get; }

        static AppDomain()
        {
            if (CurrentDomain == null) CurrentDomain = new AppDomain();
        }

        public Assembly[] GetAssemblies()
        {
            var assemblies = new List<Assembly>();
            var dependencies = DependencyContext.Default.RuntimeLibraries;
            foreach (var library in dependencies)
            {
                if (!IsCandidateCompilationLibrary(library)) continue;

                try
                {
                    var assembly = Assembly.Load(new AssemblyName(library.Name));
                    assemblies.Add(assembly);
                }
                catch (Exception)
                {
                    // ignored
                }
            }

            return assemblies.ToArray();
        }

        // ReSharper disable once UnusedParameter.Local
        private static bool IsCandidateCompilationLibrary(RuntimeLibrary compilationLibrary)
        {
            return true;
        }
    }
}