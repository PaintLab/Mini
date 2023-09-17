//MIT, 2016-present, WinterDev

using System;
using PixelFarm.Drawing;

using PixelFarm.CpuBlit.VertexProcessing;
using PixelFarm.CpuBlit.PixelProcessing;

namespace PixelFarm.CpuBlit
{

    partial class AggPainter
    {
        AggPainterCore _pcx_mask;

        TargetBufferName _targetBufferName;
        bool _enableBuiltInMaskComposite;
        MemBitmap _alphaBitmap;

        PixelBlender32 _defaultPixelBlender;
        PixelBlenderWithMask _maskPixelBlender;
        PixelBlenderPerColorComponentWithMask _maskPixelBlenderPerCompo;
        ClipingTechnique _currentClipTech;

        void ClearClipRgn()
        {
            //remove clip rgn if exists**
            switch (_currentClipTech)
            {
                case ClipingTechnique.ClipMask:
                    this.EnableBuiltInMaskComposite = false;
                    this.TargetBufferName = TargetBufferName.AlphaMask;//swicth to mask buffer
                    this.Clear(Color.Black);
                    this.TargetBufferName = TargetBufferName.Default;

                    break;
                case ClipingTechnique.ClipSimpleRect:

                    this.SetClipBox(0, 0, this.Width, this.Height);
                    break;
            }

            _currentClipTech = ClipingTechnique.None;
        }

        public override void SetClipRgn(RenderVx maskRenderVx)
        {
            throw new NotImplementedException();
        }
        public override void SetClipRgn(Image maskImg)
        {
            throw new NotImplementedException();
        }
        /// <summary>
        /// we DO NOT store vxs
        /// </summary>
        /// <param name="vxs"></param>
        public override void SetClipRgn(VertexStore vxs)
        {
            //clip rgn implementation
            //this version replace only
            //TODO: add append clip rgn 
            if (vxs != null)
            {
                if (SimpleRectClipEvaluator.EvaluateRectClip(vxs, out RectangleF clipRect))
                {

                    this.SetClipBox(
                        (int)Math.Floor(clipRect.Left), (int)Math.Floor(clipRect.Top),
                        (int)Math.Ceiling(clipRect.Right), (int)Math.Ceiling(clipRect.Bottom));

                    _currentClipTech = ClipingTechnique.ClipSimpleRect;
                }
                else
                {
                    //not simple rect => 
                    //use mask technique

                    _currentClipTech = ClipingTechnique.ClipMask;
                    //1. switch to mask buffer
                    this.TargetBufferName = TargetBufferName.AlphaMask;
                    //2.
                    Color prevColor = this.FillColor; //save
                    Brush prevBrush = CurrentBrush;

                    this.FillColor = Color.White;
                    _pcx.Render(vxs, FillColor);

                    //fill vxs with white color (on black bg)

                    this.FillColor = prevColor; //restore
                    CurrentBrush = prevBrush;//restore

                    //3. switch back to default layer
                    this.TargetBufferName = TargetBufferName.Default;//swicth to default buffer
                    this.EnableBuiltInMaskComposite = true;
                }
            }
            else
            {
                ClearClipRgn();
            }
        }

        public override void GetClipBox(out int x1, out int y1, out int x2, out int y2)
        {
            //TODO: review here again!
            throw new NotSupportedException();
            //x1 = 0; y1 = 0;
            //x2 = 0; y2 = 0;
        }
        public override void SetClipBox(int x1, int y1, int x2, int y2)
        {
            _pcx.SetClippingRect(new Q1Rect(x1, y1, x2, y2));
        }
        //---------------------------------------------------------------

        void SetupMaskPixelBlender()
        {
            //create when need and
            //after _aggsx_0 is attach to the surface
            GetOrigin(out float ox, out float oy);
            if (_pcx_mask != null)
            {
                //also set the canvas origin for the aggsx_mask
                _pcx_mask.SetScanlineRasOrigin(ox, oy);
                return;//***
            }
            //----------
            //same size as primary _aggsx_0 

            _alphaBitmap = new MemBitmap(_pcx_0.Width, _pcx_0.Height);
            //create painter core and attach to the bitmap
            _pcx_mask = new AggPainterCore() { PixelBlender = new PixelBlenderBGRA() };
            _pcx_mask.AttachDstBitmap(_alphaBitmap);
            _pcx_mask.SetScanlineRasOrigin(ox, oy); //also set the canvas origin for the aggsx_mask
#if DEBUG
            _pcx_mask.dbugName = "mask";
            _alphaBitmap._dbugNote = "AggPrinter.SetupMaskPixelBlender";
#endif
            _maskPixelBlender = new PixelBlenderWithMask();
            _maskPixelBlenderPerCompo = new PixelBlenderPerColorComponentWithMask();

            _maskPixelBlender.SetMaskBitmap(_alphaBitmap); //same alpha bitmap
            _maskPixelBlenderPerCompo.SetMaskBitmap(_alphaBitmap); //same alpha bitmap
        }
        void DetachMaskPixelBlender()
        {
            if (_pcx_mask != null)
            {
                _pcx_mask.DetachDstBitmap();
                _pcx_mask = null;

                _maskPixelBlender = null; //remove blender
                _maskPixelBlenderPerCompo = null;
            }
            if (_alphaBitmap != null)
            {
                _alphaBitmap.Dispose();
                _alphaBitmap = null;
            }

        }
        void UpdateTargetBuffer(TargetBufferName value)
        {
            //
            _targetBufferName = value;

            if (_pcx.DestBitmap != null)
            {
                switch (value)
                {
                    default: throw new NotSupportedException();
                    case TargetBufferName.Default:
                        //default 
                        _pcx = _pcx_0; //*** 
                        break;
                    case TargetBufferName.AlphaMask:
                        SetupMaskPixelBlender();
                        _pcx = _pcx_mask;//*** 
                        break;
                }

            }
        }

        public TargetBufferName TargetBufferName
        {
            get => _targetBufferName;
            set
            {
                if (_targetBufferName == value) { return; }
                //
                UpdateTargetBuffer(value);
            }
        }
        public bool EnableBuiltInMaskComposite
        {
            get => _enableBuiltInMaskComposite;
            set
            {
                if (_enableBuiltInMaskComposite == value) { return; }
                //
                _enableBuiltInMaskComposite = value;
                if (value)
                {
                    //use mask composite
                    this.DestBitmapBlender.OutputPixelBlender = _maskPixelBlender;
                }
                else
                {
                    //use default composite
                    this.DestBitmapBlender.OutputPixelBlender = _defaultPixelBlender;
                }

            }
        }

        public override void FillRegion(Region rgn)
        {
            if (!(rgn is CpuBlitRegion region)) return;
            switch (region.Kind)
            {
                case CpuBlitRegion.CpuBlitRegionKind.BitmapBasedRegion:
                    {
                        var bmpRgn = (PixelFarm.PathReconstruction.BitmapBasedRegion)region;
                        //for bitmap that is used to be a region...
                        //our convention is ...
                        //  non-region => black
                        //  region => white                        
                        //(same as the Typography GlyphTexture)
                        MemBitmap rgnBitmap = bmpRgn.GetRegionBitmap();
                        DrawImage(rgnBitmap);
                    }
                    break;
                case CpuBlitRegion.CpuBlitRegionKind.VxsRegion:
                    {
                        //fill 'hole' of the region
                        var vxsRgn = (PixelFarm.PathReconstruction.VxsRegion)region;
                        Fill(vxsRgn.GetVxs());
                    }
                    break;
                case CpuBlitRegion.CpuBlitRegionKind.MixedRegion:
                    {
                        var mixedRgn = (PixelFarm.PathReconstruction.MixedRegion)region;
                    }
                    break;
            }
        }

        public override void DrawRegion(Region rgn)
        {
            if (!(rgn is PixelFarm.CpuBlit.CpuBlitRegion region)) return;
            switch (region.Kind)
            {
                case CpuBlitRegion.CpuBlitRegionKind.BitmapBasedRegion:
                    {
                        var bmpRgn = (PixelFarm.PathReconstruction.BitmapBasedRegion)region;
                        //check if it has outline data or not
                        //if not then just return 
                    }
                    break;
                case CpuBlitRegion.CpuBlitRegionKind.VxsRegion:
                    {
                        //draw outline of the region
                        var vxsRgn = (PixelFarm.PathReconstruction.VxsRegion)region;
                        Draw(vxsRgn.GetVxs());
                    }
                    break;
                case CpuBlitRegion.CpuBlitRegionKind.MixedRegion:
                    {
                        var mixedRgn = (PixelFarm.PathReconstruction.MixedRegion)region;
                    }
                    break;
            }
        }

        public override void FillRegion(VertexStore vxs)
        {

            this.SetClipRgn(vxs);

            GetOrigin(out float ox, out float oy);
            Q1RectD bounds = vxs.GetBoundingRect();
            SetOrigin((float)(ox + bounds.Left), (float)(oy + bounds.Bottom));

            FillRect(0, 0, bounds.Width, bounds.Height);

            SetClipRgn(null as VertexStore);
            SetOrigin(ox, oy);
        }
        public override void DrawRegion(VertexStore vxs)
        {
            throw new NotImplementedException();
        }
    }

    public enum TargetBufferName
    {
        Unknown,
        Default,
        AlphaMask
    }
}