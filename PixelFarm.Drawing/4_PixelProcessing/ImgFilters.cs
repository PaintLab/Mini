using System;
using System.Collections.Generic;
namespace PixelFarm
{

    public static class ImageFilters
    {
        static readonly Dictionary<string, Drawing.IImageFilter> s_imgFilters = new Dictionary<string, Drawing.IImageFilter>();
        public static Drawing.IImageFilter GetImageFilterByName(string filterName)
        {
            if (s_imgFilters.TryGetValue(filterName, out Drawing.IImageFilter worker))
            {
                return worker;
            }
            return null;
        }
    }

}