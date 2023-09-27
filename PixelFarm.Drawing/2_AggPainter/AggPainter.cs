//MIT, 2016-present, WinterDev

using System;
using PixelFarm.Drawing;
using PixelFarm.CpuBlit.VertexProcessing;
using PixelFarm.CpuBlit.PixelProcessing;
using PixelFarm.CpuBlit.Rasterization;

namespace PixelFarm.CpuBlit
{

    public partial class AggPainter : Painter
    {

        AggPainterCore _pcx; //target rendering surface   
        AggPainterCore _pcx_0; //primary render surface

        SmoothingMode _smoothingMode;

        RenderSurfaceOriginKind _orientation;
        TargetBuffer _targetBuffer;
        float _fillOpacity = 1;
        bool _hasFillOpacity = false;


        static readonly PrebuiltGammaTable s_gammaNone;
        static readonly PrebuiltGammaTable s_gammaThreshold50;
        static AggPainter()
        {
            s_gammaNone = new PrebuiltGammaTable(new GammaNone());
            s_gammaThreshold50 = new PrebuiltGammaTable(new GammaThreshold(0.5f));
        }

        public AggPainter(AggPainterCore pcx)
        {
            //painter paint to target surface
            _orientation = RenderSurfaceOriginKind.LeftBottom;
            //----------------------------------------------------
            _pcx = _pcx_0 = pcx; //set this as default *** 

            _pcx_0.DstBitmapAttached += (s, e) =>
            {
                UpdateTargetBuffer(_targetBufferName);
            };
            _pcx_0.DstBitmapDetached += (s, e) =>
            {
                DetachMaskPixelBlender();
            };


            TargetBufferName = TargetBufferName.Default;
            _stroke = new Stroke(1);//default
            _useDefaultBrush = true;
            _defaultPixelBlender = this.DestBitmapBlender.OutputPixelBlender;
        }

        public override RenderSurface CreateNewRenderSurface(int w, int h)
        {
            return new MemBitmapRenderSurface(new MemBitmap(w, h), true);
        }

        public MemBitmap CurrentDestBitmap => _pcx.DestBitmap;
        public AggPainterCore Core => _pcx;

        public void SetGamma(PrebuiltGammaTable gamma) => _pcx.SetGamma(gamma);
        public void SetCustomPixelBlender(CustomPixelBlender pxblender) => _pcx.SetCustomPixelBlender(pxblender);

        public void AttachDstBitmap(MemBitmap bmp) => _pcx.AttachDstBitmap(bmp);
        public void Reset()
        {
            //TODO: ...
            //reset to init state
            //
            _pcx.DetachDstBitmap();
            FillingRule = FillingRule.NonZero;

        }
        public override FillingRule FillingRule
        {
            //TODO: set filling for both aggsx (default and mask)
            get => _pcx.FillingRule;
            set => _pcx.FillingRule = value;
        }
        public override float FillOpacity
        {
            get => _fillOpacity;
            set
            {
                _fillOpacity = value;
                if (value < 0)
                {
                    _fillOpacity = 0;
                    _hasFillOpacity = true;
                }
                else if (value >= 1)
                {
                    _fillOpacity = 1;
                    _hasFillOpacity = false;
                }
                else
                {
                    _fillOpacity = value;
                    _hasFillOpacity = true;
                }
            }
        }


        public override Color TextBackgroundColorHint { get; set; }

        public override TargetBuffer TargetBuffer
        {
            get => _targetBuffer;
            set
            {
                if (_targetBuffer == value) return;

                _targetBuffer = value;
                switch (value)
                {
                    case TargetBuffer.ColorBuffer:
                        this.TargetBufferName = TargetBufferName.Default;
                        break;
                    case TargetBuffer.MaskBuffer:
                        this.TargetBufferName = TargetBufferName.AlphaMask;
                        break;
                    default: throw new NotSupportedException();
                }
            }
        }
        public override bool EnableMask
        {
            get => EnableBuiltInMaskComposite;
            set => EnableBuiltInMaskComposite = value;
        }
        public override ICoordTransformer CoordTransformer
        {
            get => _pcx.CurrentTransformMatrix;
            set => _pcx.CurrentTransformMatrix = value;
        }


        public BitmapBlenderBase DestBitmapBlender => _pcx.DestBitmapBlender;
        public override int Width => _pcx.Width;
        public override int Height => _pcx.Height;
        public override void GetOrigin(out float ox, out float oy)
        {
            ox = _pcx.ScanlineRasOriginX;
            oy = _pcx.ScanlineRasOriginY;
        }
        public override void SetOrigin(float x, float y) => _pcx.SetScanlineRasOrigin(x, y);

        public override void Clear(Color color) => _pcx.Clear(color);
        public void Clear(Color color, int left, int top, int width, int height) => _pcx.Clear(color, left, top, width, height);


        RenderQuality _renderQuality; //default = High
        public override RenderQuality RenderQuality
        {
            get => _renderQuality;
            set
            {
                if (_renderQuality != value)
                {
                    //change
                    _renderQuality = value;
                    if (value == RenderQuality.HighQuality)
                    {
                        _pcx.SetGamma(s_gammaNone);
                    }
                    else
                    {
                        _pcx.SetGamma(s_gammaThreshold50);
                    }
                }
            }
        }

        public override RenderSurfaceOriginKind Orientation
        {
            get => _orientation;
            set => _orientation = value;
        }
        public override SmoothingMode SmoothingMode
        {
            get => _smoothingMode;
            set
            {
                switch (_smoothingMode = value)
                {
                    case Drawing.SmoothingMode.HighQuality:
                    case Drawing.SmoothingMode.AntiAlias:
                        //TODO: review here
                        //anti alias != lcd technique 
                        this.RenderQuality = RenderQuality.HighQuality;
                        //_aggsx.UseSubPixelLcdEffect = true;
                        break;
                    case Drawing.SmoothingMode.HighSpeed:
                    default:
                        this.RenderQuality = RenderQuality.Low;
                        _pcx.UseSubPixelLcdEffect = false;
                        break;
                }
            }
        }
        public override void Render(RenderVx renderVx)
        {
            //VG Render?
            //if (renderVx is VgRenderVx)
            //{

            //}
            //else
            //{
            //    //?
            //    throw new NotSupportedException();
            //}
        }
        public override RenderVx CreateRenderVx(VertexStore vxs)
        {
            return new AggRenderVx(vxs);
        }
        public override Region CreateRegion(VertexStore vxs)
        {
            throw new NotImplementedException();
        }
        public override Region CreateRegion(Image img)
        {
            throw new NotImplementedException();
        }

        public static AggPainter Create(MemBitmap bmp, PixelProcessing.PixelBlender32 blender = null)
        {
            //helper func

            AggPainterCore pcx = new AggPainterCore();
            pcx.AttachDstBitmap(bmp);

            if (blender == null)
            {
                blender = new PixelProcessing.PixelBlenderBGRA();
            }
            pcx.PixelBlender = blender;

            return new AggPainter(pcx);
        }


        Region _rgn;
        public override Region CurrentRegion
        {
            get
            {
                return _rgn;
            }
            set
            {
                _rgn = value;
            }
        }
        public override void ExitCurrentSurface(ViewState state)
        {
            throw new NotImplementedException();
        }
        public override ViewState EnterNewSurface(RenderSurface backbuffer)
        {
            throw new NotImplementedException();
        }

    }

}