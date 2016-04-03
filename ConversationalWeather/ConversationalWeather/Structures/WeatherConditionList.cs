using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConversationalWeather.Objects
{
    public class WeatherCondition
    {
        public int code { get; set; }
        public string hint { get; set; }
        public string meaning { get; set; }
        public string icon { get; set; }
        public bool hasDayVariant { get; set; }
    }

    public class WeatherConditionList
    {
        public List<WeatherCondition> WeatherConditions { get; set; }
    }
}
