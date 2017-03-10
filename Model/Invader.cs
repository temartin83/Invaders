using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;

namespace Invaders.Model
{
    class Invader : Ship
    {
        public static readonly Size InvaderSize = new Size(15, 15);

        public const double HorizontalSpeed = 10;
        public const double VerticalSpeed = 15;

        int _score;
        public int Score
        {
            get
            {
                return _score;
            }
            private set
            {
                _score = value;
            }
        }

        public InvaderType Type { get; private set; }
        public Invader(Point location, InvaderType type) : base(location, InvaderSize)
        {
            Type = type;
            switch (type)
            {
                case InvaderType.Bug:
                    Score = 40;
                    break;
                case InvaderType.Satellite:
                    Score = 20;
                    break;
                case InvaderType.Saucer:
                    Score = 30;
                    break;
                case InvaderType.Spaceship:
                    Score = 50;
                    break;
                case InvaderType.Star:
                    Score = 10;
                    break;
            }
        }

        public override void Move(Direction direction)
        {
            switch (direction)
            {
                case Direction.Left:
                    Location = new Point(Location.X - HorizontalSpeed, Location.Y);
                    break;
                case Direction.Right:
                    Location = new Point(Location.X + HorizontalSpeed, Location.Y);
                    break;
                case Direction.Down:
                    Location = new Point(Location.X, Location.Y + VerticalSpeed);
                    break;
                default: break;
            }
        }
    }
}
