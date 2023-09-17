//MIT, 2018-present, WinterDev
//----------------------------------------------------------------------------
// Anti-Grain Geometry - Version 2.3
// Copyright (C) 2002-2005 Maxim Shemanarev (http://www.antigrain.com)
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
//
// SVG parser.
//
//----------------------------------------------------------------------------
using System;
using System.Collections.Generic;

using LayoutFarm.WebDom.Parser;
using LayoutFarm.WebLexer;

namespace PaintLab.Svg
{

   


    public class SvgParser : XmlParserBase
    {

        ISvgDocBuilder _svgDocBuilder;
        string _currentElemName;

        public SvgParser(ISvgDocBuilder svgDocBuilder)
        {
            _svgDocBuilder = svgDocBuilder;
        }

        protected override void OnBegin()
        {
            _svgDocBuilder.OnBegin();
            base.OnBegin();
        }
        public void ParseSvg(string svgString)
        {
            ParseDocument(new TextSnapshot(svgString));
        }
        public void ParseSvg(char[] svgBuffer)
        {
            ParseDocument(new TextSnapshot(svgBuffer));
        }

        protected override void OnVisitNewElement(TextSpan ns, TextSpan localName)
        {
            string prefix = _textSnapshot.Substring(ns.startIndex, ns.len);
            _currentElemName = _textSnapshot.Substring(localName.startIndex, localName.len);
            _svgDocBuilder.OnVisitNewElement(prefix, _currentElemName);
        }
        protected override void OnVisitNewElement(TextSpan localName)
        {
            _currentElemName = _textSnapshot.Substring(localName.startIndex, localName.len);
            _svgDocBuilder.OnVisitNewElement(_currentElemName);
        }

        protected override void OnAttribute(TextSpan localAttr, TextSpan value)
        {
            string attrLocalName = _textSnapshot.Substring(localAttr.startIndex, localAttr.len);
            string attrValue = _textSnapshot.Substring(value.startIndex, value.len);

            _svgDocBuilder.OnAttribute(attrLocalName, attrValue);
        }
        protected override void OnAttribute(TextSpan ns, TextSpan localAttr, TextSpan value)
        {
            string attrPrefix = _textSnapshot.Substring(ns.startIndex, ns.len);
            string attrLocalName = _textSnapshot.Substring(localAttr.startIndex, localAttr.len);
            string attrValue = _textSnapshot.Substring(value.startIndex, value.len);
            _svgDocBuilder.OnAttribute(attrPrefix, attrLocalName, attrValue);

        }
        protected override void OnEnteringElementBody()
        {
            _svgDocBuilder.OnEnteringElementBody();
        }
        protected override void OnExitingElementBody()
        {
            _currentElemName = null;
            _svgDocBuilder.OnExitingElementBody();
        }
        protected override void OnTextNode(TextSpan text)
        {
            //not all text node that we focus
            if (_currentElemName == "text" || _currentElemName == "t")
            {
                _svgDocBuilder.OnTextNode(_textSnapshot.Substring(text.startIndex, text.len));
            }
        }
        
        public static void ParseTransform(string value, SvgVisualSpec spec)
        {
            //TODO: ....

            int openParPos = value.IndexOf('(');
            if (openParPos > -1)
            {
                string right = value.Substring(openParPos + 1, value.Length - (openParPos + 1)).Trim();
                string left = value.Substring(0, openParPos);
                switch (left)
                {
                    default:
                        break;
                    case "matrix":
                        {
                            //read matrix args  
                            spec.Transform = new SvgTransformMatrix(ParseMatrixArgs(right));
                        }
                        break;
                    case "translate":
                        {
                            //translate matrix
                            float[] matrixArgs = ParseMatrixArgs(right);
                            spec.Transform = new SvgTranslate(matrixArgs[0], matrixArgs[1]);
                        }
                        break;
                    case "rotate":
                        {
                            float[] matrixArgs = ParseMatrixArgs(right);
                            if (matrixArgs.Length == 1)
                            {
                                spec.Transform = new SvgRotate(matrixArgs[0]);
                            }
                            else if (matrixArgs.Length == 3)
                            {
                                //rotate around the axis
                                spec.Transform = new SvgRotate(matrixArgs[0], matrixArgs[1], matrixArgs[2]);
                            }

                        }
                        break;
                    case "scale":
                        {
                            float[] matrixArgs = ParseMatrixArgs(right);
                            spec.Transform = new SvgScale(matrixArgs[0], matrixArgs[1]);
                        }
                        break;
                    case "skewX":
                        {
                            float[] matrixArgs = ParseMatrixArgs(right);
                            spec.Transform = new SvgSkew(matrixArgs[0], 0);
                        }
                        break;
                    case "skewY":
                        {
                            float[] matrixArgs = ParseMatrixArgs(right);
                            spec.Transform = new SvgSkew(0, matrixArgs[1]);
                        }
                        break;
                }
            }
            else
            {
                //?
            }
        }

        static readonly char[] s_matrixStrSplitters = new char[] { ',', ' ' };
        static float[] ParseMatrixArgs(string matrixTransformArgs)
        {
            int close_paren = matrixTransformArgs.IndexOf(')');
            matrixTransformArgs = matrixTransformArgs.Substring(0, close_paren);
            string[] elem_string_args = matrixTransformArgs.Split(s_matrixStrSplitters);
            int j = elem_string_args.Length;
            float[] elem_values = new float[j];
            for (int i = 0; i < j; ++i)
            {
                elem_values[i] = float.Parse(elem_string_args[i], System.Globalization.CultureInfo.InvariantCulture);
            }
            return elem_values;
        }

    }

}