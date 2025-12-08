using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Melody.Logic
{
    public class LilypondConfig
    {
        public string PythonPath { get; set; }

        public string LilypondPath { get; set; }

        public string Mxml2lyPath { get; set; }
    }

    public class ConfigObject
    {
        public LilypondConfig LilypondConfig { get; set; }
    }

    public class ConfigHandler
    {
        public static ConfigObject ReadConfigFile(string filePath)
        {
            try
            {
                var yamlContent = File.ReadAllText(filePath);

                var deserializer = new DeserializerBuilder()
                    .WithNamingConvention(CamelCaseNamingConvention.Instance)
                    .Build();

                var config = deserializer.Deserialize<ConfigObject>(yamlContent);

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
