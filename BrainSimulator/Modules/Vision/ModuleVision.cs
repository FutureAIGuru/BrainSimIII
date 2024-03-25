//
// PROPRIETARY AND CONFIDENTIAL
// Brain Simulator 3 v.1.0
// © 2022 FutureAI, Inc., all rights reserved
//

using System;
using System.Collections.Generic;
using System.Windows.Media.Imaging;
using System.Xml.Serialization;
using static System.Math;

namespace BrainSimulator.Modules
{
    public class ModuleVision : ModuleBase
    {
        //any public variable you create here will automatically be saved and restored  with the network
        //unless you precede it with the [XmlIgnore] directive
        public string currentFilePath = "";
        public string previousFilePath = "";
        [XmlIgnore]
        public BitmapImage bitmap = null;
        [XmlIgnore]
        public float[,] boundaryArray;

        public ModuleVision()
        {
        }

        //fill this method in with code which will execute
        //once for each cycle of the engine
        public override void Fire()
        {
            Init();  //be sure to leave this here

            if (currentFilePath == previousFilePath) return;
            previousFilePath = currentFilePath;

            bitmap = new BitmapImage();
            if (bitmap == null) return;


            try
            {
                // Initialize the bitmap with the URI of the file
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(currentFilePath, UriKind.Absolute);
                bitmap.CacheOption = BitmapCacheOption.OnLoad; // To load the image fully at load time
                bitmap.EndInit();
                bitmap.Freeze(); // Make the BitmapImage thread safe
            }
            catch (Exception ex)
            {
                // Handle exceptions such as file not found or invalid file format
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
            // if you want the dlg to update, use the following code whenever any parameter changes
            UpdateDialog();

            HSLColor[,] imageArray;
            imageArray = GetImageArrayFromBitmapImage();
            boundaryArray = GetBoundariesFromImageArray(imageArray);
            InitMatchPatterns();
            MatchPatterns(matchpatterns);
        }

        private HSLColor[,] GetImageArrayFromBitmapImage()
        {
            HSLColor[,] imageArray = new HSLColor[(int)bitmap.Width, (int)bitmap.Height];
            int stride = (bitmap.PixelWidth * bitmap.Format.BitsPerPixel + 7) / 8;
            byte[] pixelBuffer = new byte[stride * bitmap.PixelHeight];
            bitmap.CopyPixels(pixelBuffer, stride, 0);
            for (int i = 0; i < imageArray.GetLength(0); i++)
            {
                for (int j = 0; j < imageArray.GetLength(1); j++)
                {
                    int index = j * stride + i * 4; // Assuming 32 bits per pixel (4 bytes: BGRA)

                    if (index < pixelBuffer.Length - 4)
                    {
                        byte blue = pixelBuffer[index];
                        byte green = pixelBuffer[index + 1];
                        byte red = pixelBuffer[index + 2];
                        byte alpha = pixelBuffer[index + 3];

                        HSLColor pixelColor = new HSLColor(red, green, blue);
                        imageArray[i, j] = pixelColor;
                    }
                }
            }

            return imageArray;
        }

        private float[,] GetBoundariesFromImageArray(HSLColor[,] imageArray)
        {
            float[,] boundaryArray = new float[imageArray.GetLength(0), imageArray.GetLength(1)];

            //todo generalize this to handle multiple angles
            float dx = 1;
            float dy = 0;
            int sx = 0; int sy = 0;
            for (sy = 0; sy < imageArray.GetLength(1); sy++)
            {
                var rayThruImage = LineThroughArray(dx, dy, sx, sy, imageArray);
                for (int i = 0; i < rayThruImage.Count - 1; i++)
                {
                    float diff = PixelDifference(rayThruImage[i], rayThruImage[i + 1]);
                    if (diff != 0)
                    {
                        float x = sx + dx * i;
                        float y = sy + dy * i;
                        boundaryArray[(int)x, (int)y] += diff;
                    }
                }
            }
            dx = 0;
            dy = 1;
            sy = 0;
            for (sx = 0; sx < imageArray.GetLength(1); sx++)
            {
                var rayThruImage = LineThroughArray(dx, dy, sx, sy, imageArray);
                //todo: filter out noisy areas in the ray
                //todo: handle boundaries which fade over a few pixels
                for (int i = 0; i < rayThruImage.Count - 1; i++)
                {
                    float diff = PixelDifference(rayThruImage[i], rayThruImage[i + 1]);
                    if (diff != 0)
                    {
                        float x = sx + dx * i;
                        float y = sy + dy * i;
                        boundaryArray[(int)x, (int)y] += diff;
                    }
                }
            }
            return boundaryArray;
        }


        List<HSLColor> LineThroughArray(float dx, float dy, int startX, int startY, HSLColor[,] imageArray)
        {
            List<HSLColor> retVal = new();
            float x = startX; float y = startY;

            while (x < imageArray.GetLength(0) && y < imageArray.GetLength(1))
            {
                HSLColor c = imageArray[(int)x, (int)y];
                if (c != null)
                    retVal.Add(c);
                x += dx;
                y += dy;
            }
            return retVal;
        }

        float PixelDifference(HSLColor c1, HSLColor c2)
        {
            float hueWeight = .2f;
            float satWeight = .2f;
            float lumWeight = 1.0f;
            float retVal = Abs(c1.hue - c2.hue) * hueWeight + Abs(c1.saturation - c2.saturation) * satWeight + Abs(c1.luminance - c2.luminance) * lumWeight;
            retVal /= 256;
            return retVal;
        }

        List<float[,]> matchpatterns;

        void InitMatchPatterns()
        {
            matchpatterns = new List<float[,]>();
            matchpatterns.Add(new float[,] {
                {0,0,1,0,0 },
                {0,0,1,0,0 },
                {0,0,1,1,1 },
                {0,0,0,0,0 },
                {0,0,0,0,0 },
                });
            matchpatterns.Add(new float[,] {
                {0,0,0,0,0 },
                {0,0,0,0,0 },
                {0,0,1,1,1 },
                {0,0,1,0,0 },
                {0,0,1,0,0 },
                });
            matchpatterns.Add(new float[,] {
                {0,0,0,0,0 },
                {0,0,0,0,0 },
                {1,1,1,0,0 },
                {0,0,1,0,0 },
                {0,0,1,0,0 },
                });

            matchpatterns.Add(new float[,] {
                {0,0,1,0,0 },
                {0,0,1,0,0 },
                {1,1,1,0,0 },
                {0,0,0,0,0 },
                {0,0,0,0,0 },
                });
        }

        public class FoundPattern
        {
            public int pattern; public int x; public int y; public float confidence;
            public override string ToString() { return x + "," + y + "," + pattern + "," + confidence; }
        };
        List<FoundPattern> foundPatterns;
        float MatchPatterns(List<float[,]> patterns)
        {
            foundPatterns = new();
            for (int x = 0; x < boundaryArray.GetLength(0); x++)
            {
                for (int y = 0; y < boundaryArray.GetLength(1); y++)
                {
                    float max = 1;
                    int bestPattern=0;
                    int bestX=0;
                    int bestY=0;
                    for (int patternIndex = 0; patternIndex < patterns.Count; patternIndex++)
                    {
                        float[,] pattern = patterns[patternIndex];
                        float matchValue = MatchPattern(x, y, pattern, boundaryArray);
                        if (matchValue > max)
                        {
                            max = matchValue;
                            bestPattern = patternIndex;
                            bestX = x;
                            bestY = y;
                        }
                    }
                    if (max > 3.7f)
                    {
                        foundPatterns.Add(new FoundPattern { x = bestX,y=bestY,pattern=bestPattern,confidence=max });
                    }
                }
            }
            return 0;
        }
        float MatchPattern(int x, int y, float[,] pattern, float[,] boundaryArray)
        {
            float retVal = 0;
            if (x + pattern.GetLength(0) >= boundaryArray.GetLength(0))
                return 0;
            if (y + pattern.GetLength(1) >= boundaryArray.GetLength(1))
                return 0;
            for (int i = 0; i < pattern.GetLength(0); i++)
                for (int j = 0; j < pattern.GetLength(1); j++)
                {
                    float boundaryValue = boundaryArray[x + i, y + j];
                    float patternValue = pattern[i, j];
                    if (boundaryValue > 0 && patternValue > 0)
                        retVal += boundaryValue * patternValue;
                    else if (boundaryValue <= 0 && patternValue <= 0)
                        retVal += .1f;
                    else
                        retVal -= .1f;

                }
            return retVal;
        }


        // fill this method in with code which will execute once
        // when the module is added, when "initialize" is selected from the context menu,
        // or when the engine restart button is pressed
        public override void Initialize()
        {
        }

        // the following can be used to massage public data to be different in the xml file
        // delete if not needed
        public override void SetUpBeforeSave()
        {
        }

        public override void SetUpAfterLoad()
        {
        }

        // called whenever the size of the module rectangle changes
        // for example, you may choose to reinitialize whenever size changes
        // delete if not needed
        public override void SizeChanged()
        {

        }

    }
}
