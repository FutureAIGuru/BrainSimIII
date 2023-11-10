using BrainSimulator.Modules;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using Emgu.Util;
using SharpDX;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace BrainSimulator
{
    public static class ImgUtils
    {
        public static System.Drawing.Size ProcessingSize{ get; set; } = new System.Drawing.Size(512, 384);

        // Image distribution method. ==========================================
        // DistributeNextFrame() takes an image from Sallie.VideoQueue, and distributes it
        // to all modules that need it. 
        public static void DistributeNextFrame(List<ModuleView> modules)
        {
            if (modules == null) return;
            Mat NextImage = null;
            string NextFilename = "";
            CameraFeedEntry newFeed;
            // Not really needed....
            // if (Sallie.VideoQueue.Count < 2) return;

            bool newFeedFound = Sallie.VideoQueue.TryDequeue(out newFeed);
            
            if (newFeedFound && newFeed != null && newFeed.image != null || Sallie.TestImage != null)
            {
                if (Sallie.TestImage != null)
                {
                    NextImage = Sallie.TestImage;
                    NextFilename = Sallie.TestImageFilename;
                }
                else if (Sallie.TestFolderName.Length > 0)
                {
                    NextFilename = Sallie.TestFolderFilenames[Sallie.TestFolderIndex++];
                    if (Sallie.TestFolderIndex >= Sallie.TestFolderFilenames.Length)
                        Sallie.TestFolderIndex = 0;
                    NextImage = CvInvoke.Imread(NextFilename);
                }
                else
                {
                    NextImage = newFeed.image;
                    NextFilename = newFeed.filename;
                }
            }
            if (NextImage == null) return;
            
            foreach (var module in modules)
            {
                module.TheModule.SetInputImage(NextImage.Clone(), NextFilename);
            }
        }

        public static BitmapImage ToBitmapImage(UnmanagedObject matOrUmat)
        {
            if (matOrUmat == null)
            {
                return null;
            }
            Bitmap bitmap;
            try
            {
                bitmap = ((Mat)matOrUmat).ToBitmap();
            }
            catch (Exception)
            {
                try
                {
                    Debug.WriteLine("WARNING: ToBitmapImage: Mat cast failed, trying cast to UMat");
                    bitmap = ((UMat)matOrUmat).ToBitmap();
                }
                catch (Exception)
                {
                    bitmap = new(100, 100);  // dummy to make sure it is assigned
                }
            }
            return Image2BitmapImage(bitmap);
        }

        public static BitmapImage Image2BitmapImage(System.Drawing.Image img)
        {
            if (img == null) return null;
            using var memory = new MemoryStream();
            img.Save(memory, ImageFormat.Bmp);
            memory.Position = 0;

            var bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.StreamSource = memory;
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapImage.EndInit();

            return bitmapImage;
        }

        public static Color GetColorFromIndex(int index)
        {
            Color[] colorSet = new Color[32]
            {
                System.Drawing.Color.FromArgb(128, 128, 128),
                System.Drawing.Color.FromArgb(255,   0,   0),
                System.Drawing.Color.FromArgb(  0, 255,   0),
                System.Drawing.Color.FromArgb(  0,   0, 255),

                System.Drawing.Color.FromArgb(128, 128, 128),
                System.Drawing.Color.FromArgb(  0, 255, 255),
                System.Drawing.Color.FromArgb(255,   0, 255),
                System.Drawing.Color.FromArgb(255, 255,   0),

                System.Drawing.Color.FromArgb( 64,  64,  64),
                System.Drawing.Color.FromArgb(128,   0,   0),
                System.Drawing.Color.FromArgb(  0, 128,   0),
                System.Drawing.Color.FromArgb(  0,   0, 128),

                System.Drawing.Color.FromArgb( 64,  64,  64),
                System.Drawing.Color.FromArgb(  0, 128, 128),
                System.Drawing.Color.FromArgb(128,   0, 128),
                System.Drawing.Color.FromArgb(128, 128,   0),

                System.Drawing.Color.FromArgb( 32,  32,  32),
                System.Drawing.Color.FromArgb( 64,   0,   0),
                System.Drawing.Color.FromArgb(  0,  64,   0),
                System.Drawing.Color.FromArgb(  0,   0,  64),

                System.Drawing.Color.FromArgb( 32,  32,  32),
                System.Drawing.Color.FromArgb(  0,  64,  64),
                System.Drawing.Color.FromArgb( 64,   0,  64),
                System.Drawing.Color.FromArgb( 64,  64,   0),

                System.Drawing.Color.FromArgb( 16,  16,  16),
                System.Drawing.Color.FromArgb( 32,   0,   0),
                System.Drawing.Color.FromArgb(  0,  32,   0),
                System.Drawing.Color.FromArgb(  0,   0,  32),

                System.Drawing.Color.FromArgb( 16,  16,  16),
                System.Drawing.Color.FromArgb(  0,  32,  32),
                System.Drawing.Color.FromArgb( 32,   0,  32),
                System.Drawing.Color.FromArgb( 32,  32,   0),


            };
            return colorSet[index % 32];
        }

        public static void DrawGraphOnImage(List<double> processed, 
                                            Mat img, 
                                            double shift = 0, 
                                            double multiplier = 1.0,
                                            int colorIndex = 0, 
                                            bool horizontal = true)
        {
            if (processed == null || processed.Count == 0 || img == null) return;
            // plot values in 
            if (horizontal)
            {
                // calculate range to plot
                int middle = img.Height / 2;

                int x1 = 0;
                int y1 = (int)(middle - shift - processed[0] * multiplier);
                int x2 = x1;
                int y2 = y1;
                foreach (double value in processed)
                {
                    y2 = (int)(middle - shift - value * multiplier);
                    CvInvoke.Line(img, new Point(x1, y1), new Point(x2, y2), new Bgr(GetColorFromIndex(colorIndex)).MCvScalar, 1);
                    x1 = x2;
                    y1 = y2;
                    x2++;
                }
            }
            else
            {
                // calculate range to plot
                int middle = img.Width / 2;

                int x1 = (int)(middle - shift - processed[0] * multiplier);
                int y1 = 0;
                int x2 = x1;
                int y2 = y1;
                foreach (double value in processed)
                {
                    x2 = (int)(middle - shift - value * multiplier);
                    CvInvoke.Line(img, new Point(x1, y1), new Point(x2, y2), new Bgr(System.Drawing.Color.Yellow).MCvScalar, 1);
                    x1 = x2;
                    y1 = y2;
                    y2++;
                }
            }
        }

        // Pixel based access to Mats. =========================================

        // GetPixelValue() allows for selection of a single pixel from a Mat regardless of the numer of planes
        // It checks whether row, col and plane are within range, and img is indeed not a null pointer.
        // If any of these tests fail, 0 is returned, or else the byte value of the given coordinates.
        public static byte GetPixelValue(Mat img, int row, int col, int plane = -1)
        {
            if (img == null || plane >= img.ElementSize) return 0;
            var value = new byte[1];
            int safeRow = Math.Clamp(row, 0, img.Rows - 1);
            int safeCol = Math.Clamp(col, 0, img.Cols - 1);
            System.IntPtr pointer = img.DataPointer + (safeRow * img.Cols + safeCol) * img.ElementSize;
            Marshal.Copy(pointer + plane, value, 0, 1);
            return value[0];
        }

        public static Pixel GetPixel(Mat img, int row, int col)
        {
            if (img == null) return new Pixel(0, 0, 0);
            var value = new byte[4];
            int safeRow = Math.Clamp(row, 0, img.Rows - 1);
            int safeCol = Math.Clamp(col, 0, img.Cols - 1);
            Marshal.Copy(img.DataPointer + (safeRow * img.Cols + safeCol) * img.ElementSize, value, 0, 4);
            return new Pixel(value);
        }

        // SetPixelValue() allows for setting of a single pixel in a Mat regardless of the numer of planes
        // It checks whether row, col and plane are within range, and img is indeed not a null pointer.
        // If any of these tests fail, img is not modified, or else the byte value of the given coordinates
        // is set to the passed in value.
        public static void SetPixelValue(Mat img, byte value, int row, int col, int plane = 0)
        {
            if (img == null || plane >= img.ElementSize) return;
            var target = new[] { value };
            int safeRow = Math.Clamp(row, 0, img.Rows - 1);
            int safeCol = Math.Clamp(col, 0, img.Cols - 1);
            System.IntPtr pointer = img.DataPointer + (safeRow * img.Cols + safeCol) * img.ElementSize;
            if (plane == -1)
            {
                Marshal.Copy(target, 0, pointer + 0, 1);
                Marshal.Copy(target, 0, pointer + 1, 1);
                Marshal.Copy(target, 0, pointer + 2, 1);
                return;
            }
            Marshal.Copy(target, 0, pointer + plane, 1);
        }

        // Color based methods. ================================================

        // ColorConversionIntToName() translates an integer input into the corresponding text for a 
        // ColorConversion in OpenCV. Default for non-matched ones is Bgr2Bgra.
        public static string ColorConversionIntToName(int input)
        {
            if (input == 0) return "Bgr2Bgra";
            if (input == 2) return "Bgr2Rgba";
            if (input == 4) return "Bgr2Rgb";
            if (input == 32) return "Bgr2Xyz";
            if (input == 36) return "Bgr2YCrCb";
            if (input == 40) return "Bgr2Hsv";
            if (input == 44) return "Bgr2Lab";
            if (input == 50) return "Bgr2Luv";
            if (input == 52) return "Bgr2Hls";
            if (input == 66) return "Bgr2HsvFull";
            if (input == 68) return "Bgr2HlsFull";
            if (input == 82) return "Bgr2Yuv";
            return "Bgr2Bgra"; // ColorConversion.Bgr2Bgra
        }

        // ColorConversionIntToName() translates a string input into the corresponding int for a 
        // ColorConversion in OpenCV. Default for non-matched ones is Bgr2Bgra.

        public static int ColorConversionNameToInt(string input)
        {
            if (input == "Bgr2Bgra") return 0;
            if (input == "Bgr2Rgba") return 2;
            if (input == "Bgr2Rgb") return 4;
            if (input == "Bgr2Xyz") return 32;
            if (input == "Bgr2YCrCb") return 36;
            if (input == "Bgr2Hsv") return 40;
            if (input == "Bgr2Lab") return 44;
            if (input == "Bgr2Luv") return 50;
            if (input == "Bgr2Hls") return 52;
            if (input == "Bgr2HsvFull") return 66;
            if (input == "Bgr2HlsFull") return 68;
            if (input == "Bgr2Yuv") return 82;
            return 0; // ColorConversion.Bgr2Bgra
        }

        // GetObjectColorName() gets the name of the color at the center of the passed in rectangle.
        public static string GetObjectColorName(Mat img, Rectangle rect)
        {
            string colorname = "Unknown";

            if (img != null)
            {
                System.Drawing.Point point = new System.Drawing.Point(rect.Left + rect.Width / 2, rect.Top + rect.Height / 2);
                return GetObjectColorNameAtPoint(img, point);
            }
            return colorname;
        }

        // GetObjectColorNameAtPoint() gets the name of the color at point passed in.
        public static string GetObjectColorNameAtPoint(Mat img, System.Drawing.Point samplePoint)
        {
            string colorname = "Unknown";

            if (img != null)
            {
                Bitmap bmp = img.ToBitmap();
                System.Drawing.Point point = samplePoint;
                Color color = bmp.GetPixel(point.X, point.Y);
                HSLColor hslcolor = new HSLColor(color);
                colorname = Utils.GetColorNameFromHSL(hslcolor);
                if (colorname.Length == 0) colorname = color.ToArgb().ToString();
            }

            return colorname;
        }

        // GetObjectHSLColorAtPoint() gets the color of a point in the image as an HSLColor.
        public static HSLColor GetObjectHSLColorAtPoint(Mat img, System.Drawing.Point samplePoint)
        {
            if (img != null)
            {
                Bitmap bmp = img.ToBitmap();
                System.Drawing.Point point = samplePoint;
                Color color = bmp.GetPixel(point.X, point.Y);
                return new HSLColor(color);
            }

            return new HSLColor(Color.Black);
        }

        // Conversions of images / arrays. =====================================

        public static void Bitmap2BitmapSource(System.Drawing.Bitmap bitmap, out BitmapSource outSource)
        {
            var bitmapData = bitmap.LockBits(
                new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height),
                System.Drawing.Imaging.ImageLockMode.ReadOnly, bitmap.PixelFormat);

            outSource = BitmapSource.Create(
                bitmapData.Width, bitmapData.Height,
                bitmap.HorizontalResolution, bitmap.VerticalResolution,
                System.Windows.Media.PixelFormats.Bgr24, null,
                bitmapData.Scan0, bitmapData.Stride * bitmapData.Height, bitmapData.Stride);

            if (bitmap == null) return;

            bitmap.UnlockBits(bitmapData);

            return;
        }

        public static Mat Bitmap2Mat(Bitmap bmp)
        {
            Mat _Mat;
            try
            {
                Emgu.CV.Image<Bgr, UInt16> ImageEmgu = new Image<Bgr, UInt16>(bmp.Width, bmp.Height);
                ImageEmgu = BitmapExtension.ToImage<Bgr, UInt16>(bmp);
                _Mat = ImageEmgu.Mat;
                _Mat.ConvertTo(_Mat, Emgu.CV.CvEnum.DepthType.Cv8U);
            }
            catch
            {
                return null;
            }
            return _Mat;
        }

        // BitmapSource2Bitmap() converts a BitmapSource into a Bitmap.
        public static Bitmap BitmapSource2Bitmap(BitmapSource bitmapsource)
        {
            Bitmap bitmap;
            using (MemoryStream outStream = new MemoryStream())
            {
                BitmapEncoder enc = new BmpBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(bitmapsource));
                enc.Save(outStream);
                bitmap = new Bitmap(outStream);
            }
            return bitmap;
        }

        public static UMat Mat2UMat(Mat input, int channel)
        {
            UMat output = new();
            if (input == null || channel < 0 || channel >= input.NumberOfChannels) return output;
            using (VectorOfUMat vom = new())
            {
                CvInvoke.Split((IInputArray)input, vom);
                output = vom[channel];
            }
            return output.Clone();
        }

        public static Mat UMats2Mat(UMat input0, UMat input1, UMat input2)
        {
            Mat output = new();
            using (VectorOfUMat inputs = new())
            {
                if (input0 != null)
                    inputs.Push(input0);
                if (input1 != null)
                    inputs.Push(input1);
                if (input2 != null)
                    inputs.Push(input2);
                CvInvoke.Merge(inputs, output);
            }

            return output;
        }

        // Functions for detections. ===========================================

        // DetermineAvoidanceWarning() calculates collision avoidance numbers from an input image.
        // It does so by examining from the bottom line of the image, how far it can go forward, 
        // until it reached too narrow a space, and also calculates the direction of travel:
        // Distance: 0...1, as representing the fraction of the image height that one can travel forward.
        // Direction: -1...1, as representing the direction between hard left and hard right.
        public static Mat DetermineAvoidanceWarning(Mat imgIn, out double distance, out double direction, bool showImages = false)
        {
            distance = 0;
            direction = 0;
            double maxleft = 0;
            double maxright = 0;
            int width = imgIn.Width;
            int height = imgIn.Height;
            Mat imgOut = imgIn.Clone();
            if (showImages)
                CvInvoke.Imshow("input image", imgIn);

            int middle = width / 2;
            int shiftleft = middle - 1;
            int shiftright = middle - 1;
            for (int row = height - 1; row >= 0; row--)
            {
                // Calculate number of pixels free on left and right side
                int range = 0;
                while (middle - range >= 0 &&
                       middle + range < width &&
                       GetPixelValue(imgIn, row, middle - range) == 0 &&
                       GetPixelValue(imgIn, row, middle + range) == 0)
                {
                    range++;
                    // SetPixelValue(imgOut, 128, row, middle - range);
                    // SetPixelValue(imgOut, 128, row, middle + range);
                }
                distance = (double)(height - row) / (double)height;
                if (range < 5)
                {
                    break;
                }

                // if left obstacle found, determine how far right is still free
                shiftleft = range;
                while (middle - shiftleft >= 0 && GetPixelValue(imgIn, row, middle - shiftleft) == 0)
                {
                    shiftleft++;
                    // SetPixelValue(imgOut, 128, row, middle - shiftleft);
                }

                // if right obstacle found, determine how far right is still free
                shiftright = range;
                while (middle + shiftright < width && GetPixelValue(imgIn, row, middle + shiftright) == 0)
                {
                    shiftright++;
                    // SetPixelValue(imgOut, 128, row, middle + shiftright);
                }

                // shift middle to be adjusted for offset free space
                int shift = (shiftright - shiftleft) / 2;
                middle += shift;
                double olddirection = direction;
                direction = ((double)middle / (double)width - 0.5) * 2;
                if (direction < 0)
                {
                    maxleft = Math.Min(maxleft, direction);
                }
                else
                {
                    maxright = Math.Max(maxright, direction);
                }

                double divergence = (double)(shiftleft + shiftright) / (double)width;
                if (Math.Abs(divergence) < 0.20)
                {
                    break;
                }
            }
            if (-maxleft > maxright)
            {
                direction = maxleft;
            }
            else
            {
                direction = maxright;
            }
            if (showImages)
                CvInvoke.Imshow("AVOIDANCE CONE", imgOut);

            return imgOut;
        }

        // Image manipulation. =================================================

        // FloodFill() tries to fill an image at a given point, but samples the original color beforehand and returns it.
        public static bool FloodFill(Mat img, System.Drawing.Point seedPoint, out HSLColor colorAtSeedPoint,
                                     int lo = -10, int hi = 10, bool darkFill = false)
        {
            colorAtSeedPoint = new HSLColor(Color.Black);
            if (img == null) return false;
            Color fill = Color.White;
            if (darkFill) fill = Color.Black;
            if (seedPoint.X <= 0 || seedPoint.X > img.Width - 1 ||
                seedPoint.Y <= 0 || seedPoint.Y > img.Height - 1)
            {
                return false;
            }
            colorAtSeedPoint = GetObjectHSLColorAtPoint(img, seedPoint);
            colorAtSeedPoint.saturation = 0.75f;
            colorAtSeedPoint.luminance = 0.5f;
            CvInvoke.FloodFill(img, null, seedPoint, new Bgr(fill).MCvScalar, out Rectangle rect2,
                               new MCvScalar(lo, lo, lo), new MCvScalar(hi, hi, hi));
            return true;
        }

        // Scoring functions. ==================================================

        // PercentageOverlap() takes two rectangles and calculates the percentage to which they overlap. 
        // a value of 1.0 means one of them is inside the other
        public static double PercentageOverlap(System.Drawing.Rectangle one, System.Drawing.Rectangle two)
        {
            System.Drawing.Rectangle inter = one;
            inter.Intersect(two);
            double result = 0;
            double commonArea = (double)inter.Width * (double)inter.Height;
            double oneArea = (double)one.Width * (double)one.Height;
            double twoArea = (double)two.Width * (double)two.Height;
            result = Math.Max(commonArea / oneArea, commonArea / twoArea);
            return result;
        }

        public static void SaveImageIf(string folder, string filename, Mat image, bool saveImage = true)
        {
            if (saveImage)
            {
                try
                {
                    string outputFolder = Utils.GetOrAddDocumentsSubFolder("ImageProcessingOutput");
                    string filespec = Path.Combine(folder, filename);
                    CvInvoke.Imwrite(filespec, image);
                }
                catch
                {
                }
            }
        }

        public static void SaveImageIf(string folder, string filename, UMat image, bool saveImage = true)
        {
            if (saveImage)
            {
                string outputFolder = Utils.GetOrAddDocumentsSubFolder("ImageProcessingOutput");
                string filespec = Path.Combine(folder, filename);
                CvInvoke.Imwrite(filespec, image);
            }
        }

        // TouchesBoundaries() returns true if the passed in bounding rectangle touches
        // the sides of the image with the given width and height.
        internal static bool TouchesBoundaries(Rectangle rectangle, int width, int height)
        {
            if (rectangle.X == 0 || 
                rectangle.Y == 0 ||
                rectangle.X + rectangle.Width == width ||
                rectangle.Y + rectangle.Height == height)
            {
                return true;
            }
            return false;
        }

        public static void DrawContourPoints(Mat output, VectorOfPoint approxContour)
        {
            if (approxContour == null || output == null) return;
            int count = approxContour.Size;
            for (int i = 0; i < count; i++)
            {
                Rectangle rect = new(approxContour[i].X - 1, approxContour[i].Y - 1, 2, 2);
                CvInvoke.Rectangle(output, rect, new Bgr(System.Drawing.Color.White).MCvScalar, 1);
            }
        }
    }
}