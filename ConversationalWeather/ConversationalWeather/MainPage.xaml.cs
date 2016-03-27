using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.ViewManagement;
using ConversationalWeather.WeatherAPI;
using Windows.Storage;

// Die Vorlage "Leere Seite" ist unter http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409 dokumentiert.

namespace ConversationalWeather
{
    /// <summary>
    /// Eine leere Seite, die eigenständig verwendet oder zu der innerhalb eines Rahmens navigiert werden kann.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        WeatherInterface weatherApi = new WeatherInterface();

        Dictionary<int, Image> weatherIconDictionary = new Dictionary<int, Image>();

        public MainPage()
        {
            // initialise component
            this.InitializeComponent();

            // add callback events for weather api
            weatherApi.PropertyChanged += weatherApiPropertyChanged;

            // create dictionary for images
            // this is basically a fake list
            weatherIconDictionary.Add(1, imageWeatherIcon1);
            weatherIconDictionary.Add(2, imageWeatherIcon2);
            weatherIconDictionary.Add(3, imageWeatherIcon3);
            weatherIconDictionary.Add(4, imageWeatherIcon4);

            // set icon size to quarter of the screen width
            Windows.Foundation.Rect bounds = ApplicationView.GetForCurrentView().VisibleBounds;
            imageWeatherIcon1.Width = (bounds.Width / 4.5);
            imageWeatherIcon2.Width = (bounds.Width / 4.5);
            imageWeatherIcon3.Width = (bounds.Width / 4.5);
            imageWeatherIcon4.Width = (bounds.Width / 4.5);

            // get current geolocation
            // note that his will trigger a weather forecast for this location
            weatherApi.GetCurrentLocation();
        }

        public void weatherApiPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // position was found
            if (e.PropertyName == "CurrentPosition")
                GeopositionFound();

            // forecast was load
            if (e.PropertyName == "WeatherForecast")
                WeatherForecastLoaded();

            // status was changed
            if (e.PropertyName == "Status")
                StatusChanged();
        }

        public void GeopositionFound()
        {
            // textWeatherInformation.Text = "Loading some weather information";
        }

        // weather data was received
        // it's now available in weatherApi.WeatherForecast
        public void WeatherForecastLoaded()
        {
            // show location string
            textLocationInformation.Text = "Seems like you're in " + weatherApi.WeatherForecast.city.name.ToString();
            String weatherSummary = "There will be ";

            // get dictionary to fill all forecasted weather states
            Dictionary<string, int> weatherDictionary = new Dictionary<string, int>();
            // iterate through all forecasted weather nodes
            for (int i = 0; i < weatherApi.WeatherForecast.list.Count; i++)
            {
                // check if the weather state is already known
                if (!weatherDictionary.ContainsKey(weatherApi.WeatherForecast.list[i].weather[0].description.ToString()))
                {
                    // if not known yet, add the weather state to the dictionary
                    weatherDictionary.Add(weatherApi.WeatherForecast.list[i].weather[0].description.ToString(), weatherApi.WeatherForecast.list[i].weather[0].id);
                }
            }

            // create a local counter
            int j = 1;

            // loop through all found weather states
            foreach (KeyValuePair<string, int> pair in weatherDictionary)
            {
                // increase the counter
                // note that this runs one in front of the item count
                j++;

                // store the weather state in the summary string
                weatherSummary += pair.Key;

                // check if this is an item in the middle
                if (j < weatherDictionary.Count)
                {
                    weatherSummary += ", ";
                }
                // or the last item in the dictionary
                else if (j == weatherDictionary.Count)
                {
                    weatherSummary += " and then ";
                }

                // the first 4 items should display a matching icon
                if (j <= 5)
                {
                    this.selectWeatherIcon(pair.Value.ToString(), (j - 1), false);
                }
            }

            // display the aggregated weather summary
            textWeatherInformation.Text = weatherSummary + " later.";
        }

        // this will load a weather icon
        // the weather code will contain the code from openweathermap
        // the icon index is the reference to the weather icon dictionary
        // recursion is a flag to avoid infinite loops by indicating if the icon has been called recusively or not
        async public void selectWeatherIcon(String weatherCode, int weatherIconIndex, Boolean recursion)
        {
            try
            {
                // try to load the file
                // note that if the file does not exist, an exception will be thrown
                StorageFile item = await StorageFile.GetFileFromApplicationUriAsync(new Uri(this.BaseUri, "/Assets/WeatherIcons/" + weatherCode + ".png"));

                // here we can assume that the file does exist
                // we create a bitmap with the icon and load it into the respective image icon slot 
                // https://msdn.microsoft.com/en-us/windows/uwp/porting/wpsl-to-uwp-namespace-and-class-mappings
                BitmapImage bitmapImage = new BitmapImage(new Uri(this.BaseUri, "/Assets/WeatherIcons/" + weatherCode + ".png"));
                weatherIconDictionary[weatherIconIndex].Source = bitmapImage;
            }
            catch (FileNotFoundException ex)
            {
                if (!recursion)
                {
                    DateTime currentTime = DateTime.Now;

                    if ((currentTime.Hour > 8) && (currentTime.Hour < 20))
                    {
                        this.selectWeatherIcon(weatherCode + "_day", weatherIconIndex, true);
                    }
                    else
                    {
                        this.selectWeatherIcon(weatherCode + "_night", weatherIconIndex, true);
                    }
                }
                else
                {
                    string[] stringSeparators = new string[] { "_" };
                    string[] result;

                    // Display the original string and delimiter string.
                    // Split a string delimited by another string and return all elements.
                    result = weatherCode.Split(stringSeparators, StringSplitOptions.None);

                    int rawWeatherCode = Convert.ToInt16(result[0]);
                    int baseWeatherCode = Convert.ToInt16(rawWeatherCode / 100) * 100;

                    if (baseWeatherCode != rawWeatherCode)
                    {
                        this.selectWeatherIcon(baseWeatherCode.ToString(), weatherIconIndex, false);
                    }
                }
            }
        }

        public void StatusChanged()
        {
            textLocationInformation.Text = weatherApi.Status;
        }

    }
}
