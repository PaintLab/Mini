//BSD, 2014-present, WinterDev
//----------------------------------------------------------------------------
// Anti-Grain Geometry - Version 2.4
// Copyright (C) 2002-2005 Maxim Shemanarev (http://www.antigrain.com)
//
// C# Port port by: Lars Brubaker
//                  larsbrubaker@gmail.com
// Copyright (C) 2007
//
// Permission to copy, use, modify, sell and distribute this software 
// is granted provided this copyright notice appears in all copies. 
// This software is provided "as is" without express or implied
// warranty, and with no claim as to its suitability for any purpose.
//
//----------------------------------------------------------------------------
// Contact: mcseem@antigrain.com
//          mcseemagg@yahoo.com
//          http://www.antigrain.com
//----------------------------------------------------------------------------

using System;
using PixelFarm.CpuBlit.FragmentProcessing;
namespace PixelFarm.CpuBlit.PixelProcessing
{
    public readonly struct GammaNone : IGammaFunction
    {
        public float GetGamma(float x) => x;
    }

    //==============================================================gamma_power
    public class GammaPower : IGammaFunction
    {
        float _gamma;
        public GammaPower() => _gamma = 1.0f;
        public GammaPower(float g) => _gamma = g;

        public void gamma(float g) => _gamma = g;
        public float gamma() => _gamma;

        public float GetGamma(float x) => (float)Math.Pow(x, _gamma);
    }

    //==========================================================gamma_threshold
    public class GammaThreshold : IGammaFunction
    {
        float _threshold;
        public GammaThreshold() => _threshold = 0.5f;
        public GammaThreshold(float t) => _threshold = t;

        public void threshold(float t) => _threshold = t;
        public float threshold() => _threshold;

        public float GetGamma(float x) => (x < _threshold) ? 0.0f : 1.0f;
    }

    //============================================================gamma_linear
    public class GammaLinear : IGammaFunction
    {
        float _start;
        float _end;
        public GammaLinear()
        {
            _start = 0.0f;
            _end = 1.0f;
        }
        public GammaLinear(float s, float e)
        {
            _start = (s);
            _end = (e);
        }
        public float Start => _start;
        public float End => _end;
        public void Set(float s, float e) { _start = s; _end = e; }

        public float GetGamma(float x)
        {
            if (x < _start) return 0.0f;
            if (x > _end) return 1.0f;

            double endMinusStart = _end - _start;

            return (endMinusStart != 0) ? (float)((x - _start) / endMinusStart) : 0f;
        }
    }

    //==========================================================gamma_multiply
    public class GammaMultiply : IGammaFunction
    {
        float _mul;
        public GammaMultiply() => _mul = 1.0f;
        public GammaMultiply(float v) => _mul = v;

        public float GetGamma(float x)
        {
            float y = x * _mul;
            if (y > 1.0) y = 1.0f;
            return y;
        }
    }


    public class GammaSigmoid : IGammaFunction
    {

        //This method SCurve calculates the y-value for a given x - value using the logistic function.
        //Adjust the xMidpoint to move the S - curve along the x - axis and change the steepness to adjust how pronounced the curve is.The steeper it is,
        //the quicker it transitions from the low part of the S to the high part.  

        /// <summary>
        /// Calculate the y-value for the given x using an S-curve function.
        /// </summary>
        /// <param name="x">The x-value. Expected to be between 0 and 255.</param>
        /// <param name="xMidpoint">The x-value of the midpoint of the curve. Typically between 0 and 255.</param>
        /// <param name="steepness">The steepness of the curve. Positive values make steeper curves.</param>
        /// <returns>The y-value of the curve, between 0 and 1.</returns>


        double xMidPoint;
        double steepness;
        double L;
        double maxX;
        public GammaSigmoid(double xMidPoint, double steepness, double L, double maxX)
        {
            this.L = L;
            this.steepness = steepness;
            this.xMidPoint = xMidPoint;
            this.maxX = maxX;

        }
        public float GetGamma(float x)
        {
            // Logistic function
            float value = (float)(L / (1.0 + Math.Exp(-steepness * (x - xMidPoint) / maxX)));
            if (value > 1.0)
            {
                return 1;
            }
            else if (value < 0)
            {
                return 0;
            }
            else
            {
                return value;
            }
        }
    }



}