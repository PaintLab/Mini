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
//
// Adaptation for high precision colors has been sponsored by 
// Liberty Technology Systems, Inc., visit http://lib-sys.com
//
// Liberty Technology Systems, Inc. is the provider of
// PostScript and PDF technology for software developers.
// 
//----------------------------------------------------------------------------
#define USE_BLENDER

using PixelFarm.Drawing; 
namespace PixelFarm.CpuBlit.PixelProcessing
{
    public abstract class PixelBlender32
    {

        public const int NUM_PIXEL_BITS = 32;
        internal const byte BASE_MASK = 255;


        /// <summary>
        /// blend single pixel
        /// </summary>
        /// <param name="dstBuffer"></param>
        /// <param name="arrayOffset"></param>
        /// <param name="srcColor"></param>
        internal abstract unsafe void BlendPixel(int* dstBuffer, int arrayOffset, Color srcColor);


        /// <summary>
        /// blend multiple pixels
        /// </summary>
        /// <param name="dstBuffer"></param>
        /// <param name="arrayElemOffset"></param>
        /// <param name="sourceColors"></param>
        /// <param name="sourceColorsOffset"></param>
        /// <param name="covers"></param>
        /// <param name="coversIndex"></param>
        /// <param name="firstCoverForAll"></param>
        /// <param name="count"></param>
        internal unsafe abstract void BlendPixels(
           int* dstBuffer, int arrayElemOffset,
           Color[] sourceColors, int sourceColorsOffset,
           byte[] covers, int coversIndex, bool firstCoverForAll, int count);



        /// <summary>
        /// copy multiple pixels
        /// </summary>
        /// <param name="dstBuffer"></param>
        /// <param name="arrayOffset"></param>
        /// <param name="srcColor"></param>
        /// <param name="count"></param>
        internal unsafe abstract void CopyPixels(int* dstBuffer, int arrayOffset, Color srcColor, int count);

        /// <summary>
        /// copy single pixel
        /// </summary>
        /// <param name="dstBuffer"></param>
        /// <param name="arrayOffset"></param>
        /// <param name="srcColor"></param>
        internal abstract unsafe void CopyPixel(int* dstBuffer, int arrayOffset, Color srcColor);
        internal abstract unsafe void BlendPixel32(int* ptr, Color sc);
        //----------------

       

    }

}

