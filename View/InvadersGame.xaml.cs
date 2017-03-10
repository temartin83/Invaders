using Invaders.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Basic Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234237

namespace Invaders.View
{
    using Windows.UI.ApplicationSettings;
    using Windows.UI.Core;
    using Windows.UI.Popups;
    /// <summary>
    /// A basic page that provides characteristics common to most applications.
    /// </summary>
    public sealed partial class InvadersGame : Page
    {

        private NavigationHelper navigationHelper;
        private ObservableDictionary defaultViewModel = new ObservableDictionary();

        /// <summary>
        /// This can be changed to a strongly typed view model.
        /// </summary>
        public ObservableDictionary DefaultViewModel
        {
            get { return this.defaultViewModel; }
        }

        /// <summary>
        /// NavigationHelper is used on each page to aid in navigation and 
        /// process lifetime management
        /// </summary>
        public NavigationHelper NavigationHelper
        {
            get { return this.navigationHelper; }
        }


        public InvadersGame()
        {
            this.InitializeComponent();
            //this.navigationHelper = new NavigationHelper(this);
            //this.navigationHelper.LoadState += navigationHelper_LoadState;
            //this.navigationHelper.SaveState += navigationHelper_SaveState;
            SettingsPane.GetForCurrentView().CommandsRequested += InvadersGame_CommandsRequested;
        }

        void InvadersGame_CommandsRequested(SettingsPane sender, SettingsPaneCommandsRequestedEventArgs args)
        {
            UICommandInvokedHandler invokedHandler = new UICommandInvokedHandler(AboutInvokedHandler);
            SettingsCommand aboutCommand = new SettingsCommand("About", "About Invaders!", invokedHandler);
            args.Request.ApplicationCommands.Add(aboutCommand);
        }

        void AboutInvokedHandler(IUICommand command)
        {
            viewModel.Paused = true;
            aboutPopup.IsOpen = true;
        }

        void ClosePopup(object sender, RoutedEventArgs e)
        {
            aboutPopup.IsOpen = false;
            viewModel.Paused = false;
        }

        /// <summary>
        /// Populates the page with content passed during navigation. Any saved state is also
        /// provided when recreating a page from a prior session.
        /// </summary>
        /// <param name="sender">
        /// The source of the event; typically <see cref="Common.NavigationHelper"/>
        /// </param>
        /// <param name="e">Event data that provides both the navigation parameter passed to
        /// <see cref="Frame.Navigate(Type, Object)"/> when this page was initially requested and
        /// a dictionary of state preserved by this page during an earlier
        /// session. The state will be null the first time a page is visited.</param>
        private void navigationHelper_LoadState(object sender, LoadStateEventArgs e)
        {
        }

        /// <summary>
        /// Preserves state associated with this page in case the application is suspended or the
        /// page is discarded from the navigation cache.  Values must conform to the serialization
        /// requirements of <see cref="Common.SuspensionManager.SessionState"/>.
        /// </summary>
        /// <param name="sender">The source of the event; typically <see cref="Common.NavigationHelper"/></param>
        /// <param name="e">Event data that provides an empty dictionary to be populated with
        /// serializable state.</param>
        private void navigationHelper_SaveState(object sender, SaveStateEventArgs e)
        {
        }

        #region NavigationHelper registration

        /// The methods provided in this section are simply used to allow
        /// NavigationHelper to respond to the page's navigation methods.
        /// 
        /// Page specific logic should be placed in event handlers for the  
        /// <see cref="Common.NavigationHelper.LoadState"/>
        /// and <see cref="Common.NavigationHelper.SaveState"/>.
        /// The navigation parameter is available in the LoadState method 
        /// in addition to page state preserved during an earlier session.

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            Window.Current.CoreWindow.KeyDown += KeyDownHandler;
            Window.Current.CoreWindow.KeyUp += KeyUpHandler;
            base.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            Window.Current.CoreWindow.KeyDown -= KeyDownHandler;
            Window.Current.CoreWindow.KeyUp -= KeyUpHandler;
            base.OnNavigatedFrom(e);
        }

        void KeyDownHandler(object sender, KeyEventArgs e)
        {
            viewModel.KeyDown(e.VirtualKey);
        }

        void KeyUpHandler(object sender, KeyEventArgs e)
        {
            viewModel.KeyUp(e.VirtualKey);
        }

        #endregion

        void playArea_Loaded(object sender, RoutedEventArgs e)
        {
            UpdatePlayAreaSize(playArea.RenderSize);
            // Don't bind the game over StackPanel to the GameOver property of the ViewModel until the playArea
            // is loaded.  This prevents the "Game Over" TextBlock and the "Start Game" button from appearing
            // before the scanlines have been drawn on the PlayArea.
            Binding gameOverBinding = new Binding()
            {
                Path = new PropertyPath("GameOver"),
                Converter = new ViewModel.BooleanVisibilityConverter(),
            };
            gameOverStack.SetBinding(StackPanel.VisibilityProperty, gameOverBinding);
        }
        void pageRoot_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdatePlayAreaSize(new Size(e.NewSize.Width, e.NewSize.Height - 160));
        }

        void UpdatePlayAreaSize(Size newPlayAreaSize)
        {
            double targetWidth;
            double targetHeight;
            if (newPlayAreaSize.Width > newPlayAreaSize.Height)
            {
                targetWidth = newPlayAreaSize.Height * 4 / 3;
                targetHeight = newPlayAreaSize.Height;
                double leftRightMargin = (newPlayAreaSize.Width - targetWidth) / 2;
                playArea.Margin = new Thickness(leftRightMargin, 0, leftRightMargin, 0);
            }
            else
            {
                targetHeight = newPlayAreaSize.Width * 3 / 4;
                targetWidth = newPlayAreaSize.Width;
                double topBottomMargin = (newPlayAreaSize.Height - targetHeight) / 2;
                playArea.Margin = new Thickness(0, topBottomMargin, 0, topBottomMargin);
            }
            playArea.Width = targetWidth;
            playArea.Height = targetHeight;
            viewModel.PlayAreaSize = playArea.RenderSize;
        }

        void pageRoot_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            if (e.Delta.Translation.X < -1)
                viewModel.LeftGestureStarted();
            else if (e.Delta.Translation.X > 1)
                viewModel.RightGestureStarted();
        }

        void pageRoot_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            viewModel.LeftGestureCompleted();
            viewModel.RightGestureCompleted();
        }

        void pageRoot_Tapped(object sender, TappedRoutedEventArgs e)
        {
            viewModel.Tapped();
        }

        private void startButton_Click(object sender, RoutedEventArgs e)
        {
            aboutPopup.IsOpen = false;
            viewModel.StartGame();
        }
    }
}
