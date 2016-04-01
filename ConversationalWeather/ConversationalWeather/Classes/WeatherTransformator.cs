using System;
using System.Collections.Generic;
using ConversationalWeather.Objects;

namespace ConversationalWeather.Classes
{
    class WeatherTransformator
    {
        // initialise dictionary containing the states of the weather forecast
        public Dictionary<string, int> weatherForecastStates = new Dictionary<string, int>();

        // constructor
        public WeatherTransformator()
        {
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

            // group 2xx: thunderstorm
            if ((weatherForecast.list[0].weather[0].id >= 200) && (weatherForecast.list[0].weather[0].id <= 299))
            {
                temperatureHint += "There is a " + weatherForecast.list[0].weather[0].description + " going on. Or will be soon. Better get inside!";
            }

            // group 3xx: drizzle
            if ((weatherForecast.list[0].weather[0].id >= 300) && (weatherForecast.list[0].weather[0].id <= 399))
            {
                temperatureHint += "Drip, drop, " + weatherForecast.list[0].weather[0].description + " outside. Better bring an umbrella.";
            }

            // group 5xx: rain
            if ((weatherForecast.list[0].weather[0].id >= 500) && (weatherForecast.list[0].weather[0].id <= 600))
            {
                temperatureHint += "Meh, " + weatherForecast.list[0].weather[0].description + " outside or coming up. Better get somewhere dry.";
            }

            // group 6xx: snow
            if ((weatherForecast.list[0].weather[0].id >= 600) && (weatherForecast.list[0].weather[0].id <= 699))
            {
                temperatureHint += "Brr, there is " + weatherForecast.list[0].weather[0].description + " outside. Keep yourself warm.";
            }

            // group 7xx: atmosphere
            if ((weatherForecast.list[0].weather[0].id >= 700) && (weatherForecast.list[0].weather[0].id <= 799))
            {
                temperatureHint += "Oh! A bit of " + weatherForecast.list[0].weather[0].description + "! Really?";
            }

            // group 800: clear
            if (weatherForecast.list[0].weather[0].id == 800)
            {
                temperatureHint += "All is clear, nothing to see.";
            }

            // group 80x: clouds
            if ((weatherForecast.list[0].weather[0].id >= 801) && (weatherForecast.list[0].weather[0].id <= 899))
            {
                temperatureHint = "It's cloudy. Just some " + weatherForecast.list[0].weather[0].description + ".";
            }

            // group 9xx: extreme
            if ((weatherForecast.list[0].weather[0].id >= 900) && (weatherForecast.list[0].weather[0].id <= 999))
            {
                temperatureHint = "Oh? Wow!. Be careful out there!";
            }

            int celsiusTemperature = 0;
            celsiusTemperature = Convert.ToInt16(weatherForecast.list[0].main.temp);
            if (!useCelsius) celsiusTemperature = Convert.ToInt16((weatherForecast.list[0].main.temp - 32) * 5 / 9);

            temperatureHint += " And it's ";

            // VERY cold
            if (celsiusTemperature < -10)
            {
                temperatureHint += "really COLD!";
            }

            // very cold
            if ((celsiusTemperature >= -10) && (celsiusTemperature < 0))
            {
                temperatureHint += "pretty cold.";
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

            // hot
            if ((celsiusTemperature >= 30) && (celsiusTemperature < 40))
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
