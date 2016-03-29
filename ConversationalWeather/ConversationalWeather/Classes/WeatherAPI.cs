using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Geolocation;
using Windows.Web.Http;
using Windows.Web.Http.Filters;
using Newtonsoft.Json;
using ConversationalWeather.Objects;

namespace ConversationalWeather.WeatherAPI
{
    public class WeatherInterface : INotifyPropertyChanged
    {
        // property changed callback
        public event PropertyChangedEventHandler PropertyChanged;

        // cancellation token for aborting geolocation search
        private CancellationTokenSource _cts = null;

        // http filter and client
        private HttpBaseProtocolFilter filter;
        private HttpClient httpClient;

        // flag to indicate that geolocation is known
        public Boolean geopositionKnown = false;

        // flag to indicate that weather data is known
        public Boolean weatherDataLoaded = false;

        // flag to indicate temperature units
        public Boolean useCelsius = true;

        // contains the found geolocation
        private Geoposition _currentPosition;

        // contains the status of the geolocation tracking
        private String _status = "";

        // object containing the returned weather data
        private WeatherForecastObject _weatherForecast = new WeatherForecastObject();

        // dictionary containing the states of the weather forecast
        public Dictionary<string, int> weatherForecastStates = new Dictionary<string, int>();

        // constructor
        public WeatherInterface()
        {
            // set initial values for flags
            geopositionKnown = false;
            weatherDataLoaded = false;
            useCelsius = true;

            // create an HttpClient instance with default settings. I.e. no custom filters. 
            filter = new HttpBaseProtocolFilter();
            httpClient = new HttpClient(filter);
        }

        // setter / getter for the current position
        public Geoposition CurrentPosition
        {
            get { return _currentPosition; }
            set
            {
                // set status that geolocation was found
                Status = "Found your position.";

                // we have a geolocation locked now
                geopositionKnown = true;

                // store the data
                _currentPosition = value;

                // call the OnPropertyChanged callback to indicate new data
                this.OnPropertyChanged("CurrentPosition");
            }
        }

        // setter / getter for the weather data response
        public WeatherForecastObject WeatherForecast
        {
            get { return _weatherForecast; }
            set
            {
                // set status that weather data was found
                Status = "Weather data received.";

                // we have weather data now
                weatherDataLoaded = true;

                // store the data
                _weatherForecast = value;

                // call the OnPropertyChanged callback to indicate new data
                this.OnPropertyChanged("WeatherForecast");
            }
        }

        // setter / getter for the status
        public String Status
        {
            get { return _status; }
            set
            {
                // store the data
                _status = value;

                // call the OnPropertyChanged callback to indicate new data
                this.OnPropertyChanged("Status");
            }
        }

        // create the OnPropertyChanged method to raise the event
        public void OnPropertyChanged(string name)
        {
            // get the handler for the event
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                // if somebody subscribed, send the event
                handler(this, new PropertyChangedEventArgs(name));
            }
        }

        // get the current user location
        // this will use Windows.Devices.Geolocation to query the current location 
        async public void GetCurrentLocation()
        {
            try
            {
                Status = "Trying to get your location.";

                // request permission to access location
                var accessStatus = await Geolocator.RequestAccessAsync();

                // user chose if access to geolocation features is granted
                switch (accessStatus)
                {
                    // in case the user allowed it, start searching for the location
                    case GeolocationAccessStatus.Allowed:

                        // get cancellation token
                        _cts = new CancellationTokenSource();
                        CancellationToken token = _cts.Token;

                        // if DesiredAccuracy or DesiredAccuracyInMeters are not set (or value is 0), DesiredAccuracy.Default is used.
                        Geolocator geolocator = new Geolocator { DesiredAccuracyInMeters = 0 };

                        // try to get the geolocation
                        // if successful this will change the CurrentPosition property
                        CurrentPosition = await geolocator.GetGeopositionAsync().AsTask(token);
                        break;

                    // if the user denied the access
                    case GeolocationAccessStatus.Denied:
                        geopositionKnown = false;
                        Status = "Access to location is denied.";
                        break;

                    // if something else went wrong
                    case GeolocationAccessStatus.Unspecified:
                        geopositionKnown = false;
                        Status = "Unspecified geolocation tracking error.";
                        break;
                }
            }
            // user cancelled request
            catch (TaskCanceledException)
            {
                geopositionKnown = false;
                Status = "Geolocation tracking cancelled.";
            }
            // exception was caught
            catch (Exception ex)
            {
                geopositionKnown = false;
                Status = "General error when tracking geolocation: " + ex.ToString();
            }
            // done
            finally
            {
                geopositionKnown = false;
                _cts = null;
            }
        }

        // get the current user location
        // this will use Windows.Devices.Geolocation to query the current location 
        async public void GetWeatherForcastForGeoposition()
        {
            // set status to loading
            Status = "Loading weather data.";

            try
            {
#if DEBUG
                String weatherApiUrl = "http://localhost:8888/forecast.json";
#else
                // create URL for getting forcast
                String weatherApiUrl = "http://api.openweathermap.org/data/2.5/forecast";
                weatherApiUrl += "?lat=" + CurrentPosition.Coordinate.Latitude.ToString();
                weatherApiUrl += "&lon=" + CurrentPosition.Coordinate.Longitude.ToString();
                weatherApiUrl += "&APPID=60c2636cd48cde2176af74be0d397d00";

                // check which temperature unit to use
                // note that the default for the API is Kelvin
                if (useCelsius)
                {
                    weatherApiUrl += "&units=metric";
                } else {
                    weatherApiUrl += "&units=imperial";
                }
#endif

                // load weather forcast data
                Uri myUri = new Uri(weatherApiUrl, UriKind.Absolute);
                HttpResponseMessage response = await httpClient.GetAsync(myUri).AsTask(_cts.Token);

                // convert json data to object
                // if successful this will change the WeatherForecast property
                WeatherForecast = JsonConvert.DeserializeObject<WeatherForecastObject>(response.Content.ToString());
            }
            // exception was caught
            catch (Exception ex)
            {
                Status = "A problem occured during loading and converting the weather data.\nPlease make sure you are connected to the internet.\nDouble tap to try again.";
            }
        }

        // aggregate and create the location information text
        public String GetLocationInformation()
        {
            return "Seems like you're in " + WeatherForecast.city.name.ToString() + ".";
        }

        // aggregate and create the weather forecast text
        public String GetWeatherForecastText()
        {
            // initialise weather summary string
            String weatherForecastText = "There will be ";

            // iterate through all forecasted weather nodes
            for (int i = 0; i < WeatherForecast.list.Count; i++)
            {
                // check if the weather state is already known
                if (!weatherForecastStates.ContainsKey(WeatherForecast.list[i].weather[0].description.ToString()))
                {
                    // if not known yet, add the weather state to the dictionary
                    weatherForecastStates.Add(WeatherForecast.list[i].weather[0].description.ToString(), WeatherForecast.list[i].weather[0].id);
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
        public String GetTemperatureInformation()
        {
            // initialise holding vars for min and max temperature
            double minTemp = 500.0;
            double maxTemp = 0.0;

            // iterate through all forecasted weather nodes
            for (int i = 0; i < WeatherForecast.list.Count; i++)
            {
                // check for min / max temo
                // note that we're only interested in the "near" future
                // hence we're only checking the first 5 entries at most
                if (i < 5)
                {
                    if (WeatherForecast.list[i].main.temp_min < minTemp) minTemp = WeatherForecast.list[i].main.temp_min;
                    if (WeatherForecast.list[i].main.temp_max > maxTemp) maxTemp = WeatherForecast.list[i].main.temp_max;
                }
            }

            // build temperature string
            // note that we change the temperature unit sign according to the chosen one
            String temperatureUnitSign = "°F";
            if (useCelsius) temperatureUnitSign = "°C";
            String temperatureText = "The temperature will be between " + Convert.ToInt16(minTemp) + temperatureUnitSign + " and " + Convert.ToInt16(maxTemp) + temperatureUnitSign + ".";

            // done
            return temperatureText;
        }

        // aggregate and create the temperature hint text
        public String GetTemperatureHint()
        {
            return "Hello.";
        }
    }
}
