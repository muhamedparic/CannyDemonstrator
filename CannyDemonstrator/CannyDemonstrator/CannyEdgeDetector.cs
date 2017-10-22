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
    enum GaussianSize
    {
        ThreeByThree = 3,
        FiveByFive = 5
    }

    struct CannyEdgeDetectorOptions
    {
        public GaussianSize gaussianSize;
        public double gaussianSigma;
        public byte weakEdgeThreshold;
        public byte strongEdgeThreshold;
        public int maxThreadCount; // 0 for same as the number of logical processors
    }

    class CannyEdgeDetector
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
            options.gaussianSize = GaussianSize.ThreeByThree;
            options.gaussianSigma = 1.4;
            options.maxThreadCount = Utility.Clamp(Environment.ProcessorCount, 1, 8);
            options.strongEdgeThreshold = 150;
            options.weakEdgeThreshold = 70;
        }

        public void LoadImage(string originalFileName)
        {
            Image originalImage = Image.FromFile(originalFileName);

            if (originalImage.PixelFormat != PixelFormat.Format24bppRgb)
                originalImage = Utility.ConvertTo24bppRgb(originalImage);

            bitmapSequence = new Bitmap[BITMAP_COUNT];
            derivatives = new Tuple<double, double>[originalImage.Height, originalImage.Width];
            edgeDirections = new EdgeDirection[originalImage.Height, originalImage.Width];
            edgeStrengths = new EdgeStrength[originalImage.Height, originalImage.Width];
            strongEdges = new Queue<Tuple<int, int>>();

            // Bitmaps are all initialized by the function that writes to them
            bitmapSequence[0] = new Bitmap(originalImage);
        }

        public void Run()
        {
            ConvertToGrayscale(bitmapSequence[0], bitmapSequence[1]);
        }

        private void ConvertToGrayscale(Bitmap source, Bitmap destination)
        {
            // If confused, please read https://stackoverflow.com/a/21498304
            destination = new Bitmap(source.Width, source.Height, PixelFormat.Format16bppGrayScale);

            BitmapData sourceData = source.LockBits(new Rectangle(0, 0, source.Width, source.Height), ImageLockMode.ReadOnly, source.PixelFormat);
            BitmapData destinationData = destination.LockBits(new Rectangle(0, 0, destination.Width, destination.Height),
                                                              ImageLockMode.WriteOnly, destination.PixelFormat);

            int sourceDepth = Image.GetPixelFormatSize(sourceData.PixelFormat) / 8;
            int destinationDepth = 2; // 16 bit grayscale image

            byte[] sourceBuffer = new byte[sourceData.Height * sourceData.Width * sourceDepth];
            byte[] destinationBuffer = new byte[destinationData.Height * destinationData.Width * destinationDepth];

            Marshal.Copy(sourceData.Scan0, sourceBuffer, 0, sourceBuffer.Length);

            Action[] threadProcedures = new Action[options.maxThreadCount];
            int pixelsPerThread = (int)Math.Ceiling((double)sourceData.Height * sourceData.Width / options.maxThreadCount);

            for (int i = 0; i < options.maxThreadCount; i++)
                threadProcedures[i] = () => ConvertToGrayscaleWorker(sourceBuffer, destinationBuffer, i * pixelsPerThread, pixelsPerThread);

            Parallel.Invoke(threadProcedures);

            Marshal.Copy(destinationBuffer, 0, destinationData.Scan0, destinationBuffer.Length);

            source.UnlockBits(sourceData);
            destination.UnlockBits(destinationData);
        }

        private void ConvertToGrayscaleWorker(byte[] source, byte[] destination, int startIndex, int pixelCount)
        {
            for (int i = 0; i < pixelCount; i++)
            {
                int pixelStartSource = startIndex + 3 * i;
                if (pixelStartSource >= source.Length)
                    break;

                int pixelStartDestination = startIndex + 2 * i;
                // Maybe check for out of bounds, should not be needed

                byte grayscaleValue = (byte)((source[pixelStartSource] + source[pixelStartSource + 1] + source[pixelStartSource + 2]) / 3);
                destination[pixelStartDestination + 1] = grayscaleValue; // Maybe remove + 1 if little endian
            }
        }
    }
}
