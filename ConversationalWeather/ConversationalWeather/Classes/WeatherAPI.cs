using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Geolocation;
using Windows.Web.Http;
using Windows.Web.Http.Filters;
using Newtonsoft.Json;
using ConversationalWeather.Objects;

namespace ConversationalWeather.Classes
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
        public bool geopositionKnown = false;

        // flag to indicate that weather data is known
        public bool weatherDataLoaded = false;

        // flag to indicate temperature units
        public bool useCelsius = true;

        // contains the found geolocation
        private Geoposition _currentPosition;

        // contains the status of the geolocation tracking
        private string _status = "";

        // object containing the returned weather data
        private WeatherForecastObject _weatherForecast = new WeatherForecastObject();

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
        public string Status
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

                        // note that we do not be super accurate as we basically just need the city we're in
                        Geolocator geolocator = new Geolocator { DesiredAccuracyInMeters = 500 };

                        // try to get the geolocation
                        // if successful this will change the CurrentPosition property
                        // note that we can also use old data since we don't expect users to travel within cities in 5 minutes
                        CurrentPosition = await geolocator.GetGeopositionAsync(maximumAge: TimeSpan.FromMinutes(5), timeout: TimeSpan.FromSeconds(10)).AsTask(token);
                        break;

                    // if the user denied the access
                    case GeolocationAccessStatus.Denied:
                        geopositionKnown = false;
                        Status = "It seems like access to the location services denied. Please activate the location services for this app, since they are needed to display weather data relevant to where you are.\nDouble tap to try again.";
                        break;

                    // if something else went wrong
                    case GeolocationAccessStatus.Unspecified:
                        geopositionKnown = false;
                        Status = "Oh, an unspecified geolocation tracking error happened.\nDouble tap to try again.";
                        break;
                }
            }
            // user cancelled request
            catch (TaskCanceledException)
            {
                geopositionKnown = false;
                Status = "Geolocation tracking cancelled.\nDouble tap to try again.";
            }
            // exception was caught
            catch (Exception ex)
            {
                geopositionKnown = false;
                Status = "Oh, a general error happened while tracking your geolocation: " + ex.ToString() + "\nDouble tap to try again.";
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
                }
                else
                {
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
                Status = "A problem occured during loading and converting the weather data. Please make sure you are connected to the internet.\nDouble tap to try again.";
            }
        }
    }
}
