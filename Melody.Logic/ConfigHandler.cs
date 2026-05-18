// Copyright (c) Matula Márton. All rights reserved.

namespace Melody.Logic
{
    using Melody.Logic.Models;
    using YamlDotNet.Serialization;
    using YamlDotNet.Serialization.NamingConventions;

    /// <summary>Handles reading and parsing the configuration file for the Melody application.</summary>
    public class ConfigHandler
    {
        /// <summary>Reads and parses the configuration file for the Melody application.</summary>
        /// <param name="filePath">The path to the configuration file.</param>
        /// <returns>The parsed configuration object.</returns>
        public static ConfigObject ReadConfigFile(string filePath)
        {
            try
            {
                string yamlContent = File.ReadAllText(filePath);

                IDeserializer deserializer = new DeserializerBuilder()
                    .WithNamingConvention(CamelCaseNamingConvention.Instance)
                    .Build();

                ConfigObject config = deserializer.Deserialize<ConfigObject>(yamlContent);

                return config;
            }
            catch (FileNotFoundException)
            {
                Console.WriteLine($"Hiba: A fájl nem található: {filePath}");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Hiba történt a YAML beolvasásakor: {ex.Message}");
                return null;
            }
        }
    }
}
