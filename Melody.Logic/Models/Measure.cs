using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Melody.Logic.Models
{
    public class Measure
    {
        [JsonPropertyName("id")]
        public string ID { get; set; }

        [JsonPropertyName("noteNumbers")]
        public List<int> NoteNumbers { get; set; }
    }
}
