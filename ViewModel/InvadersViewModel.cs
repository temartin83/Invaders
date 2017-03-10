using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.System;

namespace Invaders.ViewModel
{
    using View;
    using Model;
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using DispatcherTimer = Windows.UI.Xaml.DispatcherTimer;
    using FrameworkElement = Windows.UI.Xaml.FrameworkElement;

    class InvadersViewModel : INotifyPropertyChanged
    {
        // sprite collection
        readonly ObservableCollection<FrameworkElement> _sprites = new ObservableCollection<FrameworkElement>();
        public INotifyCollectionChanged Sprites { get { return _sprites; } }

        public bool GameOver { get { return _model.GameOver; } }

        // lives icons in the upper right corner
        readonly ObservableCollection<object> _lives = new ObservableCollection<object>();
        public INotifyCollectionChanged Lives { get { return _lives; } }

        public bool Paused { get; set; }
        bool _lastPaused = true;
        public int Score { get; private set; }

        // scale settings for different screen sizes
        public static double Scale { get; private set; }
        public Size PlayAreaSize
        {
            set
            {
                Scale = value.Width / 405;
                _model.UpdateAllShipsAndStars();
                RecreateScanLines();
            }
        }

        // list of all ships
        List<Ship> _ships = new List<Ship>();
        // new model for game control
        InvadersModel _model = new InvadersModel();
        // timer control
        DispatcherTimer _timer = new DispatcherTimer();
        // player control
        FrameworkElement _playerControl = null;
        bool _playerFlashing = false;

        readonly Dictionary<Invader, FrameworkElement> _invaders = new Dictionary<Invader, FrameworkElement>();
        readonly Dictionary<FrameworkElement, DateTime> _shotInvaders = new Dictionary<FrameworkElement, DateTime>();
        readonly Dictionary<Shot, FrameworkElement> _shots = new Dictionary<Shot, FrameworkElement>();
        readonly Dictionary<Point, FrameworkElement> _stars = new Dictionary<Point, FrameworkElement>();
        readonly List<FrameworkElement> _scanLines = new List<FrameworkElement>();

        DateTime? _leftAction = null;
        DateTime? _rightAction = null;

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        internal void KeyDown(VirtualKey virtualKey)
        {
            if (virtualKey == VirtualKey.Space)
                _model.FireShot();
            if (virtualKey == VirtualKey.Left)
                _leftAction = DateTime.Now;

            if (virtualKey == VirtualKey.Right)
                _rightAction = DateTime.Now;
        }
        internal void KeyUp(VirtualKey virtualKey)
        {
            if (virtualKey == VirtualKey.Left)
                _leftAction = null;

            if (virtualKey == VirtualKey.Right)
                _rightAction = null;
        }
        internal void LeftGestureStarted()
        {
            _leftAction = DateTime.Now;
        }
        internal void LeftGestureCompleted()
        {
            _leftAction = null;
        }
        internal void RightGestureCompleted()
        {
            _rightAction = null;
        }
        internal void RightGestureStarted()
        {
            _rightAction = DateTime.Now;
        }
        internal void Tapped()
        {
            _model.FireShot();
        }
        public InvadersViewModel()
        {
            Scale = 1;
            _model.ShipChanged += ModelShipChangedEventHandler;
            _model.ShotMoved += ModelShotMovedHandler;
            _model.StarChanged += ModelStarChangedEventHandler;

            _timer.Interval = TimeSpan.FromMilliseconds(100);
            _timer.Tick += TimerTickEventHandler;

            EndGame();
        }
        public void StartGame()
        {
            Paused = false;
            
            // remove invaders ships
            foreach (var invader in _invaders.Values)
                _sprites.Remove(invader);
            // remove all shots
            foreach (var shot in _shots.Values)
                _sprites.Remove(shot);

            _model.StartGame();
            OnPropertyChanged("GameOver");
            _timer.Start();
        }
        public void EndGame()
        {
            _model.EndGame();
        }
        void TimerTickEventHandler(object sender, object e)

        {
            if (_lastPaused != Paused)
            {
                OnPropertyChanged("Paused");
                // use the _lastPaused field to fire a propertychanged event any time Paused changes
            }

            if (!Paused)
            {
                if (_leftAction.HasValue && _rightAction.HasValue)
                    if (_leftAction > _rightAction)
                        _model.MovePlayer(Direction.Left);
                    else
                        _model.MovePlayer(Direction.Right);
                else
                {
                    if (_leftAction.HasValue)
                        _model.MovePlayer(Direction.Left);
                    else if (_rightAction.HasValue)
                        _model.MovePlayer(Direction.Right);
                }
            }
            // update InvadersModel
            _model.Update(Paused);
            // update score to match
            if (Score != _model.Score)
            {
                Score = _model.Score;
                OnPropertyChanged("Score");
            }
            // update lives icons to match lives remaining
            if (_model.Lives >= 0)
            {
                while (_model.Lives > _lives.Count())
                    _lives.Add(new object());
                while (_model.Lives < _lives.Count())
                    _lives.RemoveAt(0);
            }

            foreach (FrameworkElement control in _shotInvaders.Keys.ToList())
            {
                if (DateTime.Now - _shotInvaders[control] >= TimeSpan.FromSeconds(0.5))
                {
                    _sprites.Remove(control);
                    _shotInvaders.Remove(control);
                }
            }

            if (_model.GameOver)
            {
                OnPropertyChanged("GameOver");
                _timer.Stop();
            }
        }
        void RecreateScanLines()
        {
            foreach (FrameworkElement scanLine in _scanLines)
                if (_sprites.Contains(scanLine))
                    _sprites.Remove(scanLine);
            _scanLines.Clear();
            for (int y = 0; y < 300; y+=2)
            {
                FrameworkElement scanLine = InvadersHelper.ScanLineFactory(y, 400, Scale);
                _scanLines.Add(scanLine);
                _sprites.Add(scanLine);
            }
        }
        void ModelStarChangedEventHandler(object sender, StarChangedEventArgs e)
        {
            if (e.Disappeared && _stars.ContainsKey(e.Point))
            {
                FrameworkElement starToRemove = _stars[e.Point];
                _sprites.Remove(starToRemove);
            }
            else
            {
                if (!_stars.ContainsKey(e.Point))
                {
                    FrameworkElement newStar = InvadersHelper.StarControlFactory(e.Point, Scale);
                    _stars[e.Point] = newStar;
                    _sprites.Add(newStar);
                }
            }
        }
        void ModelShipChangedEventHandler(object sender, ShipChangedEventArgs e)
        {
            if (!e.Killed)
            {
                if (e.ShipUpdated is Invader)
                {
                    Invader invader = e.ShipUpdated as Invader;

                    if (!_invaders.ContainsKey(invader))
                    {
                        FrameworkElement invaderControl = InvadersHelper.ShipControlFactory(invader, Scale);
                        _invaders[invader] = invaderControl;
                        _sprites.Add(invaderControl);
                    }
                    else
                    {
                        FrameworkElement invaderControl = _invaders[invader];
                        InvadersHelper.MoveElementOnCanvas(invaderControl,
                            invader.Location.X * Scale, invader.Location.Y * Scale);
                        InvadersHelper.ResizeElement(invaderControl, invader.Size.Width * Scale,
                                                invader.Size.Height * Scale);
                    }
                    // if _invaders doesn't contain this invader, use InvaderControlFactory() to create
                    // a new control and add it to the collection and to sprites.
                    // else move invader to correct location and resize it, passing Scale

                }
                else if (e.ShipUpdated is Player)
                {
                    if (_playerFlashing)
                    {
                        // if flashing, stop flashing. check if _playercontrol == null
                        // if it is, use PlayerControLFactory() to create player and add to sprites
                        // else, move and resize ship
                        AnimatedImage playerImage = _playerControl as AnimatedImage;
                        playerImage.StopFlashing();
                        _playerFlashing = false;
                    }
                    if (_playerControl == null)
                    {
                        _playerControl = InvadersHelper.ShipControlFactory(e.ShipUpdated as Player, Scale);
                        _sprites.Add(_playerControl);
                    }
                    else
                    {
                        InvadersHelper.MoveElementOnCanvas(_playerControl, e.ShipUpdated.Location.X * Scale,
                            e.ShipUpdated.Location.Y * Scale);
                        InvadersHelper.ResizeElement(_playerControl, e.ShipUpdated.Size.Width * Scale,
                            e.ShipUpdated.Size.Height * Scale);
                    }
                }
            }
            else
            {
                if (e.ShipUpdated is Invader)
                {
                    Invader killedInvader = e.ShipUpdated as Invader;

                    if (!_invaders.ContainsKey(killedInvader))
                        return;

                    AnimatedImage killedInvaderControl = _invaders[killedInvader] as AnimatedImage;

                    if (killedInvader != null)
                    {
                        killedInvaderControl.InvaderShot();
                        _shotInvaders[killedInvaderControl] = DateTime.Now;
                        _invaders.Remove(killedInvader);
                    }
                    // if invader isn't null, call invadershot, add it to _shotinvaders
                }
                else if (e.ShipUpdated is Player)
                {
                    AnimatedImage playerImage = _playerControl as AnimatedImage;
                    if (playerImage != null)
                        playerImage.StartFlashing();
                    _playerFlashing = true;
                }
            }
        }
        void ModelShotMovedHandler(object sender, ShotMovedEventArgs e)
        {
            if (!e.Disappeared)
            {
                if (!_shots.ContainsKey(e.Shot))
                {
                    FrameworkElement shotControl = InvadersHelper.ShotControlFactory(e.Shot, Scale);
                    _shots[e.Shot] = shotControl;
                    _sprites.Add(shotControl);
                }
                else
                    InvadersHelper.MoveElementOnCanvas(_shots[e.Shot], e.Shot.Location.X * Scale,
                        e.Shot.Location.Y * Scale);
            }
            else
            {
                if (_shots.ContainsKey(e.Shot))
                {
                    _sprites.Remove(_shots[e.Shot]);
                   _shots.Remove(e.Shot);
                }
            }
        }
    }
}
