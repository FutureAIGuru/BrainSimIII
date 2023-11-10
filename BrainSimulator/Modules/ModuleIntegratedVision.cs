//
// Copyright (c) FutureAI. All rights reserved.  
// Code is proprietary and may not be distributed without a separate licence
//  

using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using HelixToolkit.Wpf;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace BrainSimulator.Modules
{
    public class ModuleIntegratedVision : ModuleBase
    {
        //any public variable you create here will automatically be saved and restored  with the network
        //unless you precede it with the [XmlIgnore] directive
        //[XmlIgnore] 
        //public theStatus = 1;
        public static readonly double NoMatchLimit = 4.8;
        public string SettingsFilename { get; set; }

        [XmlIgnore] public Mat ImageToRecognize { get; set; }
        [XmlIgnore] public Mat ContourOutput { get; set; }
        [XmlIgnore] public string FileToRecognize { get; set; }
        [XmlIgnore] public bool LoadSettingAfterNetworkLoad { get; set; }
        [XmlIgnore] public Mat AutoMask { get; set; } = new Mat();
        [XmlIgnore] public Mat MaskedOriginal { get; set; } = new Mat();
        [XmlIgnore] public bool SaveDebugImages { get; set; }
        [XmlIgnore] public bool UpdateDialogImage { get; set; } = true;
        [XmlIgnore] public bool CurrentImageProcessed { get; set; } = true;
        [XmlIgnore] public Mat ImgToShowOnDialog { get; set; }
        [XmlIgnore] public static double StopwatchResult { get; set; }
        [XmlIgnore] public Mat InputCopyToDisplay { get; set; }
        [XmlIgnore] public int ViewSelect { get; set; } = 3;

        private static ImageArray Pixels;
        private VectorOfVectorOfPoint ValidContours { get; set; } = new();
        private VectorOfVectorOfPoint CandidateContours { get; set; } = new();

        private bool ShouldAddUnknownAreasToKnownArea = true;

        Thing candidateOuterUnknowAreaThing;
        Thing matchingOuterUnknownThing;
        Thing matchingInnerUnknownThing;
        Thing matchingKnownThing;

        UnknownArea mostRecentOuterUnknownArea;
        UnknownArea currentAreaToProcess;

        KnownArea knownArea;

        double outerUnknownMinScore;

        bool exactMatch;

        public ModuleIntegratedVision()
        {
            minHeight = 1;
            maxHeight = 500;
            minWidth = 1;
            maxWidth = 500;
        }

        // fill this method in with code which will execute
        // once for each cycle of the engine
        public override void Fire()
        {
            Init();  //be sure to leave this here

            // Make sure we don't process multiple times...      
            if (FileToRecognize == null || FileToRecognize.Length == 0) return;
            InputCopyToDisplay = ImageToRecognize.Clone();

            SetupForNextFrame();

            if (CurrentImageProcessed)
            {
                ImageToRecognize = null;
                FileToRecognize = "";
                return;
            }

            try
            {
                CreateCameraMask();
                FindAndFilterContours();
                MatchAndCompleteUnknownAreasInUKS();
                SetRecognitionCompleteInUKS();
                ShowAndSaveResults();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }

            StopwatchResult = Sallie.StopCentralStopWatch();
            Sallie.StartCentralStopWatch();

            UpdateDialogImage = true;
            CurrentImageProcessed = true;

            UpdateDialog();
        }

        private void CreateCameraMask()
        {
            if (ImageToRecognize == null) return;
            Pixels = new ImageArray(ImageToRecognize);
            AutoMask = Pixels.CloneFromChanges();
            CvInvoke.CvtColor(AutoMask, AutoMask, ColorConversion.Bgr2Gray);
        }

        private void FindAndFilterContours()
        {
            ValidContours.Clear();
            CandidateContours = new();
            CvInvoke.GaussianBlur(AutoMask, AutoMask, new System.Drawing.Size(3, 3), 2);
            CvInvoke.FindContours(AutoMask, CandidateContours, null, RetrType.Tree, ChainApproxMethod.ChainApproxSimple);
            for (int i = 0; i < CandidateContours.Size; i++)
            {
                VectorOfPoint contourToAdd = CleanupContour(CandidateContours[i]);
                if (contourToAdd != null)
                    ValidContours.Push(contourToAdd);
            }
        }

        private VectorOfPoint CleanupContour(VectorOfPoint detailedContour)
        {
            VectorOfPoint approximatedContour = new();
            double ApproxValue = 2;
            CvInvoke.ApproxPolyDP(detailedContour, approximatedContour, ApproxValue, true);
            System.Drawing.Rectangle rect = CvInvoke.BoundingRectangle(approximatedContour);

            double minSizeFactor = 0.03;
            if (rect.Width < ImgUtils.ProcessingSize.Width * minSizeFactor ||
                rect.Height < ImgUtils.ProcessingSize.Height * minSizeFactor ||
                ImgUtils.TouchesBoundaries(rect, ImageToRecognize.Width, ImageToRecognize.Height))
                return null;
            return approximatedContour;
        }

        private void ShowAndSaveResults()
        {
            if (ImageToRecognize == null)
                return;
            ContourOutput = ImageToRecognize.Clone();
            int count = ValidContours.Size;
            for (int i = 0; i < count; i++)
            {
                AnnotateContour(i);
            }

            SetImageToShow();
            SaveImagesIfNeeded();
        }

        private void SaveImagesIfNeeded()
        {
            if (!SaveDebugImages)
                return;
            string folder = Utils.GetOrAddDocumentsSubFolder("ImageProcessingOutput");
            ImgUtils.SaveImageIf(folder, "InputImage.jpg", ImageToRecognize, SaveDebugImages);
            ImgUtils.SaveImageIf(folder, "MaskImage.jpg", AutoMask, SaveDebugImages);
            ImgUtils.SaveImageIf(folder, "Contours.jpg", ContourOutput);
        }

        private void SetImageToShow()
        {
            switch (ViewSelect)
            {
                case 1:
                    ImgToShowOnDialog = InputCopyToDisplay.Clone();
                    break;
                case 2:
                    ImgToShowOnDialog = AutoMask.Clone();
                    break;
                default:
                    ImgToShowOnDialog = ContourOutput.Clone();
                    break;
            }
        }

        private void AnnotateContour(int index,
                                     bool drawContours = true,
                                     bool drawBoxes = true,
                                     bool drawNumbers = false,
                                         bool drawPoints = false)
        {
            System.Drawing.Rectangle sizer = CvInvoke.BoundingRectangle(ValidContours[index]);

            if (drawContours)
                CvInvoke.DrawContours((IInputOutputArray)ContourOutput, ValidContours, index, new MCvScalar(0, 0, 255), 1);

            if (drawBoxes)
                CvInvoke.Rectangle((IInputOutputArray)ContourOutput, sizer, new Bgr(System.Drawing.Color.Yellow).MCvScalar, 1);

            if (drawNumbers)
                CvInvoke.PutText((IInputOutputArray)ContourOutput, index.ToString(), sizer.Location, FontFace.HersheyPlain, 1, new MCvScalar(0, 0, 255), 1);

            if (drawPoints)
                ImgUtils.DrawContourPoints(ContourOutput, ValidContours[index]);
        }

        private void GetOrAddBestMatchingOuterUnknownAreaThing()
        {
            GetUKS();
            Thing knownAreasRoot = UKS.GetOrAddThing("KnownAreas", "Visual");
            if (knownAreasRoot == null)
            {
                matchingOuterUnknownThing = null;
                return;
            }

            outerUnknownMinScore = ModuleIntegratedVision.NoMatchLimit;
            candidateOuterUnknowAreaThing = null;
            foreach (Thing knownAreaThing in knownAreasRoot.Children)
            {
                CheckAllOuterUnknownsOfKnownArea(knownAreaThing);
            }
            if (outerUnknownMinScore < 0.01)
            {
                matchingOuterUnknownThing = candidateOuterUnknowAreaThing;
                exactMatch = true;
                return;
            }
            if (candidateOuterUnknowAreaThing != null)
            {
                matchingOuterUnknownThing = candidateOuterUnknowAreaThing;
                exactMatch = false;
                return;
            }

            matchingOuterUnknownThing = new Thing();
            matchingOuterUnknownThing.V = currentAreaToProcess;
            matchingOuterUnknownThing.Label = "UnknownArea" + currentAreaToProcess.TrackID;
        }

        private void CheckAllOuterUnknownsOfKnownArea(Thing knownAreaThing)
        {
            foreach (Thing outerUnknownAreaThing in knownAreaThing.Children)
            {
                EvaluateCandidateOuterUnknown(outerUnknownAreaThing);
            }
        }

        private void EvaluateCandidateOuterUnknown(Thing outerUnknownAreaThing)
        {
            if (outerUnknownAreaThing.V is not UnknownArea outerUnknown)
                return;
            double candidateScore = outerUnknown.ScoreFor(currentAreaToProcess, NoMatchLimit);
            if (candidateScore <= outerUnknownMinScore &&
                candidateScore < NoMatchLimit - 0.1)
            {
                outerUnknownMinScore = candidateScore;
                candidateOuterUnknowAreaThing = outerUnknownAreaThing;
            }
        }

        private void MatchAndCompleteUnknownAreasInUKS()
        {
            int mostRecentOuterIndex = 0;
            mostRecentOuterUnknownArea = null;
            bool firstContour = true;

            GetUKS();
            Thing FrameNowRoot = UKS.GetOrAddThing("FrameNow", "Visual");
            if (FrameNowRoot == null) return;

            for (int i = 0; i < ValidContours.Size; i++)
            {
                Angle Pan = Utils.GetCameraPanFromAnnotatedImageFileName(FileToRecognize);
                Angle Tilt = Utils.GetCameraTiltFromAnnotatedImageFileName(FileToRecognize);
                currentAreaToProcess = GetContourAsArea(i, Pan, Tilt);
                exactMatch = false;

                bool outerContour = !currentAreaToProcess.IsMostlyInsideOf(mostRecentOuterUnknownArea);
                if (firstContour || outerContour)
                {
                    firstContour = false;
                    currentAreaToProcess.isOuterContour = true;
                    currentAreaToProcess.SetDetailImage(ImageToRecognize, ValidContours[i]);

                    GetOrAddBestMatchingOuterUnknownAreaThing();
                    GetOrAddKnownArea();
                    AddOuterUnknownAreaToKnownArea(currentAreaToProcess);

                    if (matchingOuterUnknownThing != null)
                    {
                        mostRecentOuterUnknownArea = matchingOuterUnknownThing.V as UnknownArea;
                    }
                    else
                    {
                        mostRecentOuterUnknownArea = null;
                    }
                    mostRecentOuterIndex = i;
                    if (matchingOuterUnknownThing != null)
                        FrameNowRoot.AddChild(matchingOuterUnknownThing);
                }
                else if (!exactMatch)
                {
                    currentAreaToProcess.isOuterContour = false;
                    GetOrAddAddInnerUnknownArea("UnknownArea" + (int.Parse(mostRecentOuterUnknownArea.TrackID) - mostRecentOuterIndex + i),
                                                currentAreaToProcess);
                    if (matchingInnerUnknownThing != null)
                        FrameNowRoot.AddChild(matchingInnerUnknownThing);
                }
            }
        }

        private void GetOrAddAddInnerUnknownArea(string label, UnknownArea area)
        {
            if (matchingOuterUnknownThing == null)
                return;
            matchingInnerUnknownThing = UKS.GetOrAddThing(label, matchingOuterUnknownThing, area);
        }

        private void GetOrAddKnownArea()
        {
            if (matchingOuterUnknownThing == null) return;
            if (matchingOuterUnknownThing.Parents.Count != 0)
            {
                matchingKnownThing = matchingOuterUnknownThing.Parents[0];
                knownArea = matchingKnownThing.V as KnownArea;
            }
        }

        private void AddKnownAreaIfNeeded(UnknownArea areaFromContour)
        {
            GetUKS();
            Thing KnownAreasRoot = UKS.GetOrAddThing("KnownAreas", "Visual");
            if (KnownAreasRoot == null) return;
            if (knownArea == null || matchingOuterUnknownThing.Parents.Count == 0)
            {
                knownArea = new KnownArea(areaFromContour);
                matchingKnownThing = UKS.GetOrAddThing("KnownArea" + areaFromContour.TrackID, KnownAreasRoot, knownArea);
            }
        }

        private void AddOuterUnknownAreaToKnownArea(UnknownArea areaFromContour)
        {
            if (ShouldAddUnknownAreasToKnownArea && !exactMatch)
            {
                AddKnownAreaIfNeeded(areaFromContour);
                knownArea.AddUnknownArea(areaFromContour);
                matchingOuterUnknownThing = UKS.GetOrAddThing("UnknownArea" + areaFromContour.TrackID, matchingKnownThing, areaFromContour);
            }
        }

        private void SetRecognitionCompleteInUKS()
        {
            GetUKS();
            Thing FrameNowRoot = UKS.GetOrAddThing("FrameNow", "Visual");
            if (FrameNowRoot == null) return;
            System.Drawing.Rectangle rect = new(0, 0, ImageToRecognize.Width, ImageToRecognize.Height);
            SetMovementParametersInFrameNow(rect, FileToRecognize);
            UKS.GetOrAddThing("FrameRecognized", FrameNowRoot);
            ShouldAddUnknownAreasToKnownArea = false;
        }

        private void SetMovementParametersInFrameNow(System.Drawing.Rectangle imageRect, string annotatedFilename)
        {
            double Width = imageRect.Width;
            double Height = imageRect.Height;
            Angle BodyAngleDelta = Utils.GetTurnDeltaFromAnnotatedImageFileName(annotatedFilename);
            double BodyMovementDelta = Utils.GetMoveDeltaFromAnnotatedImageFileName(annotatedFilename);
            Angle CameraPan = Utils.GetCameraPanFromAnnotatedImageFileName(annotatedFilename);
            Angle CameraTilt = Utils.GetCameraTiltFromAnnotatedImageFileName(annotatedFilename);

            GetUKS();
            Thing frameNowRoot = UKS.GetOrAddThing("FrameNow", "Visual");
            if (frameNowRoot == null) return;

            UKS.AddThing("FrameFilename", frameNowRoot, System.IO.Path.GetFileNameWithoutExtension(annotatedFilename));
            UKS.AddThing("FrameWidth", frameNowRoot, Width);
            UKS.AddThing("FrameHeight", frameNowRoot, Height);
            UKS.AddThing("FrameBodyAngleDelta", frameNowRoot, BodyAngleDelta);
            UKS.AddThing("FrameBodyMovementDelta", frameNowRoot, BodyMovementDelta);
            UKS.AddThing("FrameCameraPan", frameNowRoot, CameraPan);
            // UKS.AddThing("FrameCameraTilt", frameNowRoot, CameraTilt);
            UKS.AddThing("FrameCameraTilt", frameNowRoot, 0);
        }

        // This routine saves the last two frames of information and clears the new frame
        private void SetupForNextFrame()
        {
            GetUKS();
            Thing FrameNowRoot = UKS.GetOrAddThing("FrameNow", "Visual");
            if (FrameNowRoot != null)
            {
                UKS.DeleteAllChildren(FrameNowRoot);
            }
            UKS.GetOrAddThing("FrameNow", "Visual");
        }

        public override void SetInputImage(Mat inputImage, string inputFilename)
        {
            if (inputImage == null) return;
            ImageToRecognize = inputImage.Clone();
            FileToRecognize = inputFilename;
            CurrentImageProcessed = false;
            if (Utils.ImageHasMovement(FileToRecognize))
            {
                ShouldAddUnknownAreasToKnownArea = true;
            }
        }

        // Returns the contour at index as a single UnknownArea object
        private UnknownArea GetContourAsArea(int index, Angle CameraPan, Angle CameraTilt)
        {
            if (index >= 0 || index < ValidContours.Size)
            {
                return new UnknownArea(ImageToRecognize.ToBitmap(), ValidContours[index], CameraPan, CameraTilt);
            }
            return null;
        }

        //fill this method in with code which will execute once
        //when the module is added, when "initialize" is selected from the context menu,
        //or when the engine restart button is pressed
        public override void Initialize()
        {
        }

        //the following can be used to massage public data to be different in the xml file
        //delete if not needed
        public override void SetUpBeforeSave()
        {
        }

        public override void SetUpAfterLoad()
        {
            ShouldAddUnknownAreasToKnownArea = true;
            if (SettingsFilename != null && SettingsFilename.Length != 0)
            {
                LoadSettingAfterNetworkLoad = true;
                UpdateDialog();
            }
        }

        public override void UKSInitializedNotification()
        {
            ShouldAddUnknownAreasToKnownArea = true;
        }

        //called whenever the size of the module rectangle changes
        //for example, you may choose to reinitialize whenever size changes
        //delete if not needed
        public override void SizeChanged()
        {
            if (mv == null) return; //this is called the first time before the module actually exists
        }
    }

    // ImageArray is a combined class where a Mat object is translated into an array of Pixels
    // for better interfacing and performance during various manipulations. 
    public class ImageArray
    {
        private readonly int Width;
        private readonly int Height;
        private readonly Mat Image;
        private readonly Pixel[,] Pixels;

        // Constructor getting a Mat as input, from which image, width and height are set.
        // It subsequently calls ImageToArray() to create a consistent object.
        public ImageArray(Mat img)
        {
            Image = img;
            Width = Image.Width;
            Height = Image.Height;
            Pixels = new Pixel[Height, Width];
            Image2Array();
            CalculateChanges();
        }

        // Image2Array fills the Pixel array from the actual color values in the image
        public void Image2Array()
        {
            if (Image == null) return;

            // for (int row = 0; row < Height; row++)
            Parallel.For(0, Height, row =>
            {
                for (int col = 0; col < Width; col++)
                {
                    Pixels[row, col] = GetPixelFromImage(row, col);
                }
            });
        }

        // Array2Image updates the image from the values in the Pixel Array
        public void Array2Image()
        {
            if (Image == null) return;

            // for (int row = 0; row < Height; row++)
            Parallel.For(0, Height, row =>
            {
                for (int col = 0; col < Width; col++)
                {
                    SetPixelInImage(Pixels[row, col], row, col);
                }
            });
        }

        // CalculateChanges updates the change coefficient once the image is complete.
        public void CalculateChanges()
        {
            if (Image == null) return;

            // for (int row = 0; row < Height; row++)
            Parallel.For(0, Height, row =>
            {
                for (int col = 0; col < Width; col++)
                {
                    Pixels[row, col].Change = CalculateChange(row, col);
                }
            });
        }

        // CloneFromArray creates an image clone straight from the array values
        public Mat CloneFromArray()
        {
            if (Image == null) return new Mat();

            Mat clone = Image.Clone();
            // for (int row = 0; row < Height; row++)
            Parallel.For(0, Height, row =>
            {
                for (int col = 0; col < Width; col++)
                {
                    SetPixelInImage(Pixels[row, col], row, col, clone);
                }
            });
            return clone;
        }

        // CloneFromChanges creates an image from the change values of the image
        public Mat CloneFromChanges()
        {
            if (Image == null) return new Mat();

            Mat clone = Image.Clone();
            // for (int row = 0; row < Height; row++)
            Parallel.For(0, Height, row =>
            {
                for (int col = 0; col < Width; col++)
                {
                    Pixel current = Pixels[row, col];
                    double edge = current.Change;
                    byte gray = (byte)Math.Clamp(edge, 0, 255);
                    Pixel heatmap = new Pixel(gray, gray, gray);
                    SetPixelInImage(heatmap, row, col, clone);
                }
            });
            return clone;
        }

        // GetPixelFromImage gets a Pixel from the original image.
        public Pixel GetPixelFromImage(int row, int col)
        {
            const int pack = 3;
            if (Image == null) return new Pixel(0, 0, 0);
            var value = new byte[pack];
            int safeRow = Math.Clamp(row, 0, Height - 1);
            int safeCol = Math.Clamp(col, 0, Width - 1);
            Marshal.Copy(Image.DataPointer + (safeRow * Width + safeCol) * Image.ElementSize,
                         value, 0, pack);
            return new Pixel(value);
        }

        // GetPixelFromArray gets a Pixel from the Pixel Array, which is 
        // about three times faster than GetPixelFromImage()
        public Pixel GetPixelFromArray(int row, int col)
        {
            int safeRow = Math.Clamp(row, 0, Height - 1);
            int safeCol = Math.Clamp(col, 0, Width - 1);
            return Pixels[safeRow, safeCol];
        }

        // SetPixelInImage sets a Pixel in the original Mat that the ImageArray was created from.
        public void SetPixelInImage(Pixel pixel, int row, int col, Mat clone = null)
        {
            if (clone == null) clone = Image;
            if (clone == null) return;
            var target = new[] { pixel.Blue, pixel.Green, pixel.Red };
            int safeRow = Math.Clamp(row, 0, Height - 1);
            int safeCol = Math.Clamp(col, 0, Width - 1);
            System.IntPtr pointer = clone.DataPointer + (safeRow * Width + safeCol) * clone.ElementSize;
            Marshal.Copy(target, 0, pointer + 0, 3);
        }

        // SetPixelInArray sets a Pixel in the Array that represents the image, but it is around
        // four times faster than SetPixelInImage()
        public void SetPixelInArray(Pixel pixel, int row, int col)
        {
            if (pixel == null) return;
            int safeRow = Math.Clamp(row, 0, Height - 1);
            int safeCol = Math.Clamp(col, 0, Width - 1);
            Pixels[safeRow, safeCol] = pixel;
        }

        private int CalculateChange(int row, int col)
        {
            int maxPositive = 0;
            Pixel center = GetPixelFromArray(row, col);
            maxPositive = Math.Max(maxPositive, center.GetPixelDifference(GetPixelFromArray(row, col - 1)));
            maxPositive = Math.Max(maxPositive, center.GetPixelDifference(GetPixelFromArray(row, col + 1)));
            maxPositive = Math.Max(maxPositive, center.GetPixelDifference(GetPixelFromArray(row - 1, col)));
            maxPositive = Math.Max(maxPositive, center.GetPixelDifference(GetPixelFromArray(row + 1, col)));
            maxPositive = Math.Max(maxPositive, center.GetPixelDifference(GetPixelFromArray(row - 1, col - 1)));
            maxPositive = Math.Max(maxPositive, center.GetPixelDifference(GetPixelFromArray(row - 1, col + 1)));
            maxPositive = Math.Max(maxPositive, center.GetPixelDifference(GetPixelFromArray(row + 1, col - 1)));
            maxPositive = Math.Max(maxPositive, center.GetPixelDifference(GetPixelFromArray(row + 1, col + 1)));
            return maxPositive;
        }
    }

    public class Pixel
    {
        public const int CH_GRAY = -1;
        public const int CH_BLUE = 0;
        public const int CH_GREEN = 1;
        public const int CH_RED = 2;

        public byte Blue { get; set; }
        public byte Green { get; set; }
        public byte Red { get; set; }
        public int Change { get; set; }

        // Constructs a Pixel from its component value bytes.
        public Pixel(byte B, byte G, byte R)
        {
            Blue = B;
            Green = G;
            Red = R;
        }

        // Constructs a Pixel from a three byte value,
        // as copied from an image in memory.
        public Pixel(byte[] values)
        {
            if (values == null) return;
            Blue = values[0];
            Green = values[1];
            Red = values[2];
        }

        // These values determine the behavior of GetPixelDifference()
        public static int Sensitivity = 3000;
        public static int Threshold = 155;

        public int GetPixelDifference(Pixel other)
        {
            double v = Math.Abs(Blue - other.Blue) + Math.Abs(Green - other.Green) + Math.Abs(Red - other.Red);

            int endvalue = (int)(Math.Clamp(v * v * v / Sensitivity, 0, 255));
            if (endvalue < Threshold) return 0;
            return 255;
        }

        public double Intensity()
        {
            return (Blue + Green + Red) / 3.0;
        }

        public override string ToString()
        {
            return "Pixel(" + Blue + ", " + Green + ", " + Red + ", C:" + Change + ")";
        }
    }
}
