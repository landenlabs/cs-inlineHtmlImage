using System;
using System.IO;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Reflection;
using System.Drawing.Imaging;
using System.Collections.Generic;
using System.Drawing.Drawing2D;


namespace InlineHtmlImages_NS
{
    /// <summary>
    /// ConvertHtml - main class to encode (embed) and decode (extract) images to/from Html.
    /// 
    /// Author: Dennis Lang - 2014
    /// https://landenlabs.com/
    ///
    /// This file is part of InlineHtmlImages.
    ///
    /// InlineHtmlImages is free software: you can redistribute it and/or modify
    /// it under the terms of the GNU General Public License as published by
    /// the Free Software Foundation, either version 3 of the License, or
    /// (at your option) any later version.
    ///
    /// InlineHtmlImages is distributed in the hope that it will be useful,
    /// but WITHOUT ANY WARRANTY; without even the implied warranty of
    /// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    /// GNU General Public License for more details.
    ///
    /// The GNU General Public License: <http://www.gnu.org/licenses/>.

    /// </summary>
    class ConvertHtml
    {
        Regex imagePat = new Regex("(<img [^>]+|image: *url[^)]+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        Regex imageSrcPat = new Regex("src=(\"[^\"]+\"|'[^']+'| *[^ ]+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        Regex imageAltPat = new Regex("alt=(\"[^\"]+\"|'[^']+'| *[^ ]+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        Regex imageWidthPat = new Regex("width=(\"[^\"]+\"|'[^']+'| *[^ ]+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        Regex imageHeightPat = new Regex("height=(\"[^\"]+\"|'[^']+'| *[^ ]+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        Regex imageUrlPat = new Regex(@"url\(([^)]+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public ConvertHtml()
        {
            okayToOverwrite = false;
        }

        public bool okayToOverwrite
        {
            get;
            set;
        }

        private void ThrowIfExists(string outFile)
        {
            if (!okayToOverwrite && File.Exists(outFile))
                throw new IOException(string.Format("{0} already exists", outFile));
        }
        private TextWriter CreateWriter(string outFile)
        {
            ThrowIfExists(outFile);
            return File.CreateText(outFile);
        }

        /// <summary>
        /// Implement HasFlag - only available in .Net 4.0
        /// </summary>
        /// <param name="eValue"></param>
        /// <param name="testValue"></param>
        /// <returns></returns>
        static private bool HasFlag(Enum eValue, Enum testValue)
        {
            int iValue = (int)Convert.ChangeType(eValue, eValue.GetTypeCode());
            int iTest = (int)Convert.ChangeType(testValue, testValue.GetTypeCode());
            return (iValue & iTest) != 0;
        }

        /// <summary>
        /// Return image depth 8=has Alpha, 0=no alpha
        /// </summary>
        /// <param name="image"></param>
        /// <returns></returns>
        private static int GetImageDepth(Image image)
        {
#if NET45
            // Define NET45 in Build property page if you are able to build .NET 4.5
            return Image.GetPixelFormatSize(image.PixelFormat);
#else
            var flags = (ImageFlags)image.Flags;
            // bool hasAlpha = flags.HasFlag(ImageFlags.HasAlpha); // .NET 4.0
            bool hasAlpha = HasFlag(flags, ImageFlags.HasAlpha);
            return hasAlpha ? 8 : 0;    // Guess alpha encoded in 8bits.
#endif
        }

        const string dim3fmt = "Dim:{0,6:N0} x {1,5:N0} x {2,2:N0}";
        const string dim2fmt = "Dim:{0,6:N0} x {1,5:N0}     ";
        private static string GetImageDimStr(Image image)
        {
            int depth = GetImageDepth(image);
            if (depth != 0)
                return string.Format(dim3fmt, image.Width, image.Height, depth);
            else
                return string.Format(dim2fmt, image.Width, image.Height);
        }
        private static string GetImageDimStr(ImageInfo imageInfo)
        {
            if (imageInfo.depth != 0)
                return string.Format(dim3fmt, imageInfo.width, imageInfo.height, imageInfo.depth);
            else
                return string.Format(dim2fmt, imageInfo.width, imageInfo.height);
        }

        private static string GetMatch(Match match, int idx) 
        {
            return (match != null && match.Groups.Count > idx) ?
                match.Groups[idx].Value.Trim().Replace("'", "").Replace("\"", "") : string.Empty;
        }               

        struct ImageInfo
        {
            public override int GetHashCode()
            {
                return name.GetHashCode();
            }
            public override bool Equals(object obj)
            {
                if (obj == null || !(obj is ImageInfo))
                    return false;
                ImageInfo rhs = (ImageInfo)obj;
                return name.Equals(rhs.name);
            }


            public string name;
            public string fType;    // format type (png, gif, jpg, ...)
            public string pType;    // pixel type
            public int width, height, depth;
            public int byteCnt;
        };
        struct ImageUse
        {
            public int width, height;
        };

        /// <summary>
        /// Convert Image to base 64 encoding.
        /// Following code to convert C# images found at:
        /// http://www.dailycoding.com/Posts/convert_image_to_base64_string_and_base64_string_to_image.aspx
        /// </summary>
        /// <param name="image"></param>
        /// <param name="format"></param>
        /// <returns></returns>
        private static string ImageToBase64(Image image, System.Drawing.Imaging.ImageFormat format)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                // Convert Image to byte[]
                if (GetImageType(format) == sUnknownImageType)
                    format = ImageFormat.Png;
#if true
                image.Save(ms, format);
#else
                EncoderParameters encoderParameters = new EncoderParameters(1);
                encoderParameters.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 0L);
                ImageCodecInfo encoder =
                    Array.Find(ImageCodecInfo.GetImageEncoders(), p => p.FormatID == format.Guid);
                image.Save(ms, encoder, encoderParameters);
#endif
                byte[] imageBytes = ms.ToArray();

                // Convert byte[] to Base64 String
                string base64String = Convert.ToBase64String(imageBytes);
                return base64String;
            }
        }

        /// <summary>
        /// Convert base 64 data back to an image.
        /// </summary>
        /// <param name="base64String"></param>
        /// <returns></returns>
        private static Image Base64ToImage(string base64String)
        {
            // Convert Base64 String to byte[]
            byte[] imageBytes = Convert.FromBase64String(base64String);
            MemoryStream ms = new MemoryStream(imageBytes, 0, imageBytes.Length);

            // Convert byte[] to Image
            ms.Write(imageBytes, 0, imageBytes.Length);
            Image image = Image.FromStream(ms, true);
            return image;
        }

        /// <summary>
        /// Compute image size which fits in max dimensions while keeping aspect ratio.
        /// </summary>
        /// <param name="CurrentDimensions"></param>
        /// <param name="maxWidth"></param>
        /// <param name="maxHeight"></param>
        /// <returns></returns>
        private static Size ResizeKeepAspect(Size CurrentDimensions, int maxWidth, int maxHeight)
        {
            int newHeight = CurrentDimensions.Height;
            int newWidth = CurrentDimensions.Width;
            if (maxWidth > 0 && newWidth > maxWidth) //WidthResize
            {
                Decimal divider = Math.Abs((Decimal)newWidth / (Decimal)maxWidth);
                newWidth = maxWidth;
                newHeight = (int)Math.Round((Decimal)(newHeight / divider));
            }
            if (maxHeight > 0 && newHeight > maxHeight) //HeightResize
            {
                Decimal divider = Math.Abs((Decimal)newHeight / (Decimal)maxHeight);
                newHeight = maxHeight;
                newWidth = (int)Math.Round((Decimal)(newWidth / divider));
            }
            return new Size(newWidth, newHeight);
        }

        /// <summary>
        /// Return true if any pixel has transparent alpha (!= 255)
        /// </summary>
        /// <param name="image"></param>
        /// <returns></returns>
        private static bool IsAlphaBitmap(Bitmap image)
        {
#if CAN_MARSHALL
// Define CAN_MARSHALL in build propety page and enable unsafe build.

            Rectangle rect = new Rectangle(Point.Empty, image.Size);
            BitmapData bmpData = image.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            byte[] Bytes = new byte[bmpData.Height * bmpData.Stride];
            Marshal.Copy(bmpData.Scan0, Bytes, 0, Bytes.Length);
            for (int p = 3; p < Bytes.Length; p += 4)
            {
                if (Bytes[p] != 255) 
                    return true;
            }
#else
            // Slower - safe - scan for apha pixel.
            for (int i = 0; i < image.Width; i++)
            {
                for (int j = 0; j < image.Height; j++)
                {
                    var pixel = image.GetPixel(i, j);
                    if (pixel.A != 255)
                    {
                        return true;
                    }
                }
            }
#endif
            return false;
        }

        /// <summary>
        /// Resize image and pick optimum file type (jpg, png, gif, ...)
        /// </summary>
        /// <param name="inImg"></param>
        /// <param name="maxWidth"></param>
        /// <param name="maxHeight"></param>
        /// <param name="optImageFmt"></param>
        /// <returns></returns>
        private static Image ResizeAndOptimizeImage(Image inImg, int maxWidth, int maxHeight, ref ImageFormat optImageFmt)
        {
            ImageFormat inFmt = inImg.RawFormat;

            // bool hasAlpha = Image.IsAlphaPixelFormat(inImg.PixelFormat);
            var flags = (ImageFlags)inImg.Flags;
            // bool hasAlpha = flags.HasFlag(ImageFlags.HasAlpha) && IsAlphaBitmap(new Bitmap(inImg));
            bool hasAlpha = HasFlag(flags, ImageFlags.HasAlpha) && IsAlphaBitmap(new Bitmap(inImg));

            if (maxWidth + maxHeight != 0)
            {
                // Resize image - maintaining aspect ratio.
                Size newSize = ResizeKeepAspect(inImg.Size, maxWidth, maxHeight);
                if (newSize.Width * newSize.Height < inImg.Width * inImg.Height)
                {
                    var newImage = new Bitmap(inImg, newSize);
                    var graphic = Graphics.FromImage(newImage);
                    int xPos = 0;
                    int yPos = 0;
                    graphic.Clear(Color.Transparent);
                    graphic.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    graphic.SmoothingMode = SmoothingMode.HighQuality;
                    graphic.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    graphic.CompositingQuality = CompositingQuality.HighQuality;
                    graphic.DrawImage(inImg, xPos, yPos, newSize.Width, newSize.Height);
                    graphic.Dispose();
                    
#if false
                    // Hack to set image format.
                    using (MemoryStream ms = new MemoryStream())
                    {
                        // ########## Crashes
                        // Crashes if using input format and it is jpeg
                        // For now - force type to png.
                        ImageFormat imgFmt = ImageFormat.Png;   
                        newImage.Save(ms, imgFmt);
                        ms.Position = 0;
                        inImg = Bitmap.FromStream(ms);
                    }
#else
                    inImg = newImage;
#endif
                }     
            }

            if (optImageFmt != null)
            {
                optImageFmt = inFmt;
                int b64Length = ImageToBase64(inImg, optImageFmt).Length;

#if false
                if (!ImageFormat.Bmp.Equals(inImg.RawFormat))
                {
                    int newB64Len = ImageToBase64(inImg, ImageFormat.Bmp).Length;
                    if (newB64Len < b64Length)
                    {
                        b64Length = newB64Len;
                        optImageFmt = ImageFormat.Bmp;
                    }
                }
               
#endif
                if (!ImageFormat.Jpeg.Equals(inImg.RawFormat) && !hasAlpha)
                {
                    // EncoderParameters encoderParameters = new EncoderParameters(1);
                    // encoderParameters.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 0L);
                    // ImageCodecInfo  jpgCodec = 
                    //     Array.Find( ImageCodecInfo.GetImageEncoders(), p => p.FormatID == ImageFormat.Jpeg.Guid);
                    int newB64Len = ImageToBase64(inImg, ImageFormat.Jpeg).Length;
                    if (newB64Len < b64Length)
                    {
                        b64Length = newB64Len;
                        optImageFmt = ImageFormat.Jpeg;
                    }
                }
                if (!ImageFormat.Png.Equals(inImg.RawFormat))
                {
                    int newB64Len = ImageToBase64(inImg, ImageFormat.Png).Length;
                    if (newB64Len < b64Length)
                    {
                        b64Length = newB64Len;
                        optImageFmt = ImageFormat.Png;
                    }
                }
                if (!ImageFormat.Gif.Equals(inImg.RawFormat) && !hasAlpha)
                {
                    int newB64Len = ImageToBase64(inImg, ImageFormat.Gif).Length;
                    if (newB64Len < b64Length)
                    {
                        b64Length = newB64Len;
                        optImageFmt = ImageFormat.Gif;
                    }
                }
#if false
                if (!ImageFormat.Tiff.Equals(inImg.RawFormat))
                {
                    int newB64Len = ImageToBase64(inImg, ImageFormat.Tiff).Length;
                      if (newB64Len < b64Length)
                      {
                          b64Length = newB64Len;
                          optImageFmt = ImageFormat.Tiff;
                      }
                }
#endif
            }

            return inImg;
        }

        /// <summary>
        /// Return build (compile) date
        ///    * First looks for BuildDate.txt file set as pre-build event
        ///    * Else computes time from assembly version "*"
        /// </summary>
        private static DateTime GetBuildDateTime()
        {
            // DateTime now = DateTime.Now;
            try
            {
                Assembly a = Assembly.GetExecutingAssembly();
                StreamReader txtReader = new StreamReader(a.GetManifestResourceStream("InlineHtmlImages_NS.BuildDate.txt"));
                string txt = txtReader.ReadLine();
                // Parse windows date string: Mon 10/27/2014
                DateTime buildDT = DateTime.Parse(txt);
                return buildDT;
            }
            catch 
            {
            }

            var version = Assembly.GetEntryAssembly().GetName().Version;
            var buildDateTime = new DateTime(2000, 1, 1).Add(new TimeSpan(
            TimeSpan.TicksPerDay * version.Build + // days since 1 January 2000
            TimeSpan.TicksPerSecond * 2 * version.Revision)); // seconds since midnight, (multiply by 2 to get original)
            return buildDateTime;
        }

        /// <summary>
        /// Return Program Title banner.
        /// </summary>
        public static  string GetTitle()
        {
            DateTime buildDateTime = GetBuildDateTime();
            Assembly assembly = Assembly.GetExecutingAssembly();
            Version version = assembly.GetName().Version;
            string titleStr = string.Format("%0eInlineHtmlImages v{0}.{1}\n Build:%0f  {2:d}\n %0eAuthor:%0f Dennis Lang\n %0eWeb:%0f    landenlabs.com", 
                version.Major, version.Minor, buildDateTime);
            return titleStr;
        }

        /// <summary>
        /// Return image type (extension) from ImageFormat.
        /// </summary>
        const string sUnknownImageType = "unk";
        private static string GetImageType(ImageFormat imageFormat) 
        {
            if (ImageFormat.Bmp.Equals(imageFormat))
                return "bmp";
            else if (ImageFormat.Jpeg.Equals(imageFormat))
                return "jpg";
            else if (ImageFormat.Png.Equals(imageFormat))
                return "png";
            else if (ImageFormat.Gif.Equals(imageFormat))
                return "gif";
            else if (ImageFormat.Tiff.Equals(imageFormat))
                return "tiff";
            else if (ImageFormat.Wmf.Equals(imageFormat))
                return "wmf";

            return sUnknownImageType;
        }

        /// <summary>
        /// Return true if str matches any of the patterns in matchList
        /// </summary>
        /// <param name="str"></param>
        /// <param name="matchList"></param>
        /// <returns></returns>
        private static bool Matches(string str, List<string> matchList)
        {
            if (null != matchList)
            foreach (string matStr in matchList)
            {
                if (str.Equals(matStr))
                    return true;
                // Convert *.png to .*\.png
                string regPat = matStr.Replace(".", @"\.").Replace("?", ".").Replace("*", ".*");
                if (Regex.Match(str, matStr, RegexOptions.IgnoreCase).Success)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// EncodeImagesInHTML - method to encode (embed) images in HTML.
        /// Change img and background-image from referencing an external image
        /// to using an encoded base64 data stream.
        /// </summary>
  
        // Change:
        //      <img alt="Layout.png" src="Layout.png">
        //      background-image: url("bg512.png"); 
        // To:
        //
        //  <img alt="Layout.png" src="data:image/png;base64,
        //  iVBORw0KGgoAAAANSUhEUgAAAyoAAAItCAMAAADhZX75AAAAAXNSR0IArs4c6QAAAARnQU1BAACx
        //  ...
        //  Ay+c0HNURhcv/v/HlI9ftYoZ9wAAAABJRU5ErkJggg==">
        //
        //    background-image: url("data:image/png;base64,xxxxxxx==");
        //

        public int EncodeImagesInHTML(string inFile, string outFile, List<string> excludeList, bool addAlt, bool optizeImage)
        {

            try
            {
                ConWriter.ColorizeLine("\n\n" + ConvertHtml.GetTitle() + "\n Encode\n");

                TextReader reader = File.OpenText(inFile);
                string text = reader.ReadToEnd();

                TextWriter writer = CreateWriter(outFile);

                string dir = Path.GetDirectoryName(inFile);
                if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
                {
                    Directory.SetCurrentDirectory(dir);
                    inFile = Path.GetFileName(inFile);
                }


                Image bmp;
                String imageB64str;
                int lastPos = 0;
                int imageB64SizeTotal = 0;
                int imageCnt = 0;

                Match mImg, mAlt, mWidth, mHeight;
                string prefix = "";
                const string urlImage = "url(\"data:image/png;base64,";
                const string srcImage = "src=\"data:image/png;base64,\n";

                foreach (Match match in imagePat.Matches(text))
                {
                    mAlt = mWidth = mHeight = null;

                    int pos = match.Index;
                    string imgStr = text.Substring(pos, match.Length);
                    switch (char.ToLower(imgStr[0]))
                    {
                    case 'i':   // background-image: url(foo.png);
                        mImg = imageUrlPat.Match(imgStr);
                        prefix = urlImage;
                        break;
                    case '<':   // <img src="foo.png">
                        mImg = imageSrcPat.Match(imgStr);
                        mAlt = imageAltPat.Match(imgStr);
                        mWidth = imageWidthPat.Match(imgStr);
                        mHeight = imageHeightPat.Match(imgStr);
                        prefix = srcImage; 
                        break;
                    default:
                        mImg = null;
                        break;
                    }

                    bool dataReplaced = false;
                    if (mImg != null && mImg.Groups.Count == 2)
                    {
                        string imagePath = mImg.Groups[1].Value.Trim().Replace("'", "").Replace("\"", "");

                        // Encored already encoded images.
                        if (!imagePath.Contains("base64"))
                        {
                            if (Matches(imagePath, excludeList))
                                Console.WriteLine("Image excluded:" + imagePath);
                            else if (!File.Exists(imagePath))
                                Console.WriteLine("Image missing:" + imagePath);
                            else
                            {
                                try
                                {
                                    string imageAlt = GetMatch(mAlt, 1);
                                    string imageWidth = GetMatch(mWidth, 1);
                                    string imageHeight = GetMatch(mHeight, 1);
                                    int useWidth = 0, useHeight = 0;
                                    int.TryParse(imageWidth, out useWidth);
                                    int.TryParse(imageHeight, out useHeight);

                                    bmp =  Bitmap.FromFile(imagePath, true);
                                    ImageFormat optImgFmt = bmp.RawFormat;
                                    bmp = ResizeAndOptimizeImage(bmp, useWidth, useHeight, ref optImgFmt);
                                    ImageFormat imgFmt = optizeImage ? optImgFmt : bmp.RawFormat;
                                    imageB64str = ImageToBase64(bmp, imgFmt);
                                    string fmtType = GetImageType(imgFmt);
                                    Console.WriteLine(string.Format("  {0}  {1,9:N0} {2} {3}", GetImageDimStr(bmp), imageB64str.Length, fmtType, imagePath));

                                    imageB64SizeTotal += imageB64str.Length;
                                    imageCnt++;

                                    writer.Write(text.Substring(lastPos, pos - lastPos));
                                    writer.Write(imgStr.Substring(0, mImg.Index));
                                    writer.Write(prefix);
                                    writer.Write(imageB64str);
                                    if (addAlt && prefix == srcImage && string.IsNullOrEmpty(imageAlt))
                                        writer.Write(string.Format("\" alt='{0}' ", Path.GetFileName(imagePath)));
                                    else
                                        writer.Write("\" ");

                                    lastPos = pos + mImg.Index + mImg.Length;
                                    // lastPos = pos + imgStr.Length;

                                    string s1 = mImg.Value;
                                    int n1 = mImg.Length;
                                    dataReplaced = true;
                                }
                                catch (Exception ex1)
                                {
                                    ConWriter.ColorizeLine("%0c" + ex1.Message + "%0f");
                                }
                            }
                        }
                    }

                    if (!dataReplaced)
                    {
                        writer.Write(text.Substring(lastPos, pos - lastPos));
                        lastPos = pos;
                    }
                }

                writer.Write(text.Substring(lastPos, text.Length - lastPos));
                writer.Close();
                ConWriter.ColorizeLine(string.Format("%0eEncoded #Images:{0:N0}   #Base64Bytes:{1:N0} %0f", imageCnt, imageB64SizeTotal));
            }
            catch (Exception ex)
            {
                // Console.WriteLine(ex.Message);
                ConWriter.ColorizeLine("%0c" + ex.Message + "%0f");
            }
           
            return 0;
        }


        /// <summary>
        /// ExtractImagesInHTML - method to decode (extract) encoded images from HTML.
        /// Change img and background-image base64 data streams back to external
        /// image references. 
        /// </summary>
        
        // Change:
        //  <img alt="Layout.png" src="data:image/png;base64,
        //  iVBORw0KGgoAAAANSUhEUgAAAyoAAAItCAMAAADhZX75AAAAAXNSR0IArs4c6QAAAARnQU1BAACx
        //  ...
        //  Ay+c0HNURhcv/v/HlI9ftYoZ9wAAAABJRU5ErkJggg==">
        //
        //  or
        //    background-image: url("data:image/png;base64,xxxxxxx==");
        //
        // To:
        //      <img alt="Layout.png" src="Layout.png">
        //  or
        //      background-image: url("bg512.png"); 
     
        public int ExtractImagesInHTML(string inFile, string outFile)
        {
            string imageBase = "cnvimage{0}.png";
            int imageNum = 100;

            try
            {
                ConWriter.ColorizeLine("\n\n" + ConvertHtml.GetTitle() + "\n Extract\n");

                TextReader reader = File.OpenText(inFile);
                string text = reader.ReadToEnd();

                TextWriter writer = CreateWriter(outFile);

                string dir = Path.GetDirectoryName(inFile);
                if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
                {
                    Directory.SetCurrentDirectory(dir);
                    inFile = Path.GetFileName(inFile);
                }

                // Image bmp;
                int lastPos = 0;
                int imageB64SizeTotal = 0;
                int imageCnt = 0;
                Match mImg, mAlt, mWidth, mHeight;
                string prefix = "";
                char separator1 = ',';
                char separator2 =  char.MinValue;

                foreach (Match match in imagePat.Matches(text))
                {
                    mAlt = mWidth = mHeight = null;
                    int pos = match.Index;
                    string imgStr = text.Substring(pos, match.Length);
                    switch (char.ToLower(imgStr[0]))
                    {
                    case 'i':   // background-image: url(foo.png);
                        mImg = imageUrlPat.Match(imgStr);
                        prefix = "url(\"";
                        separator2 = char.MinValue;
                        break;
                    case '<':
                        mImg = imageSrcPat.Match(imgStr);
                        mAlt = imageAltPat.Match(imgStr);
                        mWidth = imageWidthPat.Match(imgStr);
                        mHeight = imageHeightPat.Match(imgStr);
                        prefix = "src=\"";
                        separator2 = '\n';
                        break;
                    default:
                        mImg = null;
                        break;
                    }

                    bool dataReplaced = false;
                    if (mImg != null && mImg.Groups.Count == 2)
                    {
                        string imageData = mImg.Groups[1].Value.Trim().Replace("'", "").Replace("\"", "");

                        // Ignore non-encoded images
                        if (imageData.StartsWith("data"))
                        {
                            string imageName = string.Format(imageBase, imageNum++);
                            int sepIdx = imageData.IndexOf(separator1);
                            if (separator2 != char.MinValue && imageData[sepIdx + 1] == separator2)
                                sepIdx++;

                            string imageBase64 = imageData.Substring(sepIdx + 1);
                            try
                            {
                                // bmp = Bitmap.FromFile(imagePath);
                                Image image = Base64ToImage(imageBase64);
                                ThrowIfExists(imageName);
                                image.Save(imageName, System.Drawing.Imaging.ImageFormat.Png);
                                Console.WriteLine(string.Format("  {0}  {1,9:N0} {2}", GetImageDimStr(image), imageBase64.Length, imageName));

                                imageB64SizeTotal += imageBase64.Length;
                                imageCnt++;

                                writer.Write(text.Substring(lastPos, pos - lastPos));
                                writer.Write(imgStr.Substring(0, mImg.Index));
                                writer.Write(prefix);
                                writer.Write(imageName);
                                writer.Write("\" ");

                                // lastPos = pos + imgStr.Length;
                                lastPos = pos + mImg.Index + mImg.Length;
                                dataReplaced = true;
                            }
                            catch (Exception ex1)
                            {
                                ConWriter.ColorizeLine("%0c" + ex1.Message + "%0f");
                            }
                        }
                    }

                    if (!dataReplaced)
                    {
                        writer.Write(text.Substring(lastPos, pos - lastPos));
                        lastPos = pos;
                    }
                }

                writer.Write(text.Substring(lastPos, text.Length - lastPos));
                writer.Close();
                ConWriter.ColorizeLine(string.Format("%0eExtracted #Images:{0:N0}   #Base64Bytes:{1:N0} %0f", imageCnt, imageB64SizeTotal));
            }
            catch (Exception ex)
            {
                // Console.WriteLine(ex.Message);
                ConWriter.ColorizeLine("%0c" + ex.Message + "%0f");
            }

            return 0;
        }

        /// <summary>
        /// Report summary of images (encoded or regular) in html file.
        /// </summary>
        /// <param name="inFile">Input html to examine for images. </param>
        /// <returns>0</returns>
        public int SummaryOfImagesInHTML(string inFile)
        {
            try
            {
                ConWriter.ColorizeLine("\n\n" + ConvertHtml.GetTitle() + "\n Summary\n");

                TextReader reader = File.OpenText(inFile);
                string text = reader.ReadToEnd();

                string dir = Path.GetDirectoryName(inFile);
                if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
                {
                    Directory.SetCurrentDirectory(dir);
                    inFile = Path.GetFileName(inFile);
                }

                Console.WriteLine(inFile);

                Image bmp;
                String imageB64str;
                int imageB64SizeTotal = 0;
                int encodedImageCnt = 0;
                int rawImageCnt = 0;
                Match mImg, mAlt, mHeight, mWidth;
                char separator1 = ',';
                char separator2 =  char.MinValue;


                Dictionary<ImageInfo, List<ImageUse>> imageCntDic = new Dictionary<ImageInfo, List<ImageUse>>();

                foreach (Match match in imagePat.Matches(text))
                {
                    mAlt = mHeight = mWidth = null;
                    int pos = match.Index;
                    string imgStr = text.Substring(pos, match.Length);
                    switch (char.ToLower(imgStr[0]))
                    {
                    case 'i':   // background-image: url(foo.png);
                        mImg = imageUrlPat.Match(imgStr);
                        separator2 = char.MinValue;
                        break;
                    case '<':
                        mImg = imageSrcPat.Match(imgStr);
                        mAlt = imageAltPat.Match(imgStr);
                        mWidth = imageWidthPat.Match(imgStr);
                        mHeight = imageHeightPat.Match(imgStr);
                        separator2 = '\n';
                        break;
                    default:
                        mImg = null;
                        break;
                    }

                    if (mImg != null && mImg.Groups.Count == 2)
                    {
                        string imageData = mImg.Groups[1].Value.Trim().Replace("'", "").Replace("\"", "");

                        if (imageData.StartsWith("data"))
                        {
                            int sepIdx = imageData.IndexOf(separator1);
                            if (separator2 != char.MinValue && imageData[sepIdx + 1] == separator2)
                                sepIdx++;

                            string imageBase64 = imageData.Substring(sepIdx + 1);
                            try
                            {
                                Image image = Base64ToImage(imageBase64);
                                string pixType = image.PixelFormat.ToString();

                                Console.WriteLine(string.Format("  {0,3:N0}# Encoded {1}  Length(Base64):{2,9:N0} {3,20} {4}",
                                    ++encodedImageCnt, GetImageDimStr(image), imageBase64.Length, pixType, imageData.Substring(0, sepIdx)));

                                imageB64SizeTotal += imageBase64.Length;
                            }
                            catch (Exception ex1)
                            {
                                ConWriter.ColorizeLine("%0c" + ex1.Message + "%0f");
                            }
                        }
                        else
                        {
                            string imageAlt = GetMatch(mAlt, 1);
                            string imageWidth = GetMatch(mWidth, 1);
                            string imageHeight = GetMatch(mHeight, 1);

                            try
                            {
                                bmp = Bitmap.FromFile(imageData);
                                string pixType = bmp.PixelFormat.ToString();
                                string fmtType = GetImageType(bmp.RawFormat);

                                imageB64str = ImageToBase64(bmp, bmp.RawFormat);
#if false
                                Console.WriteLine(string.Format("  {0,3:N0}#   Image {1}  Length(Base64):{2,9:N0} {3,20} {4} {5}",
                                     rawImageCnt, GetImageDimStr(bmp), imageB64str.Length, pixType, fmtType, imageData));
#else
                                ImageInfo imageInfo = new ImageInfo()
                                {
                                    name = imageData,  fType = fmtType, pType = pixType,
                                    width = bmp.Width,  height = bmp.Height, depth = GetImageDepth(bmp),
                                    byteCnt = imageB64str.Length
                                };
                                ImageUse imageUse = new ImageUse() { width = 0, height = 0 };
                                int.TryParse(imageWidth, out imageUse.width);
                                int.TryParse(imageHeight, out imageUse.height);
                                
                                if (!imageCntDic.ContainsKey(imageInfo))
                                    imageCntDic[imageInfo] = new List<ImageUse>();
                                imageCntDic[imageInfo].Add(imageUse);
#endif
                                rawImageCnt++;
                                imageB64SizeTotal += imageB64str.Length;
                            }
                            catch (Exception ex1)
                            {
                                ConWriter.ColorizeLine("%0c" + ex1.Message + "%0f");
                            }
                        }
                    }
                }

#if false
#else
                if (encodedImageCnt != 0)
                    Console.WriteLine("");
                int imageCnt = 0;
                foreach (ImageInfo imageInfo in imageCntDic.Keys)
                {
                    Console.WriteLine(string.Format("  {0,3:N0}#   Image {1}  Length(Base64):{2,9:N0} {3,20} {4} {5}",
                                   ++imageCnt, GetImageDimStr(imageInfo), imageInfo.byteCnt, imageInfo.pType, imageInfo.fType, imageInfo.name));
                    foreach (ImageUse imageUse in imageCntDic[imageInfo])
                    {
                        if (imageUse.width != 0 && imageUse.height != 0)
                            Console.WriteLine(string.Format("       Use Width:{0:N0}  Height:{1:N0}", imageUse.width, imageUse.height));
                        else if (imageUse.width != 0)
                            Console.WriteLine(string.Format("       Use Width:{0:N0}  Height:na", imageUse.width));
                        else if (imageUse.height != 0)
                            Console.WriteLine(string.Format("       Use Width:na   Height:{0:N0}", imageUse.height));
                    }
                }
#endif
                ConWriter.ColorizeLine(string.Format("%0eSummary Images #Native:{0:N0}   #Encoded:{1:N0}   #Base64Bytes:{2:N0} %0f", 
                    rawImageCnt, encodedImageCnt, imageB64SizeTotal));
            }
            catch (Exception ex)
            {
                // Console.WriteLine("infile " + ex.Message);
                ConWriter.ColorizeLine("infile %0c" + ex.Message + "%0f");
            }

            return 0;
        }

    }
}
