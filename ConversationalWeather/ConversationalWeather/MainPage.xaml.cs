using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.ViewManagement;
using Windows.UI.Notifications;
using Windows.Storage;
using ConversationalWeather.Classes;

namespace ConversationalWeather
{
    /// <summary>
    /// This is the main page showing the main weather text
    /// </summary>
    public sealed partial class MainPage : Page
    {
        // weather api class
        WeatherInterface weatherApi = new WeatherInterface();

        // weather api content transformation
        WeatherTransformator weatherTransformator = new WeatherTransformator();

        // dictionary that contains all the weather icons
        Dictionary<int, Image> weatherIconDictionary = new Dictionary<int, Image>();

        // application settings container
        ApplicationDataContainer applicationSettings = ApplicationData.Current.LocalSettings;

        // this will hold the main weather icon that is then used in the active tile
        string mainWeatherIcon = "";

        // flag that all components have been initialised
        bool componentInitialisationDone = false;

        public MainPage()
        {
            // initialise component
            this.InitializeComponent();

            // set temperature unit according to local configuration
            weatherApi.useCelsius = Convert.ToBoolean(applicationSettings.Values["useCelsius"]);
            toggleTemperatureUnit.IsOn = weatherApi.useCelsius;

            // add callback events for weather api
            weatherApi.PropertyChanged += WeatherApiPropertyChanged;

            // create dictionary for images
            // this is basically a fake list
            weatherIconDictionary.Add(0, imageWeatherIcon1);
            weatherIconDictionary.Add(1, imageWeatherIcon2);
            weatherIconDictionary.Add(2, imageWeatherIcon3);
            weatherIconDictionary.Add(3, imageWeatherIcon4);

            // add double tap event to switch page
            panelColumn0.DoubleTapped += new DoubleTappedEventHandler(this.LoadWeatherData);

            // respond to dynamic changes in user interaction mode and window size changes
            Window.Current.SizeChanged += new WindowSizeChangedEventHandler(this.AdaptUIElements);

            this.AdaptUIElements(null, null);

            // load the weather data
            this.LoadWeatherData(null, null);

            // initialisation done
            componentInitialisationDone = true;
        }

        private void AdaptUIElements(object sender, WindowSizeChangedEventArgs e)
        {
            // set icon size to quarter of the screen width
            Windows.Foundation.Rect bounds = ApplicationView.GetForCurrentView().VisibleBounds;

            // check for user interaction mode
            switch (UIViewSettings.GetForCurrentView().UserInteractionMode)
            {
                // when mouse is returned, use desktop layout
                case UserInteractionMode.Mouse:
                    // set icon size
                    imageWeatherIcon1.Width = ((bounds.Width - 30) / 13.5);
                    imageWeatherIcon2.Width = ((bounds.Width - 30) / 13.5);
                    imageWeatherIcon3.Width = ((bounds.Width - 30) / 13.5);
                    imageWeatherIcon4.Width = ((bounds.Width - 30) / 13.5);

                    // set column sizes
                    panelColumn0.Width = bounds.Width / 3;
                    panelColumn1.Width = bounds.Width / 3;
                    panelColumn2.Width = bounds.Width / 3;

                    // set scroll modes
                    scrollMainContainer.HorizontalScrollMode = ScrollMode.Disabled;
                    scrollMainContainer.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
                    scrollMainContainer.VerticalScrollMode = ScrollMode.Disabled;
                    scrollMainContainer.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
                    break;

                // when touch is returned, use mobile layout
                case UserInteractionMode.Touch:
                default:
                    // set icon size
                    imageWeatherIcon1.Width = ((bounds.Width - 30) / 4.5);
                    imageWeatherIcon2.Width = ((bounds.Width - 30) / 4.5);
                    imageWeatherIcon3.Width = ((bounds.Width - 30) / 4.5);
                    imageWeatherIcon4.Width = ((bounds.Width - 30) / 4.5);

                    // set column sizes
                    panelColumn0.Width = bounds.Width;
                    panelColumn1.Width = bounds.Width;
                    panelColumn2.Width = bounds.Width;

                    // set scroll modes
                    scrollMainContainer.HorizontalScrollMode = ScrollMode.Enabled;
                    scrollMainContainer.HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden;
                    scrollMainContainer.VerticalScrollMode = ScrollMode.Disabled;
                    scrollMainContainer.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
                    break;
            }
        }

        // trigger to reload the weather data
        // this will also take care of resetting the UI
        public void LoadWeatherData(object sender, DoubleTappedRoutedEventArgs e)
        {
            // hide UI and show loader
            panelColumn1.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
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
            textLocationInformation.Text = weatherTransformator.GetLocationInformation(weatherApi.WeatherForecast);

            // display the aggregated weather summary
            textWeatherForecast.Text = weatherTransformator.GetWeatherForecastText(weatherApi.WeatherForecast);

            // display temperature information
            textTemperatureInformation.Text = weatherTransformator.GetTemperatureInformation(weatherApi.WeatherForecast, weatherApi.useCelsius);

            // display temperature hint
            textWeatherCondition.Text = weatherTransformator.GetConditionHint(weatherApi.WeatherForecast, weatherApi.useCelsius);

            // loop through all found weather states and select matching icon
            int i = 0;
            foreach (KeyValuePair<string, int> pair in weatherTransformator.weatherForecastStates)
            {
                // the first 4 items should display a matching icon
                if (i < 4)
                {
                    // get icon name and load icon into component
                    string iconName = weatherTransformator.GetConditionIcon(pair.Value);
                    this.loadImageIntoComponent(iconName, weatherIconDictionary[i]);
                }

                // increase the counter
                i++;
            }

            // get and load hero icon
            string heroIconName = weatherTransformator.GetHeroConditionIcon(weatherApi.WeatherForecast.list[0].weather[0].id);
            this.loadImageIntoComponent(heroIconName, imageWeatherCondition);

            // update active tile with new data
            this.UpdateActiveTile();

            // hide the loader and show UI elements for data
            panelContent.Visibility = Windows.UI.Xaml.Visibility.Visible;
            panelLoadingProgress.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            panelColumn1.Visibility = Windows.UI.Xaml.Visibility.Visible;
        }

        // this will load a weather icon
        // the weather code will contain the code from openweathermap
        // the icon index is the reference to the weather icon dictionary
        // recursion is a flag to avoid infinite loops by indicating if the icon has been called recusively or not
        async public void loadImageIntoComponent(string imageUri, Image imageComponent)
        {
            try
            {
                // try to load the file
                // note that if the file does not exist, an exception will be thrown
                StorageFile item = await StorageFile.GetFileFromApplicationUriAsync(new Uri(this.BaseUri, imageUri));

                // here we can assume that the file does exist
                // we create a bitmap with the icon and load it into the respective image icon slot
                // btw. the following link helped me as some namespaces are different for UWP than standard windows / WPF
                // https://msdn.microsoft.com/en-us/windows/uwp/porting/wpsl-to-uwp-namespace-and-class-mappings
                BitmapImage bitmapImage = new BitmapImage(new Uri(this.BaseUri, imageUri));
                imageComponent.Source = bitmapImage;
            }
            catch (FileNotFoundException ex)
            {
            }
        }

        // update the live tile with updated data
        async public void UpdateActiveTile()
        {
            // get data from xml tile template
            StorageFile xmlDocument = await StorageFile.GetFileFromApplicationUriAsync(new Uri(this.BaseUri, "/Assets/ActiveTile/ActiveTileTemplate.xml"));
            string xmlDocumentContent = FileIO.ReadTextAsync(xmlDocument).AsTask().ConfigureAwait(false).GetAwaiter().GetResult();

            // set the current main weather state icon
            xmlDocumentContent = xmlDocumentContent.Replace("{{TileWeatherStateIcon}}", mainWeatherIcon);

            // build temperature string
            // note that we change the temperature unit sign according to the chosen one
            string temperatureUnitSign = "°F";
            if (weatherApi.useCelsius) temperatureUnitSign = "°C";
            string temperatureText = Convert.ToInt16(weatherApi.WeatherForecast.list[0].main.temp) + temperatureUnitSign;
            xmlDocumentContent = xmlDocumentContent.Replace("{{TileCurrentTemperature}}", temperatureText);

            // set the current weather forecast text
            xmlDocumentContent = xmlDocumentContent.Replace("{{TileForecast}}", weatherTransformator.GetConditionHint(weatherApi.WeatherForecast, weatherApi.useCelsius));

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
            if (componentInitialisationDone)
            {
                // set temperature flag accordingly
                weatherApi.useCelsius = toggleTemperatureUnit.IsOn;

                // store flag in local storage
                applicationSettings.Values["useCelsius"] = toggleTemperatureUnit.IsOn;
            }
        }

        // author information label was tapped
        async private void labelAboutAuthor_Tapped(object sender, TappedRoutedEventArgs e)
        {
            // set url and launch app registered with handling URIs
            Uri uriAuthor = new Uri(@"http://dirk.songuer.de");
            bool launcherState = await Windows.System.Launcher.LaunchUriAsync(uriAuthor);
        }

        // application information label was tapped
        async private void labelAboutApplication_Tapped(object sender, TappedRoutedEventArgs e)
        {
            // set url and launch app registered with handling URIs
            Uri uriAuthor = new Uri(@"https://github.com/DirkSonguer/UWPConversationalWeather");
            bool launcherState = await Windows.System.Launcher.LaunchUriAsync(uriAuthor);
        }
    }
}
