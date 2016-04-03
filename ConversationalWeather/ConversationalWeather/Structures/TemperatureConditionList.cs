using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConversationalWeather.Objects
{
    public class TemperatureCondition
    {
        public int min { get; set; }
        public int max { get; set; }
        public string hint { get; set; }
    }

    public class TemperatureConditionList
    {
        public List<TemperatureCondition> TemperatureConditions { get; set; }
    }
}
