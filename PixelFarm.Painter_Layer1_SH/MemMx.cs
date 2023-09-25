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
//#define USE_UNSAFE // no real code for this yet

using System;


namespace PixelFarm.Drawing.Internal
{
    public static class MemMx
    { 
        public static unsafe void memcpy(byte* dest, byte* src, int len)
        {
            new Span<byte>(src, len).CopyTo(new Span<byte>(dest, len));             
        }

        public static unsafe void memmove(byte* dest, int destIndex, byte* source, int sourceIndex, int count)
        {
            if (source != dest
                || destIndex < sourceIndex)
            {                  
                new Span<byte>(source+ sourceIndex, count).CopyTo(new Span<byte>(dest + destIndex, count));
            }
            else
            {
                throw new Exception("this code needs to be tested");
            }
        }


    }
}