using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Melody.Logic.Models
{
    public class PracticeStructure
    {
        [JsonPropertyName("combos")]
        public List<Combo> Combos { get; set; }
    }
}
