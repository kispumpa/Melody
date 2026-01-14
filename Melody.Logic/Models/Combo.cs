using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Melody.Logic.Models
{
    public class Combo
    {
        [JsonPropertyName("measureNumber")]
        public object MeasureNumber { get; set; }

        [JsonPropertyName("phase")]
        public int Phase { get; set; }
    }
}
