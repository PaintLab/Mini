//MIT, 2016, Viktor Chlumsky, Multi-channel signed distance field generator, from https://github.com/Chlumsky/msdfgen
//MIT, 2017-present, WinterDev (C# port)
using System;
using System.Collections.Generic;
using PixelFarm.Drawing;

namespace Msdfgen
{
    class CornerList
    {
        readonly List<ushort> _list = new List<ushort>();

        int _latestCorner = -1;//hint, reduce duplicated corner in this list (but not make this unique)
#if DEBUG
        public readonly int dbugId = s_dbugTotalId++;
        static int s_dbugTotalId;
#endif
        public CornerList()
        {

#if DEBUG
            if (dbugId == 124 || dbugId == 1511)
            {

            }
            if (dbugId >= 388)
            {

            }
#endif
        }
        public void Append(ushort corner)
        {
#if DEBUG
            if (dbugId == 124 || dbugId == 389)
            {

            }
#endif

            if (_latestCorner == corner) return;

            _list.Add(corner);
            _latestCorner = corner;

        }
        public void Append(CornerList another)
        {
#if DEBUG
            if (dbugId == 124 || dbugId == 389)
            {

            }
#endif

            _list.AddRange(another._list);
        }
        public int Count => _list.Count;
        public ushort this[int index] => _list[index];
#if DEBUG
        public override string ToString() => _list.Count.ToString();
#endif
    }


    class MsdfEdgePixelBlender : PixelFarm.CpuBlit.PixelProcessing.CustomPixelBlender
    {

        public enum BlenderFillMode
        {
            OuterBorder,
            MakeInnerArea,
        }

        const int BLACK = (255 << 24);

        readonly struct OverlapPart
        {
            readonly int _edgeA;
            readonly int _edgeB;
            public OverlapPart(int edgeA, int edgeB)
            {
#if DEBUG
                if (edgeA == edgeB)
                {
                    throw new NotSupportedException();
                }
#endif
                if (edgeB < edgeA)
                {
                    //swap
                    _edgeA = edgeB;
                    _edgeB = edgeA;
                }
                else
                {
                    _edgeA = edgeA;
                    _edgeB = edgeB;
                }

            }
#if DEBUG
            public override string ToString()
            {
                return _edgeA + "," + _edgeB;
            }
#endif
        }

        Dictionary<OverlapPart, ushort> _overlapParts = new Dictionary<OverlapPart, ushort>();
        internal List<CornerList> _overlapList = new List<CornerList>();

        //int _areaInside100;


        public MsdfEdgePixelBlender()
        {
        }


        public BlenderFillMode FillMode { get; set; }

        public void ClearOverlapList()
        {
            _overlapParts.Clear();
            _overlapList.Clear();
        }

        public ushort RegisterOverlapOuter(ushort corner1, ushort corner2, AreaKind areaKind)
        {

            OverlapPart overlapPart = new OverlapPart(corner1, corner2);
            if (!_overlapParts.TryGetValue(overlapPart, out ushort found))
            {
                if (_overlapList.Count > ushort.MaxValue)
                {
                    throw new NotSupportedException();
                }

                ushort newPartNo = (ushort)_overlapList.Count;
                _overlapParts.Add(overlapPart, newPartNo);


                CornerList cornerList = new CornerList();
                _overlapList.Add(cornerList);
                cornerList.Append(corner1);
                cornerList.Append(corner2);
            }
            return found;
        }
        protected override unsafe void BlendPixel32Internal(int* dstPtr, Color srcColor)
        {
            CustomBlendPixel32(dstPtr, srcColor);
        }
        protected override unsafe void BlendPixel32Internal(int* dstPtr, Color srcColor, int coverageValue)
        {
            CustomBlendPixel32(dstPtr, srcColor);
        }


        unsafe void CustomBlendPixel32(int* dstPtr, Color srcColor)
        {
            int existingColor = *dstPtr;
            int existing_G = (existingColor >> PixelFarm.Drawing.Internal.CO.G_SHIFT) & 0xFF;
            if (FillMode == BlenderFillMode.MakeInnerArea)
            {
                if (existingColor == BLACK)
                {
                    *dstPtr = srcColor.ToARGB();
                    return;
                }
                else if (existing_G == EdgeBmpLut.BORDER_OVERLAP_OUTSIDE)
                {
                    //convert to border overlap inside
                    byte a = (byte)((existingColor >> PixelFarm.Drawing.Internal.CO.A_SHIFT) & 0xff);
                    byte b = (byte)((existingColor >> PixelFarm.Drawing.Internal.CO.B_SHIFT) & 0xff);
                    byte r = (byte)((existingColor >> PixelFarm.Drawing.Internal.CO.R_SHIFT) & 0xff);
                    *dstPtr = a << PixelFarm.Drawing.Internal.CO.A_SHIFT | (b << PixelFarm.Drawing.Internal.CO.B_SHIFT) |
                              (EdgeBmpLut.BORDER_OVERLAP_INSIDE << PixelFarm.Drawing.Internal.CO.G_SHIFT) | (r << PixelFarm.Drawing.Internal.CO.R_SHIFT);
                    return;

                }
                else if (existing_G == EdgeBmpLut.BORDER_OUTSIDE)
                {

                    byte a = (byte)((existingColor >> PixelFarm.Drawing.Internal.CO.A_SHIFT) & 0xff);
                    byte b = (byte)((existingColor >> PixelFarm.Drawing.Internal.CO.B_SHIFT) & 0xff);
                    byte r = (byte)((existingColor >> PixelFarm.Drawing.Internal.CO.R_SHIFT) & 0xff);
                    *dstPtr = a << PixelFarm.Drawing.Internal.CO.A_SHIFT | (b << PixelFarm.Drawing.Internal.CO.B_SHIFT) |
                              (EdgeBmpLut.BORDER_INSIDE << PixelFarm.Drawing.Internal.CO.G_SHIFT) | (r << PixelFarm.Drawing.Internal.CO.R_SHIFT);
                    return;
                }
                else
                {

                }
            }
            int srcColorABGR = (int)srcColor.ToABGR();
            if (existingColor == BLACK)
            {
                *dstPtr = srcColor.ToARGB();
                return;
            }
            if (srcColorABGR == existingColor)
            {
                return;
            }

            //-------------------------------------------------------------
            //decode edge information
            //we use 2 bytes for encode edge number 

            ushort existingEdgeNo = EdgeBmpLut.DecodeEdgeFromColor(existingColor, out AreaKind existingAreaKind);
            ushort newEdgeNo = EdgeBmpLut.DecodeEdgeFromColor(srcColor, out AreaKind newEdgeAreaKind);

            if (existingEdgeNo == newEdgeNo)
            {
                return;
            }

            if (newEdgeAreaKind == AreaKind.OverlapInside || newEdgeAreaKind == AreaKind.OverlapOutside)
            {
                //new color is overlap color 
                if (existingAreaKind == AreaKind.OverlapInside || existingAreaKind == AreaKind.OverlapOutside)
                {
                    CornerList registerList = _overlapList[newEdgeNo];
                    _overlapList[existingEdgeNo].Append(registerList);
                }
                else
                {
                    CornerList registerList = _overlapList[newEdgeNo];
                    registerList.Append(existingEdgeNo);
                    *dstPtr = EdgeBmpLut.EncodeToColor(newEdgeNo, (existing_G == EdgeBmpLut.BORDER_INSIDE) ? AreaKind.OverlapInside : AreaKind.OverlapOutside).ToARGB();
                }
            }
            else
            {
                if (existingAreaKind == AreaKind.OverlapInside ||
                    existingAreaKind == AreaKind.OverlapOutside)
                {
                    _overlapList[existingEdgeNo].Append(newEdgeNo);
                }
                else
                {
                    OverlapPart overlapPart = new OverlapPart(existingEdgeNo, newEdgeNo);
                    AreaKind areaKind = (existingAreaKind == AreaKind.BorderInside) ? AreaKind.OverlapInside : AreaKind.OverlapOutside;

                    if (!_overlapParts.TryGetValue(overlapPart, out ushort found))
                    {
                        if (_overlapList.Count >= ushort.MaxValue)
                        {
                            throw new NotSupportedException();
                        }
                        //
                        ushort newPartNo = (ushort)_overlapList.Count;
                        _overlapParts.Add(overlapPart, newPartNo);
                        //
                        CornerList cornerList = new CornerList();
#if DEBUG
                        if (_overlapList.Count >= 388)
                        {

                        }
#endif

                        _overlapList.Add(cornerList);

                        cornerList.Append(existingEdgeNo);
                        cornerList.Append(newEdgeNo);
                        //set new color
                        *dstPtr = EdgeBmpLut.EncodeToColor(newPartNo, areaKind).ToARGB();
                    }
                    else
                    {
                        //set new color
                        *dstPtr = EdgeBmpLut.EncodeToColor(found, areaKind).ToARGB();
                    }
                }
            }
        }
    }



    /// <summary>
    /// edge bitmap lookup table
    /// </summary>
    public class EdgeBmpLut
    {
        int _w;
        int _h;
        int[] _buffer;
        readonly List<ContourCorner> _corners;
#if DEBUG
        readonly List<EdgeSegment> _flattenEdges;
#endif
        List<EdgeSegment[]> _overlappedEdgeList;

        public float ContourOuterBorderW;
        public float ContourInnerBorderW;
        internal EdgeBmpLut(List<ContourCorner> corners, List<EdgeSegment> flattenEdges, List<int> segOfNextContours, List<int> cornerOfNextContours)
        {
            //move first to last 
            int startAt = 0;
            for (int i = 0; i < segOfNextContours.Count; ++i)
            {
                int nextStartAt = segOfNextContours[i];
                //
                EdgeSegment firstSegment = flattenEdges[startAt];

                flattenEdges.RemoveAt(startAt);
                if (i == segOfNextContours.Count - 1)
                {
                    flattenEdges.Add(firstSegment);
                }
                else
                {
                    flattenEdges.Insert(nextStartAt - 1, firstSegment);
                }
                startAt = nextStartAt;
            }

            _corners = corners;
#if DEBUG
            _flattenEdges = flattenEdges;
#endif
            EdgeOfNextContours = segOfNextContours;
            CornerOfNextContours = cornerOfNextContours;
        }
        public List<int> EdgeOfNextContours { get; }
        public List<int> CornerOfNextContours { get; }

        internal void SetOverlappedList(List<CornerList> overlappedList)
        {
            int m = overlappedList.Count;
            _overlappedEdgeList = new List<EdgeSegment[]>(m);
            for (int i = 0; i < m; ++i)
            {
#if DEBUG
                //if (i == 124 || i == 389)
                //{

                //}
#endif
                CornerList cornerList = overlappedList[i];
                int count = cornerList.Count;
                EdgeSegment[] edgeSegments = new EdgeSegment[count];//overlapping corner region
                for (int a = 0; a < count; ++a)
                {
                    edgeSegments[a] = _corners[cornerList[a]].CenterSegment;
                }

                _overlappedEdgeList.Add(edgeSegments);
            }
        }

        public void SetBmpBuffer(int w, int h, int[] buffer)
        {
            _w = w;
            _h = h;
            _buffer = buffer;
        }
        public List<ContourCorner> Corners => _corners;

        public int GetPixel(int x, int y) => _buffer[y * _w + x];

        public EdgeStructure GetEdgeStructure(int x, int y)
        {
            //decode 
            int pixel = _buffer[y * _w + x];
            int pix_G = (pixel >> 8) & 0xFF;
            if (pixel == 0 || pix_G == 0)
            {
                return new EdgeStructure(); //empty
            }
            else if (pix_G == AREA_INSIDE_COVERAGE50)
            {
                return new EdgeStructure();
            }
            else
            {
                int index = DecodeEdgeFromColor(pixel, out AreaKind areaKind);
                switch (areaKind)
                {
                    default: throw new NotSupportedException();
                    case AreaKind.BorderOutside:
                    case AreaKind.BorderInside:
                        return new EdgeStructure(_corners[index].CenterSegment, areaKind);
                    case AreaKind.OverlapInside:
                    case AreaKind.OverlapOutside:
                        return new EdgeStructure(_overlappedEdgeList[index], areaKind);
                }
            }
        }


        internal const int AREA_INSIDE_COVERAGE50 = 15;

        internal const int BORDER_INSIDE = 40;
        internal const int BORDER_OUTSIDE = 50;

        internal const int BORDER_OVERLAP_INSIDE = 70;
        internal const int BORDER_OVERLAP_OUTSIDE = 75;

        public static ushort DecodeEdgeFromColor(Color c, out AreaKind areaKind)
        {
            switch ((int)c.G)
            {

                case AREA_INSIDE_COVERAGE50: areaKind = AreaKind.AreaInsideCoverage50; break;
                case BORDER_INSIDE: areaKind = AreaKind.BorderInside; break;

                case BORDER_OUTSIDE: areaKind = AreaKind.BorderOutside; break;
                case BORDER_OVERLAP_INSIDE: areaKind = AreaKind.OverlapInside; break;
                case BORDER_OVERLAP_OUTSIDE: areaKind = AreaKind.OverlapOutside; break;

                default: throw new NotSupportedException();
            }
            return (ushort)((c.R << 8) | c.B);
        }
        public static ushort DecodeEdgeFromColor(int inputColor, out AreaKind areaKind)
        {
            //int inputR = (inputColor >> CO.R_SHIFT) & 0xFF;
            //int inputB = (inputColor >> CO.B_SHIFT) & 0xFF;
            //int inputG = (inputColor >> CO.G_SHIFT) & 0xFF;

            //ABGR

            int inputB = (inputColor >> 0) & 0xFF;
            int inputG = (inputColor >> 8) & 0xFF;
            int inputR = (inputColor >> 16) & 0xFF;

            switch (inputG)
            {

                case AREA_INSIDE_COVERAGE50: areaKind = AreaKind.AreaInsideCoverage50; break;
                case BORDER_INSIDE: areaKind = AreaKind.BorderInside; break;

                case BORDER_OUTSIDE: areaKind = AreaKind.BorderOutside; break;
                case BORDER_OVERLAP_INSIDE: areaKind = AreaKind.OverlapInside; break;
                case BORDER_OVERLAP_OUTSIDE: areaKind = AreaKind.OverlapOutside; break;

                default: throw new NotSupportedException();
            }
            return (ushort)((inputR << 8) | inputB);
        }
        public static PixelFarm.Drawing.Color EncodeToColor(ushort cornerNo, AreaKind areaKind)
        {

            switch (areaKind)
            {
                default: throw new NotSupportedException();
                case AreaKind.BorderInside:
                    {
                        int r = cornerNo >> 8;
                        int b = cornerNo & 0xFF;

                        return new PixelFarm.Drawing.Color((byte)r, BORDER_INSIDE, (byte)b);
                    }

                case AreaKind.BorderOutside:
                    {
                        int r = cornerNo >> 8;
                        int b = cornerNo & 0xFF;
                        return new PixelFarm.Drawing.Color((byte)r, BORDER_OUTSIDE, (byte)b);
                    }
                case AreaKind.OverlapInside:
                    {
                        int r = cornerNo >> 8;
                        int b = cornerNo & 0xFF;
#if DEBUG
                        if (cornerNo == 389)
                        {

                        }
#endif
                        return new PixelFarm.Drawing.Color((byte)r, BORDER_OVERLAP_INSIDE, (byte)b);
                    }
                case AreaKind.OverlapOutside:
                    {
                        int r = cornerNo >> 8;
                        int b = cornerNo & 0xFF;
                        return new PixelFarm.Drawing.Color((byte)r, BORDER_OVERLAP_OUTSIDE, (byte)b);
                    }
                case AreaKind.AreaInsideCoverage50:
                    {
                        //corner=> shape number (id)
                        int r = cornerNo >> 8;
                        int b = cornerNo & 0xFF;
                        return new PixelFarm.Drawing.Color((byte)r, AREA_INSIDE_COVERAGE50, (byte)b);
                    }
            }
        }
    }

}