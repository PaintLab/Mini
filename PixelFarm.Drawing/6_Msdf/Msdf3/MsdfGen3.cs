//MIT, 2019-present, WinterDev 
//based on MIT, 2016, Viktor Chlumsky, Multi-channel signed distance field generator, from https://github.com/Chlumsky/msdfge)
//-----------------------------------  

using PixelFarm.CpuBlit;
using PixelFarm.CpuBlit.VertexProcessing;
using PixelFarm.Drawing;
using System;
using System.Collections.Generic;

namespace Msdfgen
{
    /// <summary>
    /// msdf texture generator
    /// </summary>
    public class MsdfGen3
    {

        readonly PixelFarm.CpuBlit.Rasterization.PrebuiltGammaTable _prebuiltThresholdGamma_OverlappedBorder;
        readonly PixelFarm.CpuBlit.Rasterization.PrebuiltGammaTable _prebuiltThresholdGamma_50;
        readonly MsdfEdgePixelBlender _msdfEdgePxBlender = new MsdfEdgePixelBlender();
        readonly StrokeMath _strokeMath = new StrokeMath();

        public MsdfGen3()
        {
            //our MsdfGen3 is a modified version of the original Msdf 
            _prebuiltThresholdGamma_OverlappedBorder = PixelFarm.CpuBlit.Rasterization.PrebuiltGammaTable.CreateSameValuesGammaTable(255);
            _prebuiltThresholdGamma_50 = new PixelFarm.CpuBlit.Rasterization.PrebuiltGammaTable(new PixelFarm.CpuBlit.PixelProcessing.GammaThreshold(0.5f));//***50% confident coverage              

            _strokeMath.Width = 3; //outside 1.5, inside=1.5
            _strokeMath.LineCap = LineCap.Butt;
            _strokeMath.LineJoin = LineJoin.Miter;
            _strokeMath.InnerJoin = InnerJoin.Bevel;
        }
        public MsdfGenParams MsdfGenParams { get; set; }
#if DEBUG
        public bool dbugWriteMsdfTexture { get; set; } = true;

#endif

        const int INNER_BORDER_W = 2;
        const int OUTER_BORDER_W = 12;

        double _dx;
        double _dy;


        void FillCorner(AggPainter painter, ContourCorner c, Color color)
        {
            FillCorner(painter, c.MiddlePoint.X, c.MiddlePoint.Y, color);
        }
        void FillCorner(AggPainter painter, double cornerX, double cornerY, Color color)
        {

            //a corner build from left edge and right edeg
            //so we create a color for encode both edge
            //create box around the corner
            int cornerHalf = 4;
            int cornerFull = cornerHalf * 2;
            using (Tools.BorrowVxs(out var vxs1))
            {
                double x = cornerX - cornerHalf;
                double y = cornerY - cornerHalf;

                vxs1.AddMoveTo(x, y);
                vxs1.AddLineTo(x + cornerFull, y);
                vxs1.AddLineTo(x + cornerFull, y + cornerFull);
                vxs1.AddLineTo(x, y + cornerFull);
                vxs1.AddCloseFigure();
                painter.Fill(vxs1, color);
            }
        }
        /// <summary>
        /// fill inner or outer border from corner0 to corner1
        /// </summary>
        /// <param name="painter"></param>
        /// <param name="c0"></param>
        /// <param name="c1"></param>
        void FillBorder(AggPainter painter, ContourCorner c0, ContourCorner c1)
        {

            //counter-clockwise

            if (!c0.MiddlePoint_IsTouchPoint) { return; }

            //with a given corner, have have information of 3 points
            //left-point of the corner,=> from vertex
            //middle-point, current vertex
            //right-point,=> next vertex 
            //a vertex may be touch-curve vertext, or 'not-touch-curve' vertex

            //'not touch-curve point', => this vertex is a  control point of C3 or C4 curve,
            //-------------------------------------------------------

            if (c0.RightPoint_IsTouchPoint)
            {
                //c0 => touch curve
                //c1 => touch curve,
                //we create an imaginary line from  c0 to c1
                //then we create an 'inner border' of a line from c0 to c1
                //and we create an 'outer border' of a line from c0 to c1

                using (Tools.BorrowStroke(out var strk))
                using (Tools.BorrowVxs(out var vxs, out var vxs2))
                {
                    //outer  
                    //2020-03-13, version 3 fill is still better than v3.1, 
                    //TODO: review version v3.1 

                    //version 3 fill technique


                    vxs.AddMoveTo(c0.MiddlePoint.X, c0.MiddlePoint.Y);
                    vxs.AddLineTo(c1.MiddlePoint.X, c1.MiddlePoint.Y);
                    vxs.AddNoMore();

                    strk.Width = INNER_BORDER_W * 2;
                    strk.StrokeSideForOpenShape = StrokeSideForOpenShape.Both;
                    strk.MakeVxs(vxs, vxs2);

                    painter.Fill(vxs2, c0.Color);

                    if (c0.CenterSegment != c1.CenterSegment)
                    {
                        ////fill c0 and c1 corner with c0.OuterColor
                        FillCorner(painter, c0, c0.Color);
                        FillCorner(painter, c1, c0.Color);
                    }
                    else
                    {

                    }
                    //-------------
                    vxs.Clear(); //reuse
                }
            }
            else
            {

                //painter.CurrentBxtBlendOp = null;

                //**
                //c0 is touch line,
                //but c1 is not, this means=> next segment will be a curve(C3 or C4 curve)
                //
                EdgeSegment ownerSeg = c1.CenterSegment;

                switch (ownerSeg.SegmentKind)
                {
                    default: throw new NotSupportedException();
                    case EdgeSegmentKind.CubicSegment:
                        {
                            //approximate 
                            CubicSegment seg = (CubicSegment)ownerSeg;
                            using (Tools.BorrowVxs(out var v1))
                            using (Tools.BorrowShapeBuilder(out var b))
                            using (Tools.BorrowStroke(out var strk))
                            {

                                b.MoveTo(seg.P0.x + _dx, seg.P0.y + _dy) //...
                                .Curve4To(seg.P1.x + _dx, seg.P1.y + _dy,
                                          seg.P2.x + _dx, seg.P2.y + _dy,
                                          seg.P3.x + _dx, seg.P3.y + _dy)
                                .NoMore()
                                .Flatten();

                                strk.Width = INNER_BORDER_W * 2;
                                strk.StrokeSideForOpenShape = StrokeSideForOpenShape.Both;
                                strk.MakeVxs(b.CurrentSharedVxs, v1);
                                painter.Fill(v1, c0.Color);

                                FillCorner(painter, seg.P0.x + _dx, seg.P0.y + _dy, c0.Color);
                                FillCorner(painter, seg.P3.x + _dx, seg.P3.y + _dy, c0.Color);
                            }
                        }
                        break;
                    case EdgeSegmentKind.QuadraticSegment:
                        {
                            QuadraticSegment seg = (QuadraticSegment)ownerSeg;

                            using (Tools.BorrowVxs(out var v1, out var v2))
                            using (Tools.BorrowShapeBuilder(out var b))
                            using (Tools.BorrowStroke(out var strk))
                            {

                                b.MoveTo(seg.P0.x + _dx, seg.P0.y + _dy)//...
                                .Curve3To(seg.P1.x + _dx, seg.P1.y + _dy,
                                          seg.P2.x + _dx, seg.P2.y + _dy)
                                .NoMore()
                                .Flatten();

                                //-----------------------
                                //fill outside part of the curve

                                strk.Width = INNER_BORDER_W * 2;
                                strk.StrokeSideForOpenShape = StrokeSideForOpenShape.Both;
                                strk.MakeVxs(b.CurrentSharedVxs, v1);
                                painter.Fill(v1, c0.Color);

                                FillCorner(painter, seg.P0.x + _dx, seg.P0.y + _dy, c0.Color);
                                FillCorner(painter, seg.P2.x + _dx, seg.P2.y + _dy, c0.Color);
                            }
                        }
                        break;
                }
            }
        }


        const double MAX = 1e240;

        internal static void PreviewSizeAndLocation(Shape shape, MsdfGenParams genParams,
             out int imgW, out int imgH,
             out Vector2 translate1)
        {
            double left = MAX;
            double bottom = MAX;
            double right = -MAX;
            double top = -MAX;

            shape.findBounds(ref left, ref bottom, ref right, ref top);
            int w = (int)Math.Ceiling((right - left));
            int h = (int)Math.Ceiling((top - bottom));

            if (w < genParams.minImgWidth)
            {
                w = genParams.minImgWidth;
            }
            if (h < genParams.minImgHeight)
            {
                h = genParams.minImgHeight;
            }

            //temp, for debug with glyph 'I', tahoma font
            //double edgeThreshold = 1.00000001;//default, if edgeThreshold < 0 then  set  edgeThreshold=1 
            //Msdfgen.Vector2 scale = new Msdfgen.Vector2(0.98714652956298199, 0.98714652956298199);
            //double pxRange = 4;
            //translate = new Msdfgen.Vector2(12.552083333333332, 4.0520833333333330);
            //double range = pxRange / Math.Min(scale.x, scale.y);


            int borderW = (int)((float)w / 5f);

            //org
            //var translate = new ExtMsdfgen.Vector2(left < 0 ? -left + borderW : borderW, bottom < 0 ? -bottom + borderW : borderW);
            //test


            w += borderW * 2; //borders,left- right
            h += borderW * 2; //borders, top- bottom

            imgW = w;
            imgH = h;
            translate1 = new Vector2(-left + borderW, -bottom + borderW);
        }
        //        public PixelFarm.CpuBlit.BitmapAtlas.BitmapAtlasItemSource GenerateMsdfTexture_Old(VertexStore vxs)
        //        {

        //            Shape shape = CreateShape(vxs, out EdgeBmpLut edgeBmpLut);

        //            if (MsdfGenParams == null)
        //            {
        //                MsdfGenParams = new MsdfGenParams();//use default
        //            }

        //            //---preview v1 bounds-----------
        //            PreviewSizeAndLocation(
        //               shape,
        //               MsdfGenParams,
        //               out int imgW, out int imgH,
        //               out Vector2 translateVec);

        //            _dx = translateVec.x;
        //            _dy = translateVec.y;
        //            //------------------------------------
        //            List<ContourCorner> corners = edgeBmpLut.Corners;
        //            TranslateCorners(corners, _dx, _dy);

        //            //[1] create lookup table (lut) bitmap that contains area/corner/shape information
        //            //each pixel inside it contains data that map to area/corner/shape

        //            //
        //            using (MemBitmap bmpLut = new MemBitmap(imgW, imgH))
        //            using (Tools.BorrowAggPainter(bmpLut, out var painter))
        //            using (Tools.BorrowShapeBuilder(out var sh))
        //            {

        //                _msdfEdgePxBlender.ClearOverlapList();//reset
        //                painter.SetCustomPixelBlender(_msdfEdgePxBlender);

        //                //1. clear all bg to black 
        //                painter.Clear(PixelFarm.Drawing.Color.Black);

        //                sh.InitVxs(vxs) //...
        //                    .TranslateToNewVxs(_dx, _dy)
        //                    .Flatten();


        //                //---------
        //                //2. force fill the shape (this include hole(s) inside shape to)
        //                //( we set threshold to 50 and do force fill)
        //                painter.SetGamma(_prebuiltThresholdGamma_50);
        //                _msdfEdgePxBlender.FillMode = MsdfEdgePixelBlender.BlenderFillMode.Force;
        //                painter.Fill(sh.CurrentSharedVxs, EdgeBmpLut.EncodeToColor(0, AreaKind.AreaInsideCoverage50));
        //#if DEBUG
        //                //debug for output
        //                //painter.Fill(v7, Color.Red);
        //                bmpLut.SaveImage("dbug_step0.png");
        //                //int curr_step = 1;
        //#endif


        //                //---------

        //                int cornerCount = corners.Count;
        //                List<int> cornerOfNextContours = edgeBmpLut.CornerOfNextContours;


        //                //fill inside and outside
        //                int startAt = 0;
        //                int n = 1;
        //                int corner_index = 1;
        //                _msdfEdgePxBlender.FillMode = MsdfEdgePixelBlender.BlenderFillMode.OuterBorder;
        //                for (int contour_index = 0; contour_index < cornerOfNextContours.Count; ++contour_index)
        //                {
        //                    //contour scope
        //                    int next_corner_startAt = cornerOfNextContours[contour_index];

        //                    //-----------
        //                    //AA-borders of the contour
        //                    painter.SetGamma(_prebuiltThresholdGamma_OverlappedBorder); //this creates overlapped area 

        //                    for (; n < next_corner_startAt; ++n)
        //                    {
        //                        //0-> 1
        //                        //1->2 ... n
        //                        FillBorder(painter, corners[n - 1], corners[n]);

        //#if DEBUG
        //                        //bmpLut.SaveImage("dbug_step" + curr_step + ".png");
        //                        //curr_step++;
        //#endif
        //                    }
        //                    {
        //                        //the last one 
        //                        //close contour, n-> 0
        //                        FillBorder(painter, corners[next_corner_startAt - 1], corners[startAt]);
        //#if DEBUG
        //                        //bmpLut.SaveImage("dbug_step" + curr_step + ".png");
        //                        //curr_step++;
        //#endif
        //                    }

        //                    startAt = next_corner_startAt;
        //                    n++;
        //                    corner_index++;
        //                }





        //                //
        //                //unsafe
        //                //{
        //                //    fixed (int* ptr = bmpLut.GetInt32BufferSpan())
        //                //    {
        //                //        int* ptr1 = ptr;
        //                //        int len = bmpLut.Width * bmpLut.Height;
        //                //        for (int i = 0; i < len; ++i)
        //                //        {
        //                //            int value = *ptr1;
        //                //            int g_compo = (value >> 16) & 0xff;
        //                //            if (g_compo == 40 || g_compo == 50)
        //                //            {
        //                //                //internal const int BORDER_INSIDE = 40;
        //                //                //internal const int BORDER_OUTSIDE = 50;

        //                //            }
        //                //        }
        //                //    }
        //                //}


        //#if DEBUG
        //                bmpLut.SaveImage("dbug_step2.png");
        //#endif


        //                //painter.RenderSurface.SetGamma(_prebuiltThresholdGamma_100);
        //                //_msdfEdgePxBlender.FillMode = MsdfEdgePixelBlender.BlenderFillMode.InnerAreaX;
        //                //painter.Fill(sh.CurrentSharedVxs, EdgeBmpLut.EncodeToColor(0, AreaKind.AreaInsideCoverage100));



        //                painter.SetCustomPixelBlender(null);
        //                painter.SetGamma(null);

        //                //
        //                List<CornerList> overlappedList = MakeUniqueList(_msdfEdgePxBlender._overlapList);
        //                edgeBmpLut.SetOverlappedList(overlappedList);

        //#if DEBUG

        //                if (dbugWriteMsdfTexture)
        //                {
        //                    //save for debug 
        //                    //we save to msdf_shape_lut2.png
        //                    //and check it from external program
        //                    //but we generate msdf bitmap from msdf_shape_lut.png 
        //                    bmpLut.SaveImage(dbug_msdf_shape_lutName);
        //                    var bmp5 = MemBitmapExt.LoadBitmap(dbug_msdf_shape_lutName);
        //                    int[] lutBuffer5 = bmp5.CopyImgBuffer(bmpLut.Width, bmpLut.Height);
        //                    //if (bmpLut.Width == 338 && bmpLut.Height == 477)
        //                    //{
        //                    //    dbugBreak = true;
        //                    //}
        //                    edgeBmpLut.SetBmpBuffer(bmpLut.Width, bmpLut.Height, lutBuffer5);
        //                    //generate actual sprite
        //                    PixelFarm.CpuBlit.BitmapAtlas.BitmapAtlasItemSource item = CreateMsdfImage(shape, MsdfGenParams, imgW, imgH, translateVec, edgeBmpLut);
        //                    //save msdf bitmap to file         
        //                    using (MemBitmap memBmp = MemBitmap.CreateFromCopy(item.Width, item.Height, item.Source))
        //                    {
        //                        memBmp.SaveImage(dbug_msdf_output);
        //                    }

        //                    return item;
        //                }

        //#endif

        //                //[B] after we have a lookup table
        //                int[] lutBuffer = bmpLut.CopyImgBuffer(bmpLut.Width, bmpLut.Height);
        //                edgeBmpLut.SetBmpBuffer(bmpLut.Width, bmpLut.Height, lutBuffer);
        //                edgeBmpLut.ContourOuterBorderW = OUTER_BORDER_W;
        //                edgeBmpLut.ContourInnerBorderW = INNER_BORDER_W;
        //                return CreateMsdfImage(shape, MsdfGenParams, imgW, imgH, translateVec, edgeBmpLut);
        //            }
        //        }

        public PixelFarm.CpuBlit.BitmapAtlas.BitmapAtlasItemSource GenerateMsdfTexture(VertexStore vxs)
        {

            Shape shape = CreateShape(vxs, out EdgeBmpLut edgeBmpLut);

            if (MsdfGenParams == null)
            {
                MsdfGenParams = new MsdfGenParams();//use default
            }

            //---preview bounds-----------
            PreviewSizeAndLocation(
               shape,
               MsdfGenParams,
               out int imgW, out int imgH,
               out Vector2 translateVec);

            _dx = translateVec.x;
            _dy = translateVec.y;
            //------------------------------------
            List<ContourCorner> corners = edgeBmpLut.Corners;
            TranslateCorners(corners, _dx, _dy);

            //[1] create lookup table (lut) bitmap that contains area/corner/shape information
            //each pixel inside it contains data that map to area/corner/shape


            using (MemBitmap bmpLut = new MemBitmap(imgW, imgH))
            using (Tools.BorrowAggPainter(bmpLut, out var painter))
            using (Tools.BorrowShapeBuilder(out var b))
            {

                _msdfEdgePxBlender.ClearOverlapList();//reset
                painter.SetCustomPixelBlender(_msdfEdgePxBlender);

                //1. clear all bg to black 
                painter.Clear(PixelFarm.Drawing.Color.Black);

                b.InitVxs(vxs) //...
                    .TranslateToNewVxs(_dx, _dy)
                    .Flatten();

                //2. fill border
                int cornerCount = corners.Count;
                List<int> cornerOfNextContours = edgeBmpLut.CornerOfNextContours;

                //fill inside and outside
                int startAt = 0;
                int n = 1;
                int corner_index = 1;
                _msdfEdgePxBlender.FillMode = MsdfEdgePixelBlender.BlenderFillMode.OuterBorder;
                for (int contour_index = 0; contour_index < cornerOfNextContours.Count; ++contour_index)
                {
                    //contour scope
                    int next_corner_startAt = cornerOfNextContours[contour_index];
                    //-----------
                    //AA-borders of the contour
                    painter.SetGamma(_prebuiltThresholdGamma_OverlappedBorder); //this creates overlapped area  
                    for (; n < next_corner_startAt; ++n)
                    {
                        //0-> 1
                        //1->2 ... n
                        FillBorder(painter, corners[n - 1], corners[n]);

#if DEBUG
                        //bmpLut.SaveImage("dbug_step" + curr_step + ".png");
                        //curr_step++;
#endif
                    }
                    {
                        //the last one 
                        //close contour, n-> 0
                        FillBorder(painter, corners[next_corner_startAt - 1], corners[startAt]);
#if DEBUG
                        //bmpLut.SaveImage("dbug_step" + curr_step + ".png");
                        //curr_step++;
#endif
                    }

                    startAt = next_corner_startAt;
                    n++;
                    corner_index++;
                }


                //again 
#if DEBUG
                //debug for output
                //painter.Fill(v7, Color.Red);
                bmpLut.SaveImage("dbug_step0.png");
                //int curr_step = 1;
#endif

                ////---------
                //2. force fill the shape (this include hole(s) inside shape to)
                //( we set threshold to 50 and do force fill)
                painter.SetGamma(_prebuiltThresholdGamma_50);
                _msdfEdgePxBlender.FillMode = MsdfEdgePixelBlender.BlenderFillMode.MakeInnerArea;
                painter.Fill(b.CurrentSharedVxs, EdgeBmpLut.EncodeToColor(0, AreaKind.AreaInsideCoverage50));


#if DEBUG
                bmpLut.SaveImage("dbug_step2.png");
#endif


                //painter.RenderSurface.SetGamma(_prebuiltThresholdGamma_100);
                //_msdfEdgePxBlender.FillMode = MsdfEdgePixelBlender.BlenderFillMode.InnerAreaX;
                //painter.Fill(sh.CurrentSharedVxs, EdgeBmpLut.EncodeToColor(0, AreaKind.AreaInsideCoverage100));


                painter.SetCustomPixelBlender(null);
                painter.SetGamma(null);

                //
                List<CornerList> overlappedList = MakeUniqueList(_msdfEdgePxBlender._overlapList);
                edgeBmpLut.SetOverlappedList(overlappedList);

#if DEBUG

                if (dbugWriteMsdfTexture)
                {
                    //save for debug 
                    //we save to msdf_shape_lut2.png
                    //and check it from external program
                    //but we generate msdf bitmap from msdf_shape_lut.png 
                    bmpLut.SaveImage(dbug_msdf_shape_lutName);
                    var bmp5 = MemBitmapExt.LoadBitmap(dbug_msdf_shape_lutName);
                    int[] lutBuffer5 = bmp5.CopyImgBuffer(bmpLut.Width, bmpLut.Height);

                    edgeBmpLut.SetBmpBuffer(bmpLut.Width, bmpLut.Height, lutBuffer5);
                    //generate actual sprite
                    PixelFarm.CpuBlit.BitmapAtlas.BitmapAtlasItemSource item = CreateMsdfImage(shape, MsdfGenParams, imgW, imgH, translateVec, edgeBmpLut);
                    //save msdf bitmap to file         
                    using (MemBitmap memBmp = MemBitmap.CreateFromCopy(item.Width, item.Height, item.Source))
                    {
                        memBmp.SaveImage(dbug_msdf_output);
                    }

                    return item;
                }

#endif

                //[B] after we have a lookup table
                int[] lutBuffer = bmpLut.CopyImgBuffer(bmpLut.Width, bmpLut.Height);
                edgeBmpLut.SetBmpBuffer(bmpLut.Width, bmpLut.Height, lutBuffer);
                edgeBmpLut.ContourOuterBorderW = OUTER_BORDER_W;
                edgeBmpLut.ContourInnerBorderW = INNER_BORDER_W;
                return CreateMsdfImage(shape, MsdfGenParams, imgW, imgH, translateVec, edgeBmpLut);
            }
        }

#if DEBUG
        public string dbug_msdf_shape_lutName = "msdf_shape_lut2.png";
        public string dbug_msdf_output = "msdf_shape.png";
        public static bool dbugBreak;
#endif




        readonly Dictionary<int, bool> _uniqueCorners = new Dictionary<int, bool>();

        List<CornerList> MakeUniqueList(List<CornerList> primaryOverlappedList)
        {

            List<CornerList> list = new List<CornerList>();
            //copy data to bmpLut
            int j = primaryOverlappedList.Count;
            for (int k = 0; k < j; ++k)
            {
                _uniqueCorners.Clear();
                CornerList overlapped = primaryOverlappedList[k];
                //each group -> make unique 
                CornerList newlist = new CornerList();
                int m = overlapped.Count;
                for (int n = 0; n < m; ++n)
                {
                    ushort corner = overlapped[n];
                    if (!_uniqueCorners.ContainsKey(corner))
                    {
                        _uniqueCorners.Add(corner, true);
                        newlist.Append(corner);
                    }
                }
                _uniqueCorners.Clear();
                // 
                list.Add(newlist);
            }
            return list;

        }
        static void TranslateCorners(List<ContourCorner> corners, double dx, double dy)
        {
            //test 2 if each edge has unique color
            int j = corners.Count;
            for (int i = 0; i < j; ++i)
            {
                corners[i].Offset(dx, dy);
            }
        }
        static void FlattenPoints(EdgeSegment segment, List<PointInfo> points)
        {
            switch (segment.SegmentKind)
            {
                default: throw new NotSupportedException();
                case EdgeSegmentKind.LineSegment:
                    {
                        LinearSegment seg = (LinearSegment)segment;
                        points.Add(new PointInfo(segment, PointInfoKind.Touch1, seg.P0));
                    }
                    break;
                case EdgeSegmentKind.QuadraticSegment:
                    {
                        QuadraticSegment seg = (QuadraticSegment)segment;
                        points.Add(new PointInfo(segment, PointInfoKind.Touch1, seg.P0));
                        points.Add(new PointInfo(segment, PointInfoKind.C2, seg.P1));
                    }
                    break;
                case EdgeSegmentKind.CubicSegment:
                    {
                        CubicSegment seg = (CubicSegment)segment;
                        points.Add(new PointInfo(segment, PointInfoKind.Touch1, seg.P0));
                        points.Add(new PointInfo(segment, PointInfoKind.C3, seg.P1));
                        points.Add(new PointInfo(segment, PointInfoKind.C3, seg.P2));
                    }
                    break;
            }

        }
        static void CreateCorners(List<PointInfo> points, List<ContourCorner> corners)
        {

            int j = points.Count;
            int beginAt = corners.Count;
            if (beginAt >= ushort.MaxValue)
            {
                throw new NotSupportedException();
            }

            for (int i = 1; i < j - 1; ++i)
            {
                ContourCorner corner = new ContourCorner(corners.Count, points[i - 1], points[i], points[i + 1]);
                corners.Add(corner);

#if DEBUG
                corner.dbugLeftIndex = beginAt + i - 1;
                corner.dbugMiddleIndex = beginAt + i;
                corner.dbugRightIndex = beginAt + i + 1;
#endif

            }

            {

                ContourCorner corner = new ContourCorner(corners.Count, points[j - 2], points[j - 1], points[0]);
                corners.Add(corner);
#if DEBUG
                corner.dbugLeftIndex = beginAt + j - 2;
                corner.dbugMiddleIndex = beginAt + j - 1;
                corner.dbugRightIndex = beginAt + 0;
#endif

            }

            {

                ContourCorner corner = new ContourCorner(corners.Count, points[j - 1], points[0], points[1]);
                corners.Add(corner);
#if DEBUG
                corner.dbugLeftIndex = beginAt + j - 1;
                corner.dbugMiddleIndex = beginAt + 0;
                corner.dbugRightIndex = beginAt + 1;
#endif
            }



        }
        static void CreateCorners(Contour contour, List<ContourCorner> output)
        {
            //create corner-arm relation for a given contour
            List<EdgeSegment> edges = contour.edges;
            int j = edges.Count;
            List<PointInfo> flattenPoints = new List<PointInfo>();
            for (int i = 0; i < j; ++i)
            {
                FlattenPoints(edges[i], flattenPoints);
            }
            CreateCorners(flattenPoints, output);
        }

        static Shape CreateShape(VertexStore vxs, out EdgeBmpLut bmpLut)
        {
            List<EdgeSegment> flattenEdges = new List<EdgeSegment>();
            Shape shape = new Shape(); //start with blank shape

            int i = 0;
            double x, y;
            VertexCmd cmd;
            Contour cnt = null;
            double latestMoveToX = 0;
            double latestMoveToY = 0;
            double latestX = 0;
            double latestY = 0;

            List<ContourCorner> corners = new List<ContourCorner>();
            List<int> edgeOfNextContours = new List<int>();
            List<int> cornerOfNextContours = new List<int>();

            while ((cmd = vxs.GetVertex(i, out x, out y)) != VertexCmd.NoMore)
            {
                switch (cmd)
                {
                    case VertexCmd.Close:
                        {
                            //close current cnt

                            if ((latestMoveToX != latestX) ||
                                (latestMoveToY != latestY))
                            {
                                //add line to close the shape
                                if (cnt != null)
                                {
                                    flattenEdges.Add(cnt.AddLine(latestX, latestY, latestMoveToX, latestMoveToY));
                                }
                            }
                            if (cnt != null)
                            {
                                //***                                
                                CreateCorners(cnt, corners);
                                edgeOfNextContours.Add(flattenEdges.Count);
                                cornerOfNextContours.Add(corners.Count);
                                shape.contours.Add(cnt);
                                //***
                                cnt = null;
                            }
                        }
                        break;
                    case VertexCmd.C3:
                        {

                            //C3 curve (Quadratic)                            
                            cnt ??= new Contour();
                            VertexCmd cmd1 = vxs.GetVertex(i + 1, out double x1, out double y1);
                            i++;
                            if (cmd1 != VertexCmd.LineTo)
                            {
                                throw new NotSupportedException();
                            }

                            //in this version, 
                            //we convert Quadratic to Cubic (https://stackoverflow.com/questions/9485788/convert-quadratic-curve-to-cubic-curve)

                            //Control1X = StartX + ((2f/3) * (ControlX - StartX))
                            //Control2X = EndX + ((2f/3) * (ControlX - EndX))


                            //flattenEdges.Add(cnt.AddCubicSegment(
                            //    latestX, latestY,
                            //    ((2f / 3) * (x - latestX)) + latestX, ((2f / 3) * (y - latestY)) + latestY,
                            //    ((2f / 3) * (x - x1)) + x1, ((2f / 3) * (y - y1)) + y1,
                            //    x1, y1));

                            flattenEdges.Add(cnt.AddQuadraticSegment(latestX, latestY, x, y, x1, y1));

                            latestX = x1;
                            latestY = y1;

                        }
                        break;
                    case VertexCmd.C4:
                        {
                            //C4 curve (Cubic)
                            cnt ??= new Contour();

                            VertexCmd cmd1 = vxs.GetVertex(i + 1, out double x2, out double y2);
                            VertexCmd cmd2 = vxs.GetVertex(i + 2, out double x3, out double y3);
                            i += 2;

                            if (cmd1 != VertexCmd.C4 || cmd2 != VertexCmd.LineTo)
                            {
                                throw new NotSupportedException();
                            }

                            flattenEdges.Add(cnt.AddCubicSegment(latestX, latestY, x, y, x2, y2, x3, y3));

                            latestX = x3;
                            latestY = y3;

                        }
                        break;
                    case VertexCmd.LineTo:
                        {
                            cnt ??= new Contour();
                            LinearSegment lineseg = cnt.AddLine(latestX, latestY, x, y);
                            flattenEdges.Add(lineseg);

                            latestX = x;
                            latestY = y;
                        }
                        break;
                    case VertexCmd.MoveTo:
                        {
                            latestX = latestMoveToX = x;
                            latestY = latestMoveToY = y;
                            if (cnt != null)
                            {
                                shape.contours.Add(cnt);
                                cnt = null;
                            }
                        }
                        break;
                }
                i++;
            }

            if (cnt != null)
            {
                shape.contours.Add(cnt);
                CreateCorners(cnt, corners);
                edgeOfNextContours.Add(flattenEdges.Count);
                cornerOfNextContours.Add(corners.Count);
                cnt = null;
            }

            GroupingOverlapContours(shape);

            //from a given shape we create a corner-arm for each corner  
            bmpLut = new EdgeBmpLut(corners, flattenEdges, edgeOfNextContours, cornerOfNextContours);

            return shape;
        }
        static void GroupingOverlapContours(Shape shape)
        {

            //if (shape.contours.Count > 1)
            //{
            //    //group contour into intersect group
            //    List<Contour> contours = shape.contours;
            //    int n = contours.Count;

            //    RectD[] boundsList = new RectD[n];
            //    for (int i = 0; i < n; ++i)
            //    {
            //        Contour c = contours[i];
            //        boundsList[i] = c.GetRectBounds();
            //    }

            //    //collapse all connected rgn

            //    List<ConnectedContours> connectedCnts = new List<ConnectedContours>();

            //    for (int i = 1; i < n; ++i)
            //    {
            //        Contour c0 = contours[i - 1];
            //        Contour c1 = contours[i];
            //        RectD b0 = c0.GetRectBounds();
            //        RectD b1 = c1.GetRectBounds();
            //        if (b0.IntersectWithRectangle(b1))
            //        {
            //            //if yes then we create a map
            //            ConnectedContours connContours = new ConnectedContours();
            //            connContours._members.Add(c0);
            //            connContours._members.Add(c1);
            //            connectedCnts.Add(connContours);
            //            i++;
            //        }
            //    }

            //}
        }

        internal static PixelFarm.CpuBlit.BitmapAtlas.BitmapAtlasItemSource CreateMsdfImage(Shape shape, MsdfGenParams genParams, int w, int h, Vector2 translate, EdgeBmpLut lutBuffer = null)
        {
            double edgeThreshold = genParams.edgeThreshold;
            if (edgeThreshold < 0)
            {
                edgeThreshold = 1.00000001; //use default if  edgeThreshold <0
            }

            var scale = new Vector2(genParams.scaleX, genParams.scaleY); //scale               
            double range = genParams.pxRange / Math.Min(scale.x, scale.y);
            //---------
            FloatRGBBmp frgbBmp = new FloatRGBBmp(w, h);

            EdgeColoring.edgeColoringSimple(shape, genParams.angleThreshold);

            bool flipY = false;
            // lutBuffer = null;
            if (lutBuffer != null)
            {
                GenerateMSDF3(frgbBmp,
                  shape,
                  range,
                  scale,
                  translate,//translate to positive quadrant
                  edgeThreshold,
                  lutBuffer);
                flipY = shape.InverseYAxis;
            }
            else
            {
                //use original msdf
                MsdfGenerator.generateMSDF(frgbBmp,
                  shape,
                  range,
                  scale,
                  translate,//translate to positive quadrant
                  edgeThreshold);
            }

            return new PixelFarm.CpuBlit.BitmapAtlas.BitmapAtlasItemSource(w, h)
            {
                Source = ConvertToIntBmp(frgbBmp, flipY),
                TextureXOffset = (float)translate.x,
                TextureYOffset = (float)translate.y
            };
        }

        static int[] ConvertToIntBmp(FloatRGBBmp input, bool flipY)
        {
            int height = input.Height;
            int width = input.Width;

            int[] output = new int[input.Width * input.Height];


            if (flipY)
            {
                int dstLineHead = width * (height - 1);
                for (int y = 0; y < height; ++y)
                {
                    for (int x = 0; x < width; ++x)
                    {
                        //a b g r
                        //----------------------------------
                        FloatRGB pixel = input.GetPixel(x, y);
                        //a b g r
                        //for big-endian color
                        //int abgr = (255 << 24) |
                        //    Vector2.Clamp((int)(pixel.r * 0x100), 0xff) |
                        //    Vector2.Clamp((int)(pixel.g * 0x100), 0xff) << 8 |
                        //    Vector2.Clamp((int)(pixel.b * 0x100), 0xff) << 16;

                        //for little-endian color

                        output[dstLineHead + x] = (255 << 24) |
                            Vector2.Clamp((int)(pixel.r * 0x100), 0xff) << 16 |
                            Vector2.Clamp((int)(pixel.g * 0x100), 0xff) << 8 |
                            Vector2.Clamp((int)(pixel.b * 0x100), 0xff);

                        //output[(y * width) + x] = abgr;
                        //----------------------------------
                        /**it++ = clamp(int(bitmap(x, y).r*0x100), 0xff);
                        *it++ = clamp(int(bitmap(x, y).g*0x100), 0xff);
                        *it++ = clamp(int(bitmap(x, y).b*0x100), 0xff);*/
                    }

                    dstLineHead -= width;
                }
            }
            else
            {
                int dstLineHead = 0;
                for (int y = 0; y < height; ++y)
                {
                    for (int x = 0; x < width; ++x)
                    {
                        //a b g r
                        //----------------------------------
                        FloatRGB pixel = input.GetPixel(x, y);
                        //a b g r
                        //for big-endian color
                        //int abgr = (255 << 24) |
                        //    Vector2.Clamp((int)(pixel.r * 0x100), 0xff) |
                        //    Vector2.Clamp((int)(pixel.g * 0x100), 0xff) << 8 |
                        //    Vector2.Clamp((int)(pixel.b * 0x100), 0xff) << 16;

                        //for little-endian color

                        output[dstLineHead + x] = (255 << 24) |
                            Vector2.Clamp((int)(pixel.r * 0x100), 0xff) << 16 |
                            Vector2.Clamp((int)(pixel.g * 0x100), 0xff) << 8 |
                            Vector2.Clamp((int)(pixel.b * 0x100), 0xff);

                        //output[(y * width) + x] = abgr;
                        //----------------------------------
                        /**it++ = clamp(int(bitmap(x, y).r*0x100), 0xff);
                        *it++ = clamp(int(bitmap(x, y).g*0x100), 0xff);
                        *it++ = clamp(int(bitmap(x, y).b*0x100), 0xff);*/
                    }

                    dstLineHead += width;
                }
            }
            return output;
        }

        static double Median(double a, double b, double c)
        {
            return Math.Max(Math.Min(a, b), Math.Min(Math.Max(a, b), c));
        }
        static double Max(double a, double b, double c)
        {
            return Math.Max(Math.Max(a, b), c);
        }
        static double Min(double a, double b, double c)
        {
            return Math.Min(Math.Min(a, b), c);
        }
        static void GenerateMSDF3(FloatRGBBmp output, Shape shape, double range, Vector2 scale, Vector2 translate, double edgeThreshold, EdgeBmpLut lut)
        {

            //----------------------
            //this is our extension,
            //we use lookup bitmap (lut) to check  
            //what is the nearest contour of a given pixel.   
            //----------------------  

            int w = output.Width;
            int h = output.Height;

            EdgeSegment[] singleSegment = new EdgeSegment[1];//temp array for 


            float outerBorderW = -lut.ContourOuterBorderW; //with sign
            for (int y = 0; y < h; ++y)
            {
                for (int x = 0; x < w; ++x)
                {
                    //PER-PIXEL-OPERATION
                    //check preview pixel               

                    int lutPix = lut.GetPixel(x, y);
                    int lutPixR = (lutPix & 0xFF);
                    int lutPixG = (lutPix >> 8) & 0xff;
                    int lutPixB = (lutPix >> 16) & 0xff;

                    if (lutPixG == 0)
                    {
                        //white
                        //output.SetPixel(x, y, new FloatRGB(1f, 1f, 1f));

                        //black
                        output.SetPixel(x, y, new FloatRGB(0f, 0f, 0f));
                        continue;
                    }

                    if (lutPixG == EdgeBmpLut.AREA_INSIDE_COVERAGE50)
                    {
                        //white
                        output.SetPixel(x, y, new FloatRGB(1f, 1f, 1f));
                        continue;
                        //inside the contour => fill all with black
                    }

                    //reset variables

                    EdgePoint r = new(), g = new(), b = new();

                    bool useR, useG, useB;
                    useR = useG = useB = true;
                    //------

                    Vector2 p = (new Vector2(x + .5, y + .5) / scale) - translate;

                    EdgeStructure edgeStructure = lut.GetEdgeStructure(x, y);

#if DEBUG
                    if (edgeStructure.IsEmpty)
                    {
                        //should not occurs
                        throw new NotSupportedException();
                    }
#endif
                    EdgeSegment[] edges = null;
                    if (edgeStructure.HasOverlappedSegments)
                    {
                        edges = edgeStructure.Segments;
                        //ensure unique

                    }
                    else
                    {
                        singleSegment[0] = edgeStructure.Segment;
                        edges = singleSegment;
                    }
                    //-------------

                    for (int i = 0; i < edges.Length; ++i)
                    {
                        EdgeSegment edge = edges[i];
                        SignedDistance distance = edge.signedDistance(p, out double param);//*** 
                        if (edge.HasComponent(EdgeColor.RED) && distance < r.minDistance)
                        {
                            r.minDistance = distance;
                            r.nearEdge = edge;
                            r.nearParam = param;
                            useR = false;
                        }
                        if (edge.HasComponent(EdgeColor.GREEN) && distance < g.minDistance)
                        {
                            g.minDistance = distance;
                            g.nearEdge = edge;
                            g.nearParam = param;
                            useG = false;
                        }
                        if (edge.HasComponent(EdgeColor.BLUE) && distance < b.minDistance)
                        {
                            b.minDistance = distance;
                            b.nearEdge = edge;
                            b.nearParam = param;
                            useB = false;
                        }
                    }


                    double contour_r = r.CalculateContourColor(p);
                    double contour_g = g.CalculateContourColor(p);
                    double contour_b = b.CalculateContourColor(p);

                    if (useB && contour_b <= SignedDistance.INFINITE.distance)
                    {
                        contour_b = 1 * range;
                    }
                    if (useG && contour_g <= SignedDistance.INFINITE.distance)
                    {
                        contour_g = 1 * range;
                    }
                    if (useR && contour_r <= SignedDistance.INFINITE.distance)
                    {
                        contour_r = 1 * range;
                    }

#if DEBUG
                    //debug
                    double ctr_r = contour_r;
                    double ctr_g = contour_g;
                    double ctr_b = contour_b;
#endif
                    //Negative values mean the cell is outside the shape.
                    //Positive values mean the cell is inside the shape.

                    if (lutPixG == EdgeBmpLut.BORDER_OVERLAP_OUTSIDE)
                    {

                        double median1 = Median(contour_r, contour_g, contour_b);
                        if (median1 > 0)
                        {
                            //outside => must be negative value

                            double min = Min(contour_r, contour_g, contour_b);

                            ////fix only 1 channel that we found the value 
                            //if (contour_r < max1)
                            //{
                            //    contour_r = max1;
                            //}
                            //else if (contour_g < max1)
                            //{
                            //    contour_g = max1;
                            //}
                            //else if (contour_b < max1)
                            //{
                            //    contour_b = max1;
                            //}
                            if (contour_r > min)
                            {
                                contour_r = min;
                            }
                            else if (contour_g < min)
                            {
                                contour_g = min;
                            }
                            else if (contour_b > min)
                            {
                                contour_b = min;
                            }

                        }
                    }
                    else if (lutPixG == EdgeBmpLut.BORDER_OVERLAP_INSIDE)
                    {
                        double median1 = Median(contour_r, contour_g, contour_b);
                        if (median1 < 0)
                        {
                            //inside => must be positive value

                            if (contour_r < 0)
                            {
                                contour_r = -contour_r;
                            }
                            if (contour_g < 0)
                            {
                                contour_g = -contour_g;
                            }
                            if (contour_b < 0)
                            {
                                contour_b = -contour_b;
                            }
                        }
                    }
                    output.SetPixel(x, y, new FloatRGB(
                           (float)(contour_r / range + .5),
                           (float)(contour_g / range + .5),
                           (float)(contour_b / range + .5)
                       ));
                }
            }
        }

    }
}