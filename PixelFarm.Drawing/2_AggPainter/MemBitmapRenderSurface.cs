//MIT, 2014-present, WinterDev
using System;
using PixelFarm.Drawing;
namespace PixelFarm.CpuBlit
{
    public class MemBitmapRenderSurface : RenderSurface
    {
        MemBitmap _membitmap;
        readonly bool _isMemBitmapOwner;
        public MemBitmapRenderSurface(MemBitmap membitmap, bool isMemBitmapOwner = false)
        {
            _isMemBitmapOwner = isMemBitmapOwner;
            _membitmap = membitmap;
        }
        public override int Width => _membitmap.Width;
        public override int Height => _membitmap.Height;
        public override Image CopyToNewMemBitmap() => MemBitmap.CreateFromCopy(_membitmap);
        public override void Dispose()
        {
            if (_isMemBitmapOwner && _membitmap != null)
            {
                _membitmap.Dispose();
                _membitmap = null;
            }
        }
        public override Image GetImage() => _membitmap;
    }

}