//BSD, 2014-present, WinterDev
//----------------------------------------------------------------------------
// Anti-Grain Geometry - Version 2.4
// Copyright (C) 2002-2005 Maxim Shemanarev (http://www.antigrain.com)
//
// C# Port port by: Lars Brubaker
//                  larsbrubaker@gmail.com
// Copyright (C) 2007
//
// Permission to copy, use, modify, sell and distribute this software 
// is granted provided this copyright notice appears in all copies. 
// This software is provided "as is" without express or implied
// warranty, and with no claim as to its suitability for any purpose.
//
//----------------------------------------------------------------------------
// Contact: mcseem@antigrain.com
//          mcseemagg@yahoo.com
//          http://www.antigrain.com
//----------------------------------------------------------------------------

using System;
namespace PixelFarm.CpuBlit.PixelProcessing
{
    /// <summary>
    /// sub-image reader /writer/blend part of org bitmap
    /// </summary>
    public sealed class SubBitmapBlender : BitmapBlenderBase
    {
        IBitmapSrc _sourceImage;

        public SubBitmapBlender(IBitmapBlender image,
            int arrayOffset32,
            int width,
            int height)
        {
            this.OutputPixelBlender = image.OutputPixelBlender;
            Span<byte> span = image.GetBufferSpan();
            AttachBuffer(image.GetRawBufferHead(), span.Length,
                arrayOffset32,
                width,
                height,
                image.Stride,
                image.BitDepth,
                image.BytesBetweenPixelsInclusive);
        }

        public SubBitmapBlender(IntPtr ptr, int lenInBytes,
            int arrayOffset32,
            int width,
            int height,
            int strideInBytes,
            int bitDepth,
            int distanceInBytesBetweenPixelsInclusive)
        {
            AttachBuffer(ptr, lenInBytes,
                arrayOffset32,
                width,
                height,
                strideInBytes, bitDepth,
                distanceInBytesBetweenPixelsInclusive);
        }
        public SubBitmapBlender(IBitmapSrc image,
            PixelBlender32 blender,
            int distanceBetweenPixelsInclusive,
            int arrayOffset32,
            int bitsPerPixel)
        {

            this.OutputPixelBlender = blender;
            Attach(image, blender, distanceBetweenPixelsInclusive, arrayOffset32, bitsPerPixel);
        }
        public SubBitmapBlender(IBitmapSrc image, PixelBlender32 blender)
        {
            Attach(image, blender, image.BytesBetweenPixelsInclusive, 0, image.BitDepth);
        }
        public override void WriteBuffer(ReadOnlySpan<int> newbuffer)
        {
            _sourceImage?.WriteBuffer(newbuffer);
        }
        void AttachBuffer(IntPtr buffer, int bufferLenInBytes,
          int elemOffset,
          int width,
          int height,
          int strideInBytes,
          int bitDepth,
          int distanceInBytesBetweenPixelsInclusive)
        {
            SetBufferToNull();
            SetDimmensionAndFormat(width, height, strideInBytes, bitDepth,
                distanceInBytesBetweenPixelsInclusive);
            SetBuffer(buffer, bufferLenInBytes);
            SetUpLookupTables();

        }

        void Attach(IBitmapSrc sourceImage,
          PixelBlender32 outputPxBlender,
          int distanceBetweenPixelsInclusive,
          int arrayElemOffset,
          int bitsPerPixel)
        {
            _sourceImage = sourceImage;
            SetDimmensionAndFormat(sourceImage.Width,
                sourceImage.Height,
                sourceImage.Stride,
                bitsPerPixel,
                distanceBetweenPixelsInclusive);

            int srcOffset32 = sourceImage.GetBufferOffsetXY32(0, 0);
            SetBuffer(sourceImage.GetRawBufferHead(), sourceImage.BufferLengthInBytes);
            SetUpLookupTables();
            this.OutputPixelBlender = outputPxBlender;
        }
    }

    public static class BitmapBlenderExtension
    {
        /// <summary>
        /// This will create a new ImageBuffer that references the same memory as the image that you took the sub image from.
        /// It will modify the original main image when you draw to it.
        /// </summary>
        /// <param name="parentImage"></param>
        /// <param name="subImgBounds"></param>
        /// <returns></returns>
        public static SubBitmapBlender CreateSubBitmapBlender(IBitmapBlender parentImage, PixelFarm.CpuBlit.VertexProcessing.Q1Rect subImgBounds)
        {
            if (subImgBounds.Left < 0 || subImgBounds.Bottom < 0 || subImgBounds.Right > parentImage.Width || subImgBounds.Top > parentImage.Height
                || subImgBounds.Left >= subImgBounds.Right || subImgBounds.Bottom >= subImgBounds.Top)
            {
                throw new ArgumentException("The subImageBounds must be on the image and valid.");
            }

            int left = Math.Max(0, subImgBounds.Left);
            int bottom = Math.Max(0, subImgBounds.Bottom);
            int width = Math.Min(parentImage.Width - left, subImgBounds.Width);
            int height = Math.Min(parentImage.Height - bottom, subImgBounds.Height);
            return new SubBitmapBlender(parentImage, parentImage.GetBufferOffsetXY32(left, bottom), width, height);
        }
    }

}