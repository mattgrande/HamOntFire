using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using HamOntFire.Core.Domain;
using Raven.Client;

namespace HamOntFire.Core
{
    /// <summary>
    /// Generate a Heat Map
    /// Note - Much of this is stolen from here: http://dylanvester.com/post/Creating-Heat-Maps-with-NET-20-(C-Sharp).aspx
    /// </summary>
    public class MapGenerator
    {
        private List<HeatPoint> _heatPoints = new List<HeatPoint>();

        // Calculate ratio to scale byte intensity range from 0-255 to 0-1
        private const float fRatio = 1F / Byte.MaxValue;
        // Precalulate half of byte max value
        private const byte bHalf = Byte.MaxValue / 2;

        private const int HeatMapWidth = 916;
        private const int HeatMapHeight = 844;

        public Bitmap Generate(string type, int ward, IDocumentSession session)
        {
            var td = new TweetManager(session);
            var events = td.GetEvents(type, ward);

            // Create new memory bitmap
            //var bMap = new Bitmap(HeatMapWidth, HeatMapHeight);
            Stream s = Assembly.GetExecutingAssembly().GetManifestResourceStream(@"HamOntFire.Core.Resources.map.png");
            Bitmap bMap = (Bitmap)Bitmap.FromStream(s);


            // Lets loop through the events and create a point each iteration
            foreach (Event @event in events)
            {
                // Generate locations and intensity
                int iX = LongitudeToX(@event.Long);
                int iY = LatitudeToY(@event.Lat);
                byte bIntensity = GetIntensity(@event.Units);

                // Add heat point to heat points list
                _heatPoints.Add(new HeatPoint(iX, iY, bIntensity));
            }

            bMap = CreateIntensityMask(bMap, _heatPoints);
            return Colorize(bMap, 255);
        }

        private Bitmap CreateIntensityMask(Bitmap bSurface, List<HeatPoint> aHeatPoints)
        {
            // Create new graphics surface from memory bitmap
            Graphics DrawSurface = Graphics.FromImage(bSurface);
            //// Set background color to white so that pixels can be correctly colorized
            //DrawSurface.Clear(Color.White);

            // Traverse heat point data and draw masks for each heat point
            foreach (HeatPoint DataPoint in aHeatPoints)
            {
                // Render current heat point on draw surface
                DrawHeatPoint(DrawSurface, DataPoint, 10);
            }

            return bSurface;
        }

        private void DrawHeatPoint(Graphics Canvas, HeatPoint HeatPoint, int Radius)
        {
            // Create points generic list of points to hold circumference points
            List<Point> CircumferencePointsList = new List<Point>();

            // Create an empty point to predefine the point struct used in the circumference loop
            Point CircumferencePoint;

            // Create an empty array that will be populated with points from the generic list
            Point[] CircumferencePointsArray;

            // Flip intensity on it's center value from low-high to high-low
            int iIntensity = (byte)(HeatPoint.Intensity - ((HeatPoint.Intensity - bHalf) * 2));
            // Store scaled and flipped intensity value for use with gradient center location
            float fIntensity = iIntensity * fRatio;

            // Loop through all angles of a circle
            // Define loop variable as a double to prevent casting in each iteration
            // Iterate through loop on 10 degree deltas, this can change to improve performance
            for (double i = 0; i <= 360; i += 10)
            {
                // Replace last iteration point with new empty point struct
                CircumferencePoint = new Point();

                // Plot new point on the circumference of a circle of the defined radius
                // Using the point coordinates, radius, and angle
                // Calculate the position of this iterations point on the circle
                CircumferencePoint.X = Convert.ToInt32(HeatPoint.X + Radius * Math.Cos(ConvertDegreesToRadians(i)));
                CircumferencePoint.Y = Convert.ToInt32(HeatPoint.Y + Radius * Math.Sin(ConvertDegreesToRadians(i)));

                // Add newly plotted circumference point to generic point list
                CircumferencePointsList.Add(CircumferencePoint);
            }

            // Populate empty points system array from generic points array list
            // Do this to satisfy the datatype of the PathGradientBrush and FillPolygon methods
            CircumferencePointsArray = CircumferencePointsList.ToArray();

            // Create new PathGradientBrush to create a radial gradient using the circumference points
            PathGradientBrush GradientShaper = new PathGradientBrush(CircumferencePointsArray);
            // Create new color blend to tell the PathGradientBrush what colors to use and where to put them
            ColorBlend GradientSpecifications = new ColorBlend(3);

            // Define positions of gradient colors, use intesity to adjust the middle color to
            // show more mask or less mask
            GradientSpecifications.Positions = new[] { 0, fIntensity, 1 };
            // Define gradient colors and their alpha values, adjust alpha of gradient colors to match intensity
            GradientSpecifications.Colors = new[]
                {
                    Color.FromArgb(0, Color.White),
                    Color.FromArgb(HeatPoint.Intensity, Color.Black),
                    Color.FromArgb(HeatPoint.Intensity, Color.Black)
                };

            // Pass off color blend to PathGradientBrush to instruct it how to generate the gradient
            GradientShaper.InterpolationColors = GradientSpecifications;
            // Draw polygon (circle) using our point array and gradient brush
            Canvas.FillPolygon(GradientShaper, CircumferencePointsArray);
        }

        private double ConvertDegreesToRadians(double degrees)
        {
            double radians = (Math.PI / 180) * degrees;
            return (radians);
        }
        
        public static Bitmap Colorize(Image mask, byte alpha)
        {
            // Create new bitmap to act as a work surface for the colorization process
            Bitmap Output = new Bitmap(mask.Width, mask.Height, PixelFormat.Format32bppArgb);

            // Create a graphics object from our memory bitmap so we can draw on it and clear it's drawing surface
            Graphics Surface = Graphics.FromImage(Output);
            Surface.Clear(Color.Transparent);

            // Build an array of color mappings to remap our greyscale mask to full color
            // Accept an alpha byte to specify the transparancy of the output image
            ColorMap[] Colors = CreatePaletteIndex(alpha);

            // Create new image attributes class to handle the color remappings
            // Inject our color map array to instruct the image attributes class how to do the colorization
            ImageAttributes Remapper = new ImageAttributes();
            Remapper.SetRemapTable(Colors);

            // Draw our mask onto our memory bitmap work surface using the new color mapping scheme
            Surface.DrawImage(mask, new Rectangle(0, 0, mask.Width, mask.Height), 0, 0, mask.Width, mask.Height, GraphicsUnit.Pixel, Remapper);

            // Send back newly colorized memory bitmap
            return Output;
        }

        private static ColorMap[] CreatePaletteIndex(byte Alpha)
        {
            ColorMap[] OutputMap = new ColorMap[256];

            // Change this path to wherever you saved the palette image.
            //Bitmap Palette = (Bitmap)Bitmap.FromFile(@"HamOntFire.Core.Resources.image.jpg");
            Assembly abc = Assembly.GetAssembly(typeof (MapGenerator));
            Stream s = abc.GetManifestResourceStream(@"HamOntFire.Core.Resources.image.jpg");
            Bitmap Palette = (Bitmap)Bitmap.FromStream(s);

            // Loop through each pixel and create a new color mapping
            for (int X = 0; X <= 255; X++)
            {
                OutputMap[X] = new ColorMap();
                OutputMap[X].OldColor = Color.FromArgb(X, X, X);
                OutputMap[X].NewColor = Color.FromArgb(Alpha, Palette.GetPixel(X, 0));
            }

            return OutputMap;
        }
        public int LatitudeToY(decimal d)
        {
            var diff = (d - GeoCoder.NorthernEdge) / (GeoCoder.SouthernEdge - GeoCoder.NorthernEdge);
            return Math.Abs( Convert.ToInt32(diff * HeatMapHeight) );
        }

        public int LongitudeToX(decimal d)
        {
            var diff = (d - GeoCoder.WesternEdge) / (GeoCoder.WesternEdge - GeoCoder.EasternEdge);
            return Math.Abs( Convert.ToInt32(diff * HeatMapWidth) );
        }

        private static byte GetIntensity(short units)
        {
            // Intensity is calculated linearly so that 1 Unit = 10 intensity, and 20 (or greater) Units = 50 intensity
            // Remember Grade 9 math class!  y = Intensity; x = Nbr of Units
            //  Calculate m
            //      m = delta y / delta x
            //      m = (20 - 1) / (50 - 20)
            //      m = 0.633
            //
            //  Calculate b
            //      y = mx + b
            //      1 = (0.633)(10) + b
            //      b = 5.33
            //
            //      y = mx + b
            //      y = (0.633)x + 5.33
            double y = (0.633 * units) + 5.33;
            if (y > 50)
                return 50;
            return Convert.ToByte(y);
        }
    }

    public struct HeatPoint
    {
        public int X;
        public int Y;
        public byte Intensity;

        public HeatPoint(int x, int y, byte i)
        {
            X = x;
            Y = y;
            Intensity = i;
        }
    }
}
