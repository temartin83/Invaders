using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;

namespace Invaders.Model
{
    using Invaders.View;
    using ViewModel;
    using FrameworkElement = Windows.UI.Xaml.FrameworkElement;

    class InvadersModel
    {
        public readonly static Size PlayAreaSize = new Size(400, 300);

        public const int MaximumPlayerShots = 3;

        public const int InitialStarCount = 50;

        readonly Random _random = new Random();

        public int Score { get; private set; }
        public int Wave { get; private set; }
        public int Lives { get; private set; }
        public bool GameOver { get; private set; }

        DateTime? _playerDied = null;
        public bool PlayerDying { get { return _playerDied.HasValue; } }

        Player _player;

        readonly List<Invader> _invaders = new List<Invader>();
        readonly List<Shot> _playerShots = new List<Shot>();
        readonly List<Shot> _invaderShots = new List<Shot>();
        readonly List<Point> _stars = new List<Point>();
        Direction _invaderDirection = Direction.Right;
        bool _justMovedDown = false;
        DateTime _lastUpdated = DateTime.MinValue;

        public event EventHandler<ShipChangedEventArgs> ShipChanged;
        public void OnShipChanged(Ship ship, bool killed)
        {
            ShipChanged?.Invoke(this, new ShipChangedEventArgs(ship, killed));
        }

        public event EventHandler<ShotMovedEventArgs> ShotMoved;
        public void OnShotMoved(Shot shot, bool disappeared)
        {
            ShotMoved?.Invoke(this, new ShotMovedEventArgs(shot, disappeared));
        }

        public event EventHandler<StarChangedEventArgs> StarChanged;
        public void OnStarChanged(Point point, bool disappeared)
        {
            StarChanged?.Invoke(this, new StarChangedEventArgs(point, disappeared));
        }

        public InvadersModel()
        {
            EndGame();
        }

        public void StartGame()
        {
            GameOver = false;

            //clear ships, shots, and stars

            foreach (Invader invader in _invaders)
                OnShipChanged(invader as Invader, true);

            foreach (Shot shot in _playerShots)
                OnShotMoved(shot, true);
            foreach (Shot shot in _invaderShots)
                OnShotMoved(shot, true);

            foreach (Point point in _stars)
                OnStarChanged(point, true);

            // clear all lists
            _invaders.Clear();
            _playerShots.Clear();
            _invaders.Clear();
            _stars.Clear();

            // new stars
            for (int i = 0; i < InitialStarCount; i++)
            {
                AddStar();
            }

            // generate new player
            _player = new Player();
            OnShipChanged(_player, false);

            // set lives, stats, and generate first wave
            Lives = 2;
            Wave = 0;
            Score = 0;
            NewWave();
        }
        public void EndGame()
        {
            GameOver = true;
        }
        
        // when player fires a shot
        public void FireShot()
        {
            // This method checks the number of player shots on screen to make sure there aren’t too many,
            // then it adds a new Shot to the _playerShots collection and fires the ShotMoved event.
            if (GameOver || PlayerDying || _lastUpdated == DateTime.MinValue)
                return;

            if (_playerShots.Count() < MaximumPlayerShots)
            {
                Shot shotFired = new Shot(new Point(_player.Location.X + (_player.Size.Width / 2) - 1, _player.Location.Y),
                    Direction.Up);
                _playerShots.Add(shotFired);
                OnShotMoved(shotFired, false);
            }
        }

        // player movement
        public void MovePlayer(Direction direction)
        {
            if (!_playerDied.HasValue)
            {
                _player.Move(direction);
                OnShipChanged(_player, false);
            }
        }

        public void MoveShots()
        {
            List<Shot> playerShots = _playerShots.ToList();
            foreach (Shot shot in playerShots)
            {
                shot.Move();
                OnShotMoved(shot, false);
                if (shot.Location.Y < 0)
                {
                    _playerShots.Remove(shot);
                    OnShotMoved(shot, true);
                }
            }

            List<Shot> invaderShots = _invaderShots.ToList();
            foreach (Shot shot in invaderShots)
            {
                shot.Move();
                OnShotMoved(shot, false);
                if (shot.Location.Y > PlayAreaSize.Height)
                {
                    _invaderShots.Remove(shot);
                    OnShotMoved(shot, true);
                }
            }
        }

        // stars twinkle
        public void Twinkle()
        {
            // add a star
            if (_random.Next(2) == 0)
                if (_stars.Count() < (1.5 * InitialStarCount) - 1)
                    AddStar();
            else
                RemoveStar();
        }

        // add a a star to the background
        public void AddStar()
        {
            Point newStar = new Point((double)_random.Next((int)PlayAreaSize.Width - 10),
                (double)_random.Next((int)PlayAreaSize.Height - 10));
            _stars.Add(newStar);
            OnStarChanged(newStar, false);
        }

        // remove a star from the background
        public void RemoveStar()
        {
            Point starToRemove = _stars[_random.Next(_stars.Count())];
            _stars.Remove(starToRemove);
            OnStarChanged(starToRemove, true);
        }

        // update the game
        public void Update(bool paused)
        {
            Twinkle();
            if (!paused)
            {
                if (_invaders.Count() < 1)
                {
                    NewWave();
                }

                if (!PlayerDying)
                {
                    // move all invaders
                    MoveInvaders();
                    // move all shots
                    MoveShots();
                    // invaders return fire
                    ReturnFire();
                    // check for collisions
                    CheckForPlayerCollisions();
                    CheckForInvaderCollisions();
                }

                if (PlayerDying && TimeSpan.FromSeconds(2.5) < DateTime.Now - _playerDied)
                {
                    _playerDied = null;
                    OnShipChanged(_player, false);
                }
            }
        }

        // setup new wave of invaders
        public void NewWave()
        {
            double spacing = 1.4;
            Wave++;
            _invaders.Clear();
            RemoveAllShots();
            // invaders lineup
            List<InvaderType> waveSetup = new List<InvaderType>()
            {
                InvaderType.Spaceship,
                InvaderType.Bug,
                InvaderType.Saucer,
                InvaderType.Satellite,
                InvaderType.Star,
                InvaderType.Star
            };
            // 1.4 invader lengths apart both horizontally and vertically
            for (int row = 0; row < 6; row++)
            {
                for (int col = 0; col < 11; col++)
                {
                    Point location = new Point(col * (Invader.InvaderSize.Width * spacing),
                        row * (Invader.InvaderSize.Height * spacing));
                    _invaders.Add(new Invader(location, waveSetup[row]));
                    OnShipChanged(_invaders[_invaders.Count() - 1], false);
                }
            }
        }

        // move the invaders
        void MoveInvaders()
        {
            TimeSpan timeSinceLastMoved = DateTime.Now - _lastUpdated;
            double millisecondsBetweenMoves = Math.Max(7 - Wave, 1) * (2 * _invaders.Count());

            if (timeSinceLastMoved >= TimeSpan.FromMilliseconds(millisecondsBetweenMoves))
            {
                _lastUpdated = DateTime.Now;
                if (_invaderDirection == Direction.Right)
                {
                    var rightBoundaryInvaders = from invader in _invaders
                                                where invader.Area.Right > PlayAreaSize.Width - Invader.HorizontalSpeed
                                                select invader;

                    if (rightBoundaryInvaders.Count() > 0)
                    {
                        _invaderDirection = Direction.Down;
                        foreach (Invader invader in _invaders)
                        {
                            invader.Move(_invaderDirection);
                            OnShipChanged(invader, false);
                        }
                        _justMovedDown = true;
                        _invaderDirection = Direction.Left;
                    }
                    else
                    {
                        _justMovedDown = false;
                        foreach (Invader invader in _invaders)
                        {
                            invader.Move(_invaderDirection);
                            OnShipChanged(invader, false);
                        }
                    }
                }
                else if (_invaderDirection == Direction.Left)
                {
                    var leftSideInvaders = from invaders in _invaders
                                           where invaders.Area.Left < Invader.HorizontalSpeed
                                           select invaders;

                    if (leftSideInvaders.Count() > 0)
                    {
                        _invaderDirection = Direction.Down;
                        foreach (Invader invader in _invaders)
                        {
                            invader.Move(_invaderDirection);
                            OnShipChanged(invader, false);
                        }
                        _justMovedDown = true;
                        _invaderDirection = Direction.Right;
                    }
                    else
                    {
                        _justMovedDown = false;
                        foreach (Invader invader in _invaders)
                        {
                            invader.Move(_invaderDirection);
                            OnShipChanged(invader, false);
                        }
                    }
                }
            }
            if (_justMovedDown)
            {
                // if the invaders get too far down, take a life and start a new wave
                if (TooFarDown())
                {
                    Lives--;
                    OnShipChanged(_player, true);
                    if (Lives == 0)
                        EndGame();
                    else
                    {
                        List<Invader> invaders = _invaders.ToList();
                        foreach (Invader invader in invaders)
                        {
                            _invaders.Remove(invader);
                            OnShipChanged(invader, true);
                        }
                        NewWave();
                    }
                }
            }
        }

        bool TooFarDown()
        {
            var invaderColumns = from invader in _invaders
                                 group invader by invader.Location.X
                                 into invaderGroup
                                 orderby invaderGroup.Key descending
                                 select invaderGroup;

            foreach (var column in invaderColumns)
                foreach (Invader invader in column)
                    if (invader.Area.Bottom + Invader.VerticalSpeed >= _player.Area.Top)
                        return true;
            return false;

        }

        // invaders return fire
        void ReturnFire()
        {
            if (_invaderShots.Count() > Wave + 1 || _random.Next(10) < 10 - Wave)
                return;

            var invaderColumns = from invader in _invaders
                               group invader by invader.Location.X
                               into invaderGroup
                               orderby invaderGroup.Key descending
                               select invaderGroup;

            var randomColumn = invaderColumns.ElementAt(_random.Next(invaderColumns.Count()));
            var shooter = randomColumn.Last();
            Point shotFrom = new Point(shooter.Area.X + (shooter.Size.Width / 2) - 1, shooter.Area.Bottom);
            Shot invaderShot = new Shot(shotFrom, Direction.Down);
            _invaderShots.Add(invaderShot);
            OnShotMoved(invaderShot, false);
        }

        public bool RectsOverlap(Rect r1, Rect r2)
        {
            r1.Intersect(r2);
            if (r1.Width > 0 || r1.Height > 0)
                return true;
            return false;
        }

        // check to see if player is hit
        void CheckForPlayerCollisions()
        {
            List<Shot> invaderShots = _invaderShots.ToList();

            foreach (Shot shot in invaderShots)
            {
                Rect shotRect = new Rect(shot.Location.X, shot.Location.Y, Shot.ShotSize.Width,
                    Shot.ShotSize.Height);

                if (RectsOverlap(_player.Area, shotRect))
                {
                    if (Lives == 0)
                        EndGame();
                    else
                    {
                        _invaderShots.Remove(shot);
                        OnShotMoved(shot, true);
                        _playerDied = DateTime.Now;
                        OnShipChanged(_player, true);
                        RemoveAllShots();
                        Lives--;
                    }
                }
            }
        }

        // check if invader ship is hit
        void CheckForInvaderCollisions()
        {
            List<Shot> playerShots = _playerShots.ToList();
            List<Invader> invaders = _invaders.ToList();

            foreach (Shot shot in playerShots)
            {
                Rect shotRect = new Rect(shot.Location.X, shot.Location.Y, Shot.ShotSize.Width,
                    Shot.ShotSize.Height);

                var hitInvaders = from invader in invaders
                                  where RectsOverlap(invader.Area, shotRect)
                                  select invader;

                foreach (Invader invader in hitInvaders)
                {
                    _invaders.Remove(invader);
                    OnShipChanged(invader, true);
                    _playerShots.Remove(shot);
                    OnShotMoved(shot, true);
                    Score += invader.Score;
                }
            }
        }

        void RemoveAllShots()
        {
            List<Shot> playerShots = _playerShots.ToList();
            List<Shot> invaderShots = _invaderShots.ToList();

            foreach (Shot shot in playerShots)
                OnShotMoved(shot, true);

            foreach (Shot shot in invaderShots)
                OnShotMoved(shot, true);

            _playerShots.Clear();
            _invaderShots.Clear();
        }
        // update all ships and stars
        public void UpdateAllShipsAndStars()
        {
            foreach (Shot shot in _playerShots)
                OnShotMoved(shot, false);
            foreach (Invader ship in _invaders)
                OnShipChanged(ship, false);
            OnShipChanged(_player, false);
            foreach (Point point in _stars)
                OnStarChanged(point, false);
        }
    }
}
