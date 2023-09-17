//MIT, 2014-present, WinterDev


namespace PixelFarm.Drawing
{
    public abstract class RenderSurface : System.IDisposable
    {
        
#if DEBUG
        public enum DbugBackBufferKind
        {
            General,
            WordPlate,
            VirtualScreen,
            DoubleBufferSurface
        }
        bool _isValid;
        public DbugBackBufferKind _dbugKind;

        public bool IsValid
        {
            get => _isValid;
            set
            {
                if (_isValid && !value)
                {
                    switch (_dbugKind)
                    {
                        case DbugBackBufferKind.WordPlate:
                            break;
                        case DbugBackBufferKind.DoubleBufferSurface:
                            break;
                    }
                }
                _isValid = value;
            }
        }
#else
        public bool IsValid { get; set; }
#endif

        public abstract Image GetImage();
        public abstract int Width { get; }
        public abstract int Height { get; }
        public abstract void Dispose();
        public abstract Image CopyToNewMemBitmap();
    }
}



