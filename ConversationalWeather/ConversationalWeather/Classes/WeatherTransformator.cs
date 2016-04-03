using System;
using System.Collections.Generic;
using Windows.Storage;
using Newtonsoft.Json;
using ConversationalWeather.Objects;

namespace ConversationalWeather.Classes
{
    class WeatherTransformator
    {
        // dictionary containing the states of the weather forecast
        public Dictionary<string, int> weatherForecastStates = new Dictionary<string, int>();

        // list containing the weather conditions
        public WeatherConditionList WeatherConditions { get; set; }

        // list containing the temperature conditions
        public TemperatureConditionList TemperatureConditions { get; set; }

        // constructor
        public WeatherTransformator()
        {
            this.loadWeatherConditions();
        }

        // initially load the weather conditions
        // these are stored in a json files in /Assets/WeatherConditions/WeatherConditions.json
        async public void loadWeatherConditions()
        {
            // open and load file
            StorageFile weatherConditionFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri(@"ms-appx:///Assets/WeatherConditions/WeatherConditions.json"));
            string jsonWeatherConditions = await FileIO.ReadTextAsync(weatherConditionFile);

            // convert file contents into object
            WeatherConditions = JsonConvert.DeserializeObject<WeatherConditionList>(jsonWeatherConditions);

            // open and load file
            StorageFile temperatureConditionFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri(@"ms-appx:///Assets/WeatherConditions/TemperatureConditions.json"));
            string jsonTemperatureConditions = await FileIO.ReadTextAsync(temperatureConditionFile);

            // convert file contents into object
            TemperatureConditions = JsonConvert.DeserializeObject<TemperatureConditionList>(jsonTemperatureConditions);
        }

        // aggregate and create the location information text
        public string GetLocationInformation(WeatherForecastObject weatherForecast)
        {
            return "Seems like you're in " + weatherForecast.city.name.ToString() + ".";
        }

        // aggregate and create the weather forecast text
        public string GetWeatherForecastText(WeatherForecastObject weatherForecast)
        {
            // initialise weather summary string
            string weatherForecastText = "There will be ";

            // iterate through all forecasted weather nodes
            for (int i = 0; i < weatherForecast.list.Count; i++)
            {
                // check if the weather state is already known
                if (!weatherForecastStates.ContainsKey(weatherForecast.list[i].weather[0].description.ToString()))
                {
                    // if not known yet, add the weather state to the dictionary
                    // however cap the number of states to 5
                    if (weatherForecastStates.Count < 5)
                    {
                        weatherForecastStates.Add(weatherForecast.list[i].weather[0].description.ToString(), weatherForecast.list[i].weather[0].id);
                    }
                }
            }

            // loop through all found weather states
            // we only need the weather states once
            // this will determine the icons and states to show on screen
            int j = 1;
            foreach (KeyValuePair<string, int> pair in weatherForecastStates)
            {
                // increase the counter
                // note that this runs one in front of the item count
                j++;

                // store the weather state in the summary string
                weatherForecastText += pair.Key;

                // check if this is an item in the middle
                if (j < weatherForecastStates.Count)
                {
                    weatherForecastText += ", ";
                }
                // or the last item in the dictionary
                else if (j == weatherForecastStates.Count)
                {
                    weatherForecastText += " and then ";
                }
            }

            // finish aggregated forecast
            weatherForecastText += " later.";

            // done
            return weatherForecastText;
        }

        // aggregate and create the temperature information text
        public string GetTemperatureInformation(WeatherForecastObject weatherForecast, bool useCelsius)
        {
            // initialise holding vars for min and max temperature
            double minTemp = 500.0;
            double maxTemp = 0.0;

            // iterate through all forecasted weather nodes
            for (int i = 0; i < weatherForecast.list.Count; i++)
            {
                // check for min / max temo
                // note that we're only interested in the "near" future
                // hence we're only checking the first 5 entries at most
                if (i < 5)
                {
                    if (weatherForecast.list[i].main.temp_min < minTemp) minTemp = weatherForecast.list[i].main.temp_min;
                    if (weatherForecast.list[i].main.temp_max > maxTemp) maxTemp = weatherForecast.list[i].main.temp_max;
                }
            }

            // build temperature string
            // note that we change the temperature unit sign according to the chosen one
            string temperatureUnitSign = "°F";
            if (useCelsius) temperatureUnitSign = "°C";
            string temperatureText = "The temperature will be between " + Convert.ToInt16(minTemp) + temperatureUnitSign + " and " + Convert.ToInt16(maxTemp) + temperatureUnitSign + ".";

            // done
            return temperatureText;
        }

        public string GetMainConditionIcon(WeatherForecastObject weatherForecast)
        {
            return "";
        }

        // aggregate and create the temperature hint text
        public string GetConditionHint(WeatherForecastObject weatherForecast, bool useCelsius)
        {
            // initialise temperature hint string
            string conditionHint = "";

            // add current weather condition hint
            // note that all the groups are defined weather conditions by OpenWeatherMap
            // http://openweathermap.org/weather-conditions
            WeatherCondition currentWeatherCondition = WeatherConditions.WeatherConditions.Find(item => item.code == weatherForecast.list[0].weather[0].id);

            // check if we really did find a weather condition
            if (currentWeatherCondition != null)
            {
                conditionHint += currentWeatherCondition.hint;
            }
            else
            {
                conditionHint += "The weather seems strange.";
            }

            // convert temperature as condition definitions are in Celsius
            int celsiusTemperature = 0;
            celsiusTemperature = Convert.ToInt16(weatherForecast.list[0].main.temp);
            if (!useCelsius) celsiusTemperature = Convert.ToInt16((weatherForecast.list[0].main.temp - 32) * 5 / 9);

            // add current temperature condition hint
            TemperatureCondition currentTemperatureCondition = TemperatureConditions.TemperatureConditions.Find(item => ((item.min <= celsiusTemperature) && (item.max > celsiusTemperature)));
            conditionHint += " " + currentTemperatureCondition.hint;

            // done
            return conditionHint;
        }

        // aggregate and create the temperature hint text
        public string GetConditionIcon(int weatherCondition)
        {
            WeatherCondition currentWeatherCondition = WeatherConditions.WeatherConditions.Find(item => item.code == weatherCondition);
            string iconName = "/Assets/WeatherIcons/" + currentWeatherCondition.icon;
            if (currentWeatherCondition.hasDayVariant)
            {
                // get the current time
                DateTime currentTime = DateTime.Now;

                // based on the time, we can choose the day / night version
                if ((currentTime.Hour > 8) && (currentTime.Hour < 20))
                {
                    // load day version
                    iconName += "_day";
                }
                else
                {
                    // load night version
                    iconName += "_night";
                }
            }

            // add file type
            iconName += ".png";

            // done
            return iconName;
        }

        // aggregate and create the temperature hint text
        public string GetHeroConditionIcon(int weatherCondition)
        {
            WeatherCondition currentWeatherCondition = WeatherConditions.WeatherConditions.Find(item => item.code == weatherCondition);
            string iconName = "/Assets/WeatherIcons/" + currentWeatherCondition.heroIcon + ".png";

            // done
            return iconName;
        }
    }
}
