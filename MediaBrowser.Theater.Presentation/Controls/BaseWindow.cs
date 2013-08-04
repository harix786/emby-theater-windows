﻿using MediaBrowser.Theater.Interfaces.Navigation;
using MediaBrowser.Theater.Interfaces.Presentation;
using MediaBrowser.Theater.Interfaces.UserInput;
using System;
using System.ComponentModel;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace MediaBrowser.Theater.Presentation.Controls
{
    /// <summary>
    /// Provides a base class for all Windows
    /// </summary>
    public abstract class BaseWindow : Window, INotifyPropertyChanged
    {
        protected INavigationService NavigationManager { get; private set; }
        protected IUserInputManager UserInputManager { get; private set; }

        private Timer _activityTimer;

        /// <summary>
        /// Occurs when [property changed].
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseWindow" /> class.
        /// </summary>
        protected BaseWindow(IUserInputManager userInputManager, INavigationService nav)
            : base()
        {
            UserInputManager = userInputManager;
            NavigationManager = nav;

            SizeChanged += MainWindow_SizeChanged;
            Loaded += BaseWindow_Loaded;
        }

        void NavigationManager_Navigated(object sender, NavigationEventArgs e)
        {
            if (e.NewPage is IFullscreenVideoPage)
            {
                Dispatcher.InvokeAsync(() => OnPropertyChanged("IsMouseIdle"), DispatcherPriority.Background);

                UserInputManager.MouseMove += _userInput_MouseMove;
            }
        }

        void BaseWindow_Loaded(object sender, RoutedEventArgs e)
        {
            _activityTimer = new Timer(TimerCallback, null, 100, 100);
            NavigationManager.Navigated += NavigationManager_Navigated;
        }

        /// <summary>
        /// Called when [property changed].
        /// </summary>
        /// <param name="info">The info.</param>
        public void OnPropertyChanged(String info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }

        /// <summary>
        /// The _content scale
        /// </summary>
        private double _contentScale = 1;
        /// <summary>
        /// Gets the content scale.
        /// </summary>
        /// <value>The content scale.</value>
        public double ContentScale
        {
            get { return _contentScale; }
            private set
            {
                _contentScale = value;
                OnPropertyChanged("ContentScale");
            }
        }

        private DateTime _lastMouseInput;

        /// <summary>
        /// The _is mouse idle
        /// </summary>
        private bool _isMouseIdle = true;
        /// <summary>
        /// Gets or sets a value indicating whether this instance is mouse idle.
        /// </summary>
        /// <value><c>true</c> if this instance is mouse idle; otherwise, <c>false</c>.</value>
        public bool IsMouseIdle
        {
            get { return _isMouseIdle; }
            set
            {
                var changed = _isMouseIdle != value;

                _isMouseIdle = value;

                if (changed)
                {
                    Dispatcher.InvokeAsync(() =>
                    {
                        Cursor = value ? Cursors.None : Cursors.Arrow;

                        OnPropertyChanged("IsMouseIdle");

                    }, DispatcherPriority.Background);
                }
            }
        }

        /// <summary>
        /// The _last mouse move point
        /// </summary>
        private Point _lastMouseMovePoint;

        /// <summary>
        /// Handles OnMouseMove to auto-select the item that's being moused over
        /// </summary>
        /// <param name="e">Provides data for <see cref="T:System.Windows.Input.MouseEventArgs" />.</param>
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            // Store the last position for comparison purposes
            // Even if the mouse is not moving this event will fire as elements are showing and hiding
            var pos = e.GetPosition(this);

            if (pos == _lastMouseMovePoint)
            {
                return;
            }

            _lastMouseMovePoint = pos;

            OnMouseMove();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            NavigationManager.Navigated -= NavigationManager_Navigated;
            UserInputManager.MouseMove -= _userInput_MouseMove;
            DisposeActivityTimer();

            base.OnClosing(e);
        }

        void _userInput_MouseMove(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            OnMouseMove();
        }

        private void OnMouseMove()
        {
            _lastMouseInput = DateTime.Now;
            IsMouseIdle = false;
        }

        private void TimerCallback(object state)
        {
            IsMouseIdle = (DateTime.Now - _lastMouseInput).TotalMilliseconds > 5000;
        }

        /// <summary>
        /// Disposes the Activity Timer
        /// </summary>
        public void DisposeActivityTimer()
        {
            if (_activityTimer != null)
            {
                _activityTimer.Dispose();
            }
        }

        /// <summary>
        /// Handles the SizeChanged event of the MainWindow control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="SizeChangedEventArgs" /> instance containing the event data.</param>
        void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ContentScale = e.NewSize.Height / 1080;
        }

        /// <summary>
        /// Called when [browser back].
        /// </summary>
        protected virtual void OnBrowserBack()
        {

        }

        /// <summary>
        /// Called when [browser forward].
        /// </summary>
        protected virtual void OnBrowserForward()
        {

        }

        /// <summary>
        /// Invoked when an unhandled <see cref="E:System.Windows.Input.Keyboard.PreviewKeyDown" /> attached event reaches an element in its route that is derived from this class. Implement this method to add class handling for this event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.Windows.Input.KeyEventArgs" /> that contains the event data.</param>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (IsBackPress(e))
            {
                e.Handled = true;

                if (!e.IsRepeat)
                {
                    OnBrowserBack();
                }
            }

            else if (IsForwardPress(e))
            {
                e.Handled = true;

                if (!e.IsRepeat)
                {
                    OnBrowserForward();
                }
            }
            base.OnKeyDown(e);
        }

        /// <summary>
        /// Determines if a keypress should be treated as a backward press
        /// </summary>
        /// <param name="e">The <see cref="KeyEventArgs" /> instance containing the event data.</param>
        /// <returns><c>true</c> if [is back press] [the specified e]; otherwise, <c>false</c>.</returns>
        private bool IsBackPress(KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                return true;
            }

            if (e.Key == Key.BrowserBack || e.Key == Key.Back)
            {
                return true;
            }

            if (e.SystemKey == Key.Left && (e.KeyboardDevice.Modifiers & ModifierKeys.Alt) == ModifierKeys.Alt)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Determines if a keypress should be treated as a forward press
        /// </summary>
        /// <param name="e">The <see cref="KeyEventArgs" /> instance containing the event data.</param>
        /// <returns><c>true</c> if [is forward press] [the specified e]; otherwise, <c>false</c>.</returns>
        private bool IsForwardPress(KeyEventArgs e)
        {
            if (e.Key == Key.BrowserForward)
            {
                return true;
            }

            if (e.SystemKey == Key.Right && (e.KeyboardDevice.Modifiers & ModifierKeys.Alt) == ModifierKeys.Alt)
            {
                return true;
            }

            return false;
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.XButton1)
            {
                e.Handled = true;
                OnBrowserBack();
            }
            else if (e.ChangedButton == MouseButton.XButton2)
            {
                e.Handled = true;
                OnBrowserForward();
            }
            else
            {
                base.OnMouseUp(e);
            }
        }
    }
}