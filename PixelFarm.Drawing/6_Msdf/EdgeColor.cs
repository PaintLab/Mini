//MIT, 2016, Viktor Chlumsky, Multi-channel signed distance field generator, from https://github.com/Chlumsky/msdfgen
//MIT, 2017-present, WinterDev (C# port)
 
namespace Msdfgen
{
    /// <summary>
    /// Edge color specifies which color channels an edge belongs to.
    /// </summary>
    [System.Flags]
    public enum EdgeColor
    {
        BLACK = 0,
        RED = 1, //1<<0
        GREEN = 2, //1<<1
        YELLOW = 3, // RED| GREEN
        BLUE = 4,  //1<<2
        MAGENTA = 5, //RED| BLUE
        CYAN = 6, //GREEN |BLUE
        WHITE = 7
    }
}