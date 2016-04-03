using System;
using System.Collections.Generic;
using Windows.Storage;
using Newtonsoft.Json;
using ConversationalWeather.Objects;

using System.ComponentModel;
using System.IO;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.ViewManagement;
using Windows.UI.Notifications;
using ConversationalWeather.Classes;
using System.Linq;

namespace ConversationalWeather.Classes
{
    class WeatherTransformator
    {
        // initialise dictionary containing the states of the weather forecast
        public Dictionary<string, int> weatherForecastStates = new Dictionary<string, int>();

        public WeatherConditionList WeatherConditions { get; set; }

        // constructor
        public WeatherTransformator()
        {
            this.loadWeatherConditions();
        }

        async public void loadWeatherConditions()
        {
            StorageFile forecastConditionFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri(@"ms-appx:///Assets/WeatherConditions/WeatherConditions.json"));
            string jsonForecastCondition = await FileIO.ReadTextAsync(forecastConditionFile);

            WeatherConditions = JsonConvert.DeserializeObject<WeatherConditionList>(jsonForecastCondition);
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

        // aggregate and create the temperature hint text
        public string GetTemperatureHint(WeatherForecastObject weatherForecast, bool useCelsius)
        {
            // initialise temperature hint string
            string temperatureHint = "";

            // note that all the groups are defined weather conditions by OpenWeatherMap
            // http://openweathermap.org/weather-conditions

            WeatherCondition currentWeatherCondition = WeatherConditions.WeatherConditions.Find(item => item.code == weatherForecast.list[0].weather[0].id);

            if (currentWeatherCondition != null)
            {
                temperatureHint += currentWeatherCondition.hint;
            }
            else
            {
                temperatureHint += "The weather seems strange.";
            }

            int celsiusTemperature = 0;
            celsiusTemperature = Convert.ToInt16(weatherForecast.list[0].main.temp);
            if (!useCelsius) celsiusTemperature = Convert.ToInt16((weatherForecast.list[0].main.temp - 32) * 5 / 9);

            temperatureHint += " And it's ";

            // VERY cold
            if (celsiusTemperature < -10)
            {
                temperatureHint += "really COLD";
            }

            // very cold
            if ((celsiusTemperature >= -10) && (celsiusTemperature < 0))
            {
                temperatureHint += "pretty cold";
            }

            // cold
            if ((celsiusTemperature >= 0) && (celsiusTemperature < 10))
            {
                temperatureHint += "a bit cold";
            }

            // a bit chilly
            if ((celsiusTemperature >= 10) && (celsiusTemperature < 20))
            {
                temperatureHint += "a bit chilly";
            }

            // nice
            if ((celsiusTemperature >= 20) && (celsiusTemperature < 30))
            {
                temperatureHint += "quite nice";
            }

            // warm
            if ((celsiusTemperature >= 30) && (celsiusTemperature < 35))
            {
                temperatureHint += "warm and cozy";
            }

            // hot
            if ((celsiusTemperature >= 35) && (celsiusTemperature < 40))
            {
                temperatureHint += "pretty hot";
            }

            // REALLY hot
            if (celsiusTemperature >= 40)
            {
                temperatureHint += "really HOT";
            }

            // finish temperature hint
            temperatureHint += " outside.";

            // done
            return temperatureHint;
        }
    }
}
