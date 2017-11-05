using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace CannyDemonstrator
{
    public enum GaussianSize
    {
        ThreeByThree = 3,
        FiveByFive = 5
    }

    public class CannyEdgeDetectorOptions
    {
        public GaussianSize gaussianSize;
        public double gaussianSigma;
        public byte weakEdgeThreshold;
        public byte strongEdgeThreshold;
        public int maxThreadCount; // 0 for same as the number of logical processors
    }

    public class CannyEdgeDetector
    {
        private enum EdgeDirection
        {
            Vertical = 0,
            Diagonal1 = 45,
            Horizontal = 90,
            Diagonal2 = 135,
            None // For non-edge pixels
        }

        private enum EdgeStrength
        {
            Strong,
            Weak,
            None
        }

        private const int BITMAP_COUNT = 6;

        private Bitmap[] bitmapSequence;
        private Tuple<double, double>[,] derivatives;
        private EdgeDirection[,] edgeDirections;
        private EdgeStrength[,] edgeStrengths;
        private Queue<Tuple<int, int>> strongEdges;
        private CannyEdgeDetectorOptions options;

        public Bitmap[] BitmapSequence
        {
            get
            {
                return bitmapSequence;
            }
        }

        public CannyEdgeDetectorOptions Options
        {
            get
            {
                return options;
            }

            set
            {
                options = value;
            }
        }

        public CannyEdgeDetector()
        {
            options = new CannyEdgeDetectorOptions
            {
                gaussianSize = GaussianSize.ThreeByThree,
                gaussianSigma = 1.4,
                maxThreadCount = Utility.Clamp(Environment.ProcessorCount, 1, 8),
                strongEdgeThreshold = 150,
                weakEdgeThreshold = 70
            };
        }

        public void LoadImage(string originalFileName)
        {
            Bitmap originalBitmap = Image.FromFile(originalFileName) as Bitmap;

            if (originalBitmap.PixelFormat != PixelFormat.Format24bppRgb)
                originalBitmap = Utility.ConvertTo24bppRgb(originalBitmap);

            bitmapSequence = new Bitmap[BITMAP_COUNT];
            derivatives = new Tuple<double, double>[originalBitmap.Height, originalBitmap.Width];
            edgeDirections = new EdgeDirection[originalBitmap.Height, originalBitmap.Width];
            edgeStrengths = new EdgeStrength[originalBitmap.Height, originalBitmap.Width];
            strongEdges = new Queue<Tuple<int, int>>();

            // Bitmaps are all initialized by the function that writes to them
            bitmapSequence[0] = originalBitmap;
        }

        public void Run()
        {
            ConvertToGrayscale(bitmapSequence[0], ref bitmapSequence[1]);
            GaussianBlur(bitmapSequence[1], ref bitmapSequence[2]);
        }

        private void ConvertToGrayscale(Bitmap source, ref Bitmap destination)
        {
            // If confused, please read https://stackoverflow.com/a/21498304
            destination = new Bitmap(source.Width, source.Height, PixelFormat.Format24bppRgb);

            BitmapData sourceData = source.LockBits(new Rectangle(0, 0, source.Width, source.Height), 
                                                    ImageLockMode.ReadOnly, source.PixelFormat);
            BitmapData destinationData = destination.LockBits(new Rectangle(0, 0, destination.Width, destination.Height),
                                                              ImageLockMode.WriteOnly, destination.PixelFormat);

            const int sourceDepth = 3;
            const int destinationDepth = 3; 

            byte[] sourceBuffer = new byte[sourceData.Height * sourceData.Width * sourceDepth];
            byte[] destinationBuffer = new byte[destinationData.Height * destinationData.Width * destinationDepth];

            Marshal.Copy(sourceData.Scan0, sourceBuffer, 0, sourceBuffer.Length);

            Action[] threadProcedures = new Action[options.maxThreadCount];
            int totalPixelCount = sourceData.Height * sourceData.Width;
            int pixelsPerThread = (int)((double)totalPixelCount / options.maxThreadCount);

            for (int i = 0; i < options.maxThreadCount; i++)
            {
                int localCounter = i;
                int currentThreadPixels = (i < options.maxThreadCount - 1) ? pixelsPerThread 
                                                                           : (totalPixelCount - pixelsPerThread * (options.maxThreadCount - 1));
                threadProcedures[i] = () => ConvertToGrayscaleWorker(sourceBuffer, destinationBuffer, localCounter * pixelsPerThread, currentThreadPixels);
            }

            Parallel.Invoke(threadProcedures);

            Marshal.Copy(destinationBuffer, 0, destinationData.Scan0, destinationBuffer.Length);

            source.UnlockBits(sourceData);
            destination.UnlockBits(destinationData);
        }

        private void ConvertToGrayscaleWorker(byte[] source, byte[] destination, int startPixel, int pixelCount)
        {
            int startIndex = 3 * startPixel;

            for (int i = 0; i < pixelCount; i++)
            {
                int pixelStartSource = startIndex + 3 * i;

                int pixelStartDestination = startIndex + 3 * i;
                // Maybe check for out of bounds, should not be needed

                byte grayscaleValue = (byte)((source[pixelStartSource] + source[pixelStartSource + 1] + source[pixelStartSource + 2]) / 3.0);
                destination[pixelStartDestination] = destination[pixelStartDestination + 1] = destination[pixelStartDestination + 2] = grayscaleValue;
            }
        }

        private void GaussianBlur(Bitmap source, ref Bitmap destination)
        {

        }
    }
}
