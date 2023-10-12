//MIT, 2014-present, WinterDev
namespace PixelFarm.Drawing
{
    public enum TextDrawingTech : byte
    {
        Stencil,//default
        LcdSubPix,
        Copy,
    }
   
    public enum SmoothingMode
    {
        AntiAlias = 4,
        Default = 0,
        HighQuality = 2,
        HighSpeed = 1,
        Invalid = -1,
        None = 3
    }
    public enum RenderSurfaceOriginKind
    {
        LeftTop,
        LeftBottom,
    }
    public enum CanvasBackEnd
    {
        Software,
        Hardware,
        HardwareWithSoftwareFallback
    }

    public enum TargetBuffer
    {
        ColorBuffer,
        MaskBuffer
    }
    public enum LineCap
    {
        Butt,
        Square,
        Round
    }

    public enum LineJoin
    {
        Miter,
        MiterRevert,
        Round,
        Bevel,
        MiterRound

        //TODO: implement svg arg join
    }

    public enum InnerJoin
    {
        Bevel,
        Miter,
        Jag,
        Round
    }
}