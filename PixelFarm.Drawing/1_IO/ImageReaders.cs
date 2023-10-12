//MIT, 2017-present, WinterDev
using System;
using System.IO;
using PixelFarm.Platforms;
namespace PixelFarm.CpuBlit.Imaging
{

    public static class PngImageReader
    {

    }
    public static class PngImageWriter
    {
        public static void SaveImgBufferToPngFile(MemBitmap mem, int stride, int width, int height, string filename)
        {
            if (s_saveToFile != null)
            {
                s_saveToFile(mem.GetRawBufferHead(), stride, width, height, filename);
            }
        }
        static SaveImageBufferToFileDel s_saveToFile;

        public static bool HasDefaultSaveToFileDelegate()
        {
            return s_saveToFile != null;
        }
        internal static void InstallImageSaveToFileService(SaveImageBufferToFileDel saveToFileDelegate)
        {
            s_saveToFile = saveToFileDelegate;
        }

#if DEBUG
        public static void dbugSaveToPngFile(this MemBitmap bmp, string filename)
        {

            SaveImgBufferToPngFile(bmp,
                bmp.Stride,
                bmp.Width,
                bmp.Height,
                filename);
        }
#endif
    }

    //---------------------------------
    //jpg

    public static class JpgImageReader
    {


    }
    public static class JpgImageWriter
    {
        public static void SaveImgBufferToJpgFile(
            int[] imgBuffer,
            int stride,
            int width,
            int height,
            string filename)
        {
            if (s_saveToFile != null)
            {
                unsafe
                {
                    fixed (int* head = &imgBuffer[0])
                    {
                        s_saveToFile((IntPtr)head, stride, width, height, filename);
                    }
                }
            }
        }
        public static unsafe void SaveImgBufferToJpgFileUnsafe(
           int* head,
           int stride,
           int width,
           int height,
           string filename)
        {
            if (s_saveToFile != null)
            {
                unsafe
                {
                    s_saveToFile((IntPtr)head, stride, width, height, filename);
                }
            }
        }
        static SaveImageBufferToFileDel s_saveToFile;

        public static bool HasDefaultSaveToFileDelegate()
        {
            return s_saveToFile != null;
        }
        internal static void InstallImageSaveToFileService(SaveImageBufferToFileDel saveToFileDelegate)
        {
            s_saveToFile = saveToFileDelegate;
        }


#if DEBUG
        public static void dbugSaveToJpgFile(this MemBitmap bmp, string filename)
        {
            unsafe
            {
                SaveImgBufferToJpgFileUnsafe(bmp.GetRawInt32BufferHead(),
                    bmp.Stride,
                    bmp.Width,
                    bmp.Height,
                    filename);
            }
        }
#endif
    }

}
namespace PixelFarm.Platforms
{
    public delegate void SaveImageBufferToFileDel(IntPtr imgBuffer,
      int stride, int width, int height,
      string filename);

    public class ImageHint
    {
        public string Extension { get; set; }
        public int ReqWidth { get; set; }
        public int ReqHeight { get; set; }
    }




    public class ImageIOSetupParameters
    {
        public SaveImageBufferToFileDel SaveToPng;
        public SaveImageBufferToFileDel SaveToJpg;
        public Func<MemoryStream, ImageHint, PixelFarm.Drawing.Image> ReadFromMemStream;
        public Func<object, ImageHint, PixelFarm.Drawing.Image> ReadFromNativeImage;
    }
    public static class ImageIOPortal
    {
        static Func<MemoryStream, ImageHint, PixelFarm.Drawing.Image> s_readImgDataFromMemStream;
        static Func<object, ImageHint, PixelFarm.Drawing.Image> s_readFromNativeImage;

        public static PixelFarm.Drawing.Image ReadImageDataFromMemStream(MemoryStream ms, ImageHint hint)
        {
            return s_readImgDataFromMemStream(ms, hint);
        }
        public static PixelFarm.Drawing.Image ReadImageFromNativeObject(object nativeImage, ImageHint hint)
        {
            return s_readFromNativeImage(nativeImage, hint);
        }
        public static void Setup(ImageIOSetupParameters pars)
        {
            //check 

            if (pars.SaveToPng != null)
            {
                PixelFarm.CpuBlit.Imaging.PngImageWriter.InstallImageSaveToFileService(pars.SaveToPng);
            }
            if (pars.SaveToJpg != null)
            {
                PixelFarm.CpuBlit.Imaging.PngImageWriter.InstallImageSaveToFileService(pars.SaveToJpg);
            }

            s_readImgDataFromMemStream = pars.ReadFromMemStream;
            s_readFromNativeImage = pars.ReadFromNativeImage;
        }

    }
}