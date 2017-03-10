using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;


namespace Invaders.View
{
    using Model;
    using FrameworkElement = Windows.UI.Xaml.FrameworkElement;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;
    using Windows.UI.Xaml.Media.Animation;
    using Windows.UI.Xaml.Shapes;
    using Windows.UI.Xaml.Media;
    using Windows.UI;

    class InvadersHelper
    {
        static Random _random = new Random();

        public static FrameworkElement ShipControlFactory(Ship ship, double scale)
        {
            AnimatedImage shipImage = new AnimatedImage();
            if (ship is Invader)
            {
                Invader invader = ship as Invader;
                shipImage = new AnimatedImage(GenerateImageList(invader.Type), TimeSpan.FromMilliseconds(500));
            }
            else if (ship is Player)
            {
                Player player = ship as Player;
                shipImage = new AnimatedImage(new List<string>() { "player.png", "player.png" },
                    TimeSpan.FromMilliseconds(1));
            }
            shipImage.Width = ship.Size.Width * scale;
            shipImage.Height = ship.Size.Height * scale;
            SetCanvasLocation(shipImage, ship.Location.X * scale, ship.Location.Y * scale);
            return shipImage;
        }

        public static IEnumerable<string> GenerateImageList(InvaderType type)
        {
            List<string> imageNames = new List<string>();
            string invaderTypeName = "";
            switch (type)
            {
                case InvaderType.Bug:
                    invaderTypeName = "bug";
                    break;
                case InvaderType.Satellite:
                    invaderTypeName = "satellite";
                    break;
                case InvaderType.Saucer:
                    invaderTypeName = "flyingsaucer";
                    break;
                case InvaderType.Spaceship:
                    invaderTypeName = "spaceship";
                    break;
                case InvaderType.Star:
                    invaderTypeName = "star";
                    break;
            }

            if (!string.IsNullOrEmpty(invaderTypeName))
            {
                int numOfImages = 4;
                for (int i = 1; i <= numOfImages; i++)
                    imageNames.Add(invaderTypeName + i + ".png");
            }

            return imageNames;
        }

        public static void SetCanvasLocation(UIElement uiElement, double x, double y)
        {
            Canvas.SetLeft(uiElement, x);
            Canvas.SetTop(uiElement, y);
        }
        public static void MoveElementOnCanvas(UIElement uiElement, double toX, double toY)
        {
            double fromX = Canvas.GetLeft(uiElement);
            double fromY = Canvas.GetTop(uiElement);

            Storyboard sb = new Storyboard();
            DoubleAnimation animationX = CreateDoubleAnimation(uiElement, fromX, toX, "(Canvas.Left)");
            DoubleAnimation animationY = CreateDoubleAnimation(uiElement, fromY, toY, "(Canvas.Top)");

            sb.Children.Add(animationX);
            sb.Children.Add(animationY);
            sb.Begin();
        }
        public static FrameworkElement StarControlFactory(Point point, double scale)
        {
            FrameworkElement star;

            switch (_random.Next(3))
            {
                case 0:
                    star = new Rectangle()
                    {
                        Width = 2,
                        Height = 2,
                        Fill = new SolidColorBrush(GetRandomStarColor()),
                    };
                    break;
                case 1:
                    star = new Ellipse()
                    {
                        Width = 2,
                        Height = 2,
                        Fill = new SolidColorBrush(GetRandomStarColor()),
                    };
                    break;
                default:
                    star = new StarControl();
                    ((StarControl)star).SetFill(new SolidColorBrush(GetRandomStarColor()));
                    break;
            };

            SetCanvasLocation(star, point.X * scale, point.Y * scale);
            SendToBack(star);

            return star;
        }

        static Color GetRandomStarColor()
        {
            int colorR = _random.Next(255);
            int colorG = _random.Next(255);
            int colorB = _random.Next(255);
            Color newColor = Color.FromArgb(255, (byte)colorR, (byte)colorG, (byte)colorB);
            return newColor;
        }
        public static FrameworkElement ScanLineFactory(int yPosition, int screenWidth, double scale)
        {
            Rectangle scanLine = new Rectangle()
            {
                Height = 2,
                Width = screenWidth * scale,
                Opacity = .1,
                Fill = new SolidColorBrush(Colors.White)
            };

            SetCanvasLocation(scanLine, 0, yPosition * scale);
            return scanLine;
            // scan lines, rectangle filled set to new SolidColorBrush(Colors.White), height=2, opacity=1
        }

        public static FrameworkElement ShotControlFactory(Shot shot, double scale)
        {
            FrameworkElement newShot;

            newShot = new Rectangle()
            {
                Width = Shot.ShotSize.Width,
                Height = Shot.ShotSize.Height,
                Fill = new SolidColorBrush(Colors.Yellow)
            };
            SetCanvasLocation(newShot, shot.Location.X * scale, shot.Location.Y * scale);
            return newShot;
        }
        public static void ResizeElement(FrameworkElement control, double width, double height)
        {
            control.Width = width;
            control.Height = height;
        }

        public static DoubleAnimation CreateDoubleAnimation(UIElement uiElement, double from, double to,
            string propertyToAnimate)
        {
            return CreateDoubleAnimation(uiElement, from, to, propertyToAnimate, TimeSpan.FromMilliseconds(25));
        }
        public static DoubleAnimation CreateDoubleAnimation(UIElement uiElement, double from, double to, 
            string propertyToAnimate, TimeSpan duration)
        {
            DoubleAnimation animation = new DoubleAnimation();
            Storyboard.SetTarget(animation, uiElement);
            Storyboard.SetTargetProperty(animation, propertyToAnimate);
            animation.From = from;
            animation.To = to;
            animation.Duration = duration;
            return animation;
        }

        public static void SendToBack(FrameworkElement newStar)
        {
            Canvas.SetZIndex(newStar, -1000);
        }
    }
}
