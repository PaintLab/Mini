//MIT, 2014-present, WinterDev
namespace PixelFarm.Drawing
{  
    /// <summary>
   /// image filter
   /// </summary>
    public interface IImageFilter
    {
        //implementation for cpu-based and gpu-based may be different
        void Apply();
        float[] ColorMatrix { get; set; }
    }

}