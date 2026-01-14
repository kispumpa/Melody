using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Melody.Logic.Models
{
    public class Progress
    {
        [JsonPropertyName("currentCombo")]
        public int CurrentCombo { get; set; }

        [JsonPropertyName("totalCombo")]
        public int TotalCombo { get; set; }
    }
}
