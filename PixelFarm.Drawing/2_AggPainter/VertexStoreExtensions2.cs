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



using PixelFarm.CpuBlit;
using PixelFarm.CpuBlit.VertexProcessing;

namespace PixelFarm.Drawing
{
    public static class VertexStoreExtensions2
    {

        public static VertexStore ReverseClockDirection(this VertexStore src, VertexStore outputVxs)
        {

            //temp fix
            int closeCmdAt = -1;
            for (int i = src.Count - 1; i >= 0; --i)
            {
                var cmd = src.GetVertex(i, out double x, out double y);
                switch (cmd)
                {
                    default: throw new System.Exception();
                    case VertexCmd.NoMore:
                        break;
                    case VertexCmd.Close:
                        closeCmdAt = outputVxs.Count;
                        outputVxs.AddMoveTo(x, y);
                        break;
                    case VertexCmd.MoveTo:
                        {
                            if (closeCmdAt >= 0)
                            {
                                //change closeCmdAt
                                outputVxs.ReplaceVertex(closeCmdAt, x, y);
                                closeCmdAt = -1;//reset
                            }
                        }
                        outputVxs.AddCloseFigure();
                        break;
                    case VertexCmd.LineTo:
                    case VertexCmd.C3:
                    case VertexCmd.C4:
                        outputVxs.AddVertex(x, y, cmd);
                        break;
                }
            }
            return outputVxs;
        }

        /// <summary>
        /// copy + translate vertext data from src to outputVxs
        /// </summary>
        /// <param name="src"></param>
        /// <param name="dx"></param>
        /// <param name="dy"></param>
        /// <param name="outputVxs"></param>
        /// <returns></returns>
        public static VertexStore TranslateToNewVxs(this VertexStore src, double dx, double dy, VertexStore outputVxs)
        {
            int count = src.Count;

            for (int i = 0; i < count; ++i)
            {
                VertexCmd cmd = src.GetVertex(i, out double x, out double y);
                x += dx;
                y += dy;
                outputVxs.AddVertex(x, y, cmd);
            }
            return outputVxs;
        }
        public static VertexStore ScaleToNewVxs(this VertexStore src, double s, VertexStore outputVxs)
        {

            return AffineMat.GetScaleMat(s, s).TransformToVxs(src, outputVxs);

            //TODO: review here
            //use struct
            //Affine aff = Affine.NewScaling(s, s);
            //return aff.TransformToVxs(src, outputVxs);
        }
        public static VertexStore ScaleToNewVxs(this VertexStore src, double sx, double sy, VertexStore outputVxs)
        {
            return AffineMat.GetScaleMat(sx, sy).TransformToVxs(src, outputVxs);
            ////TODO: review here, use struct mat
            //Affine aff = Affine.NewScaling(sx, sy);
            //return aff.TransformToVxs(src, outputVxs);
        }

        public static VertexStore RotateDegToNewVxs(this VertexStore src, double deg, VertexStore outputVxs)
        {
            return AffineMat.GetRotateDegMat(deg).TransformToVxs(src, outputVxs);

            //TODO: review here, use struct mat
            //Affine aff = Affine.NewRotationDeg(deg);
            //return aff.TransformToVxs(src, outputVxs);
        }
        public static VertexStore RotateRadToNewVxs(this VertexStore src, double rad, VertexStore outputVxs)
        {
            return AffineMat.GetRotateMat(rad).TransformToVxs(src, outputVxs);
            //Affine aff = Affine.NewRotation(rad);
            //return aff.TransformToVxs(src, outputVxs);
        }
        public static VertexStore RotateRadToNewVxs(this VertexStore src, double rad, float centerX, float centerY, VertexStore outputVxs)
        {
            AffineMat aff = AffineMat.Iden();
            aff.Translate(-centerX, -centerY);
            aff.Rotate(rad);
            aff.Translate(centerX, centerY);
            aff.TransformToVxs(src, outputVxs);
            return outputVxs;
            //Affine aff = Affine.NewRotation(rad);
            //return aff.TransformToVxs(src, outputVxs);
        }
    }
}