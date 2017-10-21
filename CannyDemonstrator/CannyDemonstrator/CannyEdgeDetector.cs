using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
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
        GaussianSize gaussianSize;
        double gaussianSigma;
        byte weakEdgeThreshold;
        byte strongEdgeThreshold;
        int maxThreadCount; // 0 for same as the number of logical processors
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
        Queue<Tuple<int, int>> strongEdges;
        CannyEdgeDetectorOptions options;

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
        }

        public CannyEdgeDetector()
        { }

        public void LoadImage(Image originalImage)
        {
            bitmapSequence = new Bitmap[BITMAP_COUNT];
            derivatives = new Tuple<double, double>[originalImage.Height, originalImage.Width];
            edgeDirections = new EdgeDirection[originalImage.Height, originalImage.Width];
            edgeStrengths = new EdgeStrength[originalImage.Height, originalImage.Width];
            strongEdges = new Queue<Tuple<int, int>>();

            bitmapSequence[0] = new Bitmap(originalImage);
        }


    }
}
