using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Melody.Logic.Models
{
    public class SheetCollection
    {
        [JsonPropertyName("sheets")]
        public Dictionary<string, string> Sheets { get; set; } //id, name
    }
}
