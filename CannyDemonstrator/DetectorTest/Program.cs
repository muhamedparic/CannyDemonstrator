using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CannyDemonstrator;

namespace DetectorTest
{
    class Program
    {
        static void Main(string[] args)
        {
            CannyEdgeDetector detector = new CannyEdgeDetector();
            string fileName = @"C:\Users\Muhamed\Desktop\Coding\Images\plane.jpg";

            detector.Options.maxThreadCount = 8;
            detector.LoadImage(fileName);
            detector.Run();
            //detector.BitmapSequence[1].Save(@"C:\Users\Muhamed\Desktop\Coding\Images\gray.png");
        }
    }
}
