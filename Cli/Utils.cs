using System;
using System.Reflection;

namespace Cli
{
    public static class Utils
    {
        public static void Welcome()
        {
            string version = null;
            string product = null;
            try
            {
                version = Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
                product = Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyProductAttribute>()?.Product;
                if (!string.IsNullOrEmpty(version))
                {
                    version = "v" + version;
                }
            }
            catch { }

            Console.WriteLine();
            Console.WriteLine(@" _  __                      ");
            Console.WriteLine(@"| |/ /___ ___ _ __  ___ _ _ ");
            Console.WriteLine(@"| ' </ -_) -_) '_ \/ -_) '_|");
            Console.WriteLine(@"|_|\_\___\___| .__/\___|_|  ");
            Console.WriteLine(@"             |_|            ");
            Console.WriteLine(@"password manager & digital vault");
            Console.WriteLine();
            Console.WriteLine($"{product ?? ""} {version ?? ""}");
            Console.WriteLine("Type \"?\" for command help");
            Console.WriteLine();
        }
    }
}
