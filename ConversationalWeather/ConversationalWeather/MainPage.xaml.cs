using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.ViewManagement;
using Windows.UI.Notifications;
using ConversationalWeather.WeatherAPI;
using Windows.Storage;

namespace ConversationalWeather
{
    /// <summary>
    /// This is the main page showing the main weather text
    /// </summary>
    public sealed partial class MainPage : Page
    {
        // weather api class singleton
        WeatherInterface weatherApi = new WeatherInterface();

        // dictionary that contains all the weather icons
        Dictionary<int, Image> weatherIconDictionary = new Dictionary<int, Image>();

        // this will hold the main weather icon that is then used in the active tile
        String mainWeatherIcon = "";

        public MainPage()
        {
            // initialise component
            this.InitializeComponent();

            // add callback events for weather api
            weatherApi.PropertyChanged += WeatherApiPropertyChanged;

            // create dictionary for images
            // this is basically a fake list
            weatherIconDictionary.Add(0, imageWeatherIcon1);
            weatherIconDictionary.Add(1, imageWeatherIcon2);
            weatherIconDictionary.Add(2, imageWeatherIcon3);
            weatherIconDictionary.Add(3, imageWeatherIcon4);

            // set icon size to quarter of the screen width
            Windows.Foundation.Rect bounds = ApplicationView.GetForCurrentView().VisibleBounds;
            imageWeatherIcon1.Width = (bounds.Width / 4.5);
            imageWeatherIcon2.Width = (bounds.Width / 4.5);
            imageWeatherIcon3.Width = (bounds.Width / 4.5);
            imageWeatherIcon4.Width = (bounds.Width / 4.5);

            // add double tap event to switch page
            pageGrid.DoubleTapped += new DoubleTappedEventHandler(this.LoadWeatherData);

            // load the weather data
            this.LoadWeatherData(null, null);
        }

        // trigger to reload the weather data
        // this will also take care of resetting the UI
        public void LoadWeatherData(object sender, DoubleTappedRoutedEventArgs e)
        {
            // hide UI and show loader
            rootPivot.Items.Remove(pivotTemperatureItem);
            panelContent.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            panelLoadingProgress.Visibility = Windows.UI.Xaml.Visibility.Visible;

            // first get current geolocation
            // note that his will trigger a weather forecast for this location
            weatherApi.GetCurrentLocation();
        }

        // weather data has changed
        public void WeatherApiPropertyChanged(object sender, PropertyChangedEventArgs e)
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
            // load weather data from server
            weatherApi.GetWeatherForcastForGeoposition();
        }

        // weather data was received
        // it's now available in weatherApi.WeatherForecast
        public void WeatherForecastLoaded()
        {
            // display location string
            textLocationInformation.Text = weatherApi.GetLocationInformation();

            // display the aggregated weather summary
            textWeatherForecast.Text = weatherApi.GetWeatherForecastText();

            // display temperature information
            textTemperatureInformation.Text = weatherApi.GetTemperatureInformation();

            // display temperature hint
            textTemperatureHint.Text = weatherApi.GetTemperatureHint();

            // loop through all found weather states and select matching icon
            int i = 0;
            foreach (KeyValuePair<string, int> pair in weatherApi.weatherForecastStates)
            {
                // the first 4 items should display a matching icon
                if (i < 4)
                {
                    this.selectWeatherIcon(pair.Value.ToString(), (i), false);
                }

                // increase the counter
                i++;
            }

            // update active tile with new data
            this.UpdateActiveTile();

            BitmapImage bitmapImage = new BitmapImage(new Uri(this.BaseUri, mainWeatherIcon));
            imageTemperatureIcon1.Source = bitmapImage;

            // hide the loader and show UI elements for data
            panelContent.Visibility = Windows.UI.Xaml.Visibility.Visible;
            panelLoadingProgress.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            rootPivot.Items.Insert(1, pivotTemperatureItem);
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
                // btw. the following link helped me as some namespaces are different for UWP than standard windows / WFP
                // https://msdn.microsoft.com/en-us/windows/uwp/porting/wpsl-to-uwp-namespace-and-class-mappings
                BitmapImage bitmapImage = new BitmapImage(new Uri(this.BaseUri, "/Assets/WeatherIcons/" + weatherCode + ".png"));
                weatherIconDictionary[weatherIconIndex].Source = bitmapImage;

                // set main weather icon
                if (weatherIconIndex == 0)
                {
                    mainWeatherIcon = "/Assets/WeatherIcons/" + weatherCode + ".png";
                }
            }
            catch (FileNotFoundException ex)
            {
                // this means the image file for the weather state could not be found
                // first check if this happened before
                if (!recursion)
                {
                    // if not, it might be that the weather state has different icons for day / night
                    // for this, we can check the current time
                    DateTime currentTime = DateTime.Now;

                    // based on the time, we can choose the day / night version
                    if ((currentTime.Hour > 8) && (currentTime.Hour < 20))
                    {
                        // load day version
                        // note the recursion flag
                        this.selectWeatherIcon(weatherCode + "_day", weatherIconIndex, true);
                    }
                    else
                    {
                        // load night version
                        // note the recursion flag
                        this.selectWeatherIcon(weatherCode + "_night", weatherIconIndex, true);
                    }
                }
                else
                {
                    // this means that no icon exists or the weather state
                    // and we also checked for day / night versions
                    // in this case, we can fall back to the base states
                    // example: 812 -> 800
                    // we start by removing any day / night identifiers
                    string[] stringSeparators = new string[] { "_" };
                    string[] result;
                    result = weatherCode.Split(stringSeparators, StringSplitOptions.None);

                    // convert to base state
                    int rawWeatherCode = Convert.ToInt16(result[0]);
                    int baseWeatherCode = Convert.ToInt16(rawWeatherCode / 100) * 100;

                    // check if the code has changed
                    // this will make sure that we really found a new base code and will not try to load the same file again
                    if (baseWeatherCode != rawWeatherCode)
                    {
                        // try loading the new base code
                        // also note the recursion flag
                        this.selectWeatherIcon(baseWeatherCode.ToString(), weatherIconIndex, false);
                    }
                }
            }
        }

        // update the live tile with updated data
        async public void UpdateActiveTile()
        {
            // get data from xml tile template
            StorageFile xmlDocument = await StorageFile.GetFileFromApplicationUriAsync(new Uri(this.BaseUri, "/Assets/ActiveTile/ActiveTileTemplate.xml"));
            String xmlDocumentContent = FileIO.ReadTextAsync(xmlDocument).AsTask().ConfigureAwait(false).GetAwaiter().GetResult();

            // set the current main weather state icon
            xmlDocumentContent = xmlDocumentContent.Replace("{{TileWeatherStateIcon}}", mainWeatherIcon);

            // build temperature string
            // note that we change the temperature unit sign according to the chosen one
            String temperatureUnitSign = "°F";
            if (weatherApi.useCelsius) temperatureUnitSign = "°C";
            String temperatureText = Convert.ToInt16(weatherApi.WeatherForecast.list[0].main.temp) + temperatureUnitSign;
            xmlDocumentContent = xmlDocumentContent.Replace("{{TileCurrentTemperature}}", temperatureText);

            // set the current weather forecast text
            xmlDocumentContent = xmlDocumentContent.Replace("{{TileForecast}}", weatherApi.GetTemperatureHint());

            // convert xml template (string) into a proper xml object
            var xmlTileData = new Windows.Data.Xml.Dom.XmlDocument();
            xmlTileData.LoadXml(xmlDocumentContent);

            // publish tile data to tile updater
            TileNotification tileNotification = new TileNotification(xmlTileData);
            TileUpdater tileUpdateManager = TileUpdateManager.CreateTileUpdaterForApplication();
            tileUpdateManager.Update(tileNotification);
        }

        // show status message
        // this will happen mostly during loading so we just update the loading status
        public void StatusChanged()
        {
            // set loading status text
            textLoadingStatus.Text = weatherApi.Status;
        }

        // temperature unit has changed
        private void ToggleTemperatureUnit_Toggled(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            // set temperature flag accordingly
            weatherApi.useCelsius = toggleTemperatureUnit.IsOn;
        }

        // current pivot item has changed
        private void RootPivot_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // set the background color brush of the root pivot to the background color of the current pivot item
            // this basically leads to a smooth transition from one color to the next when switching pivot items
            rootPivot.Background = (rootPivot.Items[rootPivot.SelectedIndex] as PivotItem).Background;
        }

        // author information label was tapped
        async private void labelAboutAuthor_Tapped(object sender, TappedRoutedEventArgs e)
        {
            // set url and launch app registered with handling URIs
            Uri uriAuthor = new Uri(@"http://dirk.songuer.de");
            Boolean success = await Windows.System.Launcher.LaunchUriAsync(uriAuthor);
        }
    }
}
