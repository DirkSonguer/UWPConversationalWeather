using System;
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

        // contains the found geolocation
        private Geoposition _currentPosition;

        // cotnains the status of the geolocation tracking
        private String _status = "";

        // object containing the returned weather data
        private WeatherForecastObject _weatherForecast = new WeatherForecastObject();

        // constructor
        public WeatherInterface()
        {
            geopositionKnown = false;
            weatherDataLoaded = false;

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
                // we have a geolocation locked now
                geopositionKnown = true;

                // store the data
                _currentPosition = value;

                // load weather data from server
                this.GetWeatherForcastForGeoposition();

                // call the OnPropertyChanged callback to indicate new data
                this.OnPropertyChanged("CurrentPosition");
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

        // setter / getter for the weather data response
        public WeatherForecastObject WeatherForecast
        {
            get { return _weatherForecast; }
            set
            {
                // we have weather data now
                weatherDataLoaded = true;

                // store the data
                _weatherForecast = value;

                // call the OnPropertyChanged callback to indicate new data
                this.OnPropertyChanged("WeatherForecast");
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
                Status = "Trying to get your location";

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

                        // try to get the geolocation and set it to the property
                        CurrentPosition = await geolocator.GetGeopositionAsync().AsTask(token);
                        Status = "Ok, found your position";
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
            try
            {
#if DEBUG
                String weatherApiUrl = "http://localhost:8888/forecast.json";
#else
                // create URL for getting forcast
                String weatherApiUrl = "http://api.openweathermap.org/data/2.5/forecast";
                weatherApiUrl += "?lat=" + CurrentPosition.Coordinate.Latitude.ToString();
                weatherApiUrl += "&lon=" + CurrentPosition.Coordinate.Longitude.ToString();
                weatherApiUrl += "&units=metric";
                weatherApiUrl += "&APPID=60c2636cd48cde2176af74be0d397d00";
#endif

                // set status to loading
                Status = "Loading weather data";

                // load weather forcast data
                Uri myUri = new Uri(weatherApiUrl, UriKind.Absolute);
                HttpResponseMessage response = await httpClient.GetAsync(myUri).AsTask(_cts.Token);

                // convert json data to object
                WeatherForecast = JsonConvert.DeserializeObject<WeatherForecastObject>(response.Content.ToString());
            }
            // exception was caught
            catch (Exception ex)
            {
                Status = "A problem occured during loading and converting the weather data: " + ex.ToString();
            }
        }
    }
}
