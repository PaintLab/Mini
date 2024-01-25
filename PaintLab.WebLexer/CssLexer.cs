//BSD, 2014-present, WinterDev

using System;
using System.Collections.Generic;


namespace LayoutFarm.WebDom.Parser
{
    public delegate void CssLexerEmitHandler(CssTokenName tkname, int startIndex, int len);

    /// <summary>
    /// event driven parser
    /// </summary>
    public class CssLexer
    {
        readonly CssLexerEmitHandler _emitHandler;
        public CssLexer(CssLexerEmitHandler emitHandler)
        {
            _emitHandler = emitHandler;
        }

#if DEBUG
        char[] _cssSourceBuffer;
#endif
        void Emit(int start, int len, CssTokenName tk)
        {
            _emitHandler(tk, start, len);
        }
        public void Lex(char[] cssSourceBuffer)
        {

#if DEBUG
            _cssSourceBuffer = cssSourceBuffer;
#endif

            int j = cssSourceBuffer.Length;
            for (int i = 0; i < j;)
            {
                char c = cssSourceBuffer[i];
                CssTokenName terminalTokenName = GetTerminalTokenName(c);
                switch (terminalTokenName)
                {
                    default:
                        throw new NotSupportedException();
                    //single token
                    case CssTokenName.RAngle:
                        {
                            //>, >=
                            if (i < j - 1)
                            {
                                //look ahead
                                char c2 = cssSourceBuffer[i + 1];
                                if (c2 == '=')
                                {
                                    //emit as double colon
                                    i += 2;
                                    Emit(i, 2, CssTokenName.GreaterOrEqual);
                                }
                                else
                                {
                                    Emit(i, 1, CssTokenName.RAngle);
                                    i++;
                                }
                            }
                            else
                            {
                                //emit
                                //last one
                                Emit(i, 1, CssTokenName.RAngle);
                                i++;
                            }
                        }
                        break;
                    case CssTokenName.LAngle: //<
                        {
                            //<
                            //<=
                            if (i < j - 1)
                            {
                                //look ahead
                                char c2 = cssSourceBuffer[i + 1];
                                if (c2 == '=')
                                {
                                    //emit as double colon
                                    i += 2;
                                    Emit(i, 2, CssTokenName.LessorOrEqual);
                                }
                                else
                                {
                                    Emit(i, 1, CssTokenName.LAngle);
                                    i++;
                                }
                            }
                            else
                            {
                                //emit
                                //last one
                                Emit(i, 1, CssTokenName.LAngle);
                                i++;
                            }

                        }
                        break;
                    case CssTokenName.Tile:
                    case CssTokenName.Sharp:
                    case CssTokenName.At:
                    case CssTokenName.LBrace:
                    case CssTokenName.RBrace:
                    case CssTokenName.LBracket:
                    case CssTokenName.RBracket:
                    case CssTokenName.LParen:
                    case CssTokenName.RParen:
                    case CssTokenName.OpEq:
                    case CssTokenName.SemiColon:
                    case CssTokenName.Comma:
                    case CssTokenName.Plus:
                    case CssTokenName.Star:
                        {
                            //emit
                            Emit(i, 1, terminalTokenName);
                            i += 1;
                        }
                        break;
                    case CssTokenName.Colon:
                        {
                            //single colon or double colon
                            if (i < j - 1)
                            {
                                //look ahead
                                char c2 = cssSourceBuffer[i + 1];
                                if (c2 == ':')
                                {
                                    //emit as double colon
                                    i += 2;
                                    Emit(i, 2, CssTokenName.DoubleColon);
                                }
                                else
                                {
                                    Emit(i, 1, CssTokenName.Colon);
                                    i++;
                                }
                            }
                            else
                            {
                                //emit
                                //last one
                                Emit(i, 1, CssTokenName.Colon);
                                i++;
                            }
                        }
                        break;
                    case CssTokenName.Dot:
                        {
                            //after dot may be iden or number
                            //we look head
                            if (i < j - 1)
                            {
                                char c2 = cssSourceBuffer[i + 1];
                                if (char.IsNumber(c2))
                                {
                                    //parse as number
                                    i++;
                                    ReadIntNumberLiteral(cssSourceBuffer, i, out int len);
                                    Emit(i - 1, len + 1, CssTokenName.Number);
                                    i += len;

                                    //number may has concat number unit after the number value
                                    if (i < j - 1)
                                    {
                                        char c3 = cssSourceBuffer[i];
                                        if (char.IsLetter(c3))
                                        {
                                            ReadIden(cssSourceBuffer, i, out int len2);
                                            //emit numbder with unit
                                            Emit(i, len2, CssTokenName.NumberUnit);
                                            i += len2;
                                        }
                                    }
                                }
                                else
                                {
                                    Emit(i, 1, CssTokenName.Dot);
                                    i++;
                                }
                            }
                            else
                            {
                                //end 
                                //Emit
                                Emit(i, 1, CssTokenName.Dot);
                                i++;
                            }
                        }
                        break;
                    case CssTokenName.Quote:
                        {
                            ReadStringLiteral(cssSourceBuffer, '\'', i, out int len);
                            Emit(i - 1, len, CssTokenName.LiteralString);

                            i += len;
                            //Emit
                        }
                        break;
                    case CssTokenName.DoubleQuote:
                        {
                            //collect string literal
                            ReadStringLiteral(cssSourceBuffer, '"', i, out int len);
                            Emit(i - 1, len, CssTokenName.LiteralString);
                            i += len;
                            //Emit
                        }
                        break;
                    case CssTokenName.Newline:
                    case CssTokenName.Whitespace:
                        {
                            ReadWhtiespace(cssSourceBuffer, i, out int len);
                            Emit(i, len, CssTokenName.Whitespace);
                            i += len;
                            //Emit 
                        }
                        break;
                    case CssTokenName.Unknown:
                        {
                            //read iden include number
                            ReadIden(cssSourceBuffer, i, out int len);
                            Emit(i, len, CssTokenName.Iden);
                            i += len;
                            //emit iden
                        }
                        break;
                    case CssTokenName.Minus:
                        {
                            //read iden include number
                            //after minus
                            if (i < j - 1)
                            {
                                char c2 = cssSourceBuffer[i + 1];
                                if (c2 == '-' || char.IsLetter(c2) || c2 == '_')
                                {
                                    ReadIden(cssSourceBuffer, i, out int len);
                                    Emit(i, len, CssTokenName.Iden);
                                    i += len;
                                }
                                else
                                {
                                    //read as minus
                                    Emit(i, 1, CssTokenName.Iden);
                                    i++;
                                }
                            }
                            else
                            {
                                Emit(i, 1, CssTokenName.Iden);
                                i++;
                            }

                            //emit iden
                        }
                        break;
                }
            }
        }
        static void ReadIden(char[] buffer, int startAt, out int len)
        {
            len = 0;
            for (int i = startAt; i < buffer.Length; ++i)
            {
                char c = buffer[i];
                if (char.IsLetterOrDigit(c) || c == '-' || c == '_')
                {
                    //collect
                    len++;
                }
                else
                {
                    break;
                }
            }
        }
        static void ReadWhtiespace(char[] buffer, int startAt, out int len)
        {
            len = 0;
            for (int i = startAt; i < buffer.Length; ++i)
            {
                char c = buffer[i];
                if (char.IsWhiteSpace(c))
                {
                    //collect
                    len++;
                }
                else
                {
                    break;
                }
            }
            //read until end?

        }
        static void ReadIntNumberLiteral(char[] buffer, int startAt, out int len)
        {
            len = 0;
            for (int i = startAt; i < buffer.Length; ++i) //**+1
            {
                char c = buffer[i];
                if (char.IsNumber(c))
                {
                    len++;
                }
                else
                {
                    break;
                }
            }
        }
        static void ReadStringLiteral(char[] buffer, char escapeWith, int startAt, out int len)
        {
            len = 1;
            for (int i = startAt + 1; i < buffer.Length; ++i) //**+1
            {
                char c = buffer[i];
                if (c == '\\')
                {

                }

                if (c == escapeWith)
                {
                    //break here
                    len++;
                    break;
                }
                else
                {
                    len++;
                }
            }
        }

        static CssTokenName GetTerminalTokenName(char c)
        {
            if (s_terminals.TryGetValue(c, out CssTokenName tokenName))
            {
                return tokenName;
            }
            else
            {
                return CssTokenName.Unknown;
            }
        }

        //===============================================================================================
        static readonly Dictionary<char, CssTokenName> s_terminals = new Dictionary<char, CssTokenName>();
        static readonly Dictionary<string, CssTokenName> s_multiCharTokens = new Dictionary<string, CssTokenName>();
        static CssLexer()
        {
            //" @+-*/%.:;[](){}"
            s_terminals.Add(' ', CssTokenName.Whitespace);
            s_terminals.Add('\r', CssTokenName.Whitespace);
            s_terminals.Add('\t', CssTokenName.Whitespace);
            s_terminals.Add('\f', CssTokenName.Whitespace);
            s_terminals.Add('\n', CssTokenName.Newline);
            s_terminals.Add('\'', CssTokenName.Quote);
            s_terminals.Add('"', CssTokenName.DoubleQuote);
            s_terminals.Add(',', CssTokenName.Comma);
            s_terminals.Add('@', CssTokenName.At);
            s_terminals.Add('+', CssTokenName.Plus);
            s_terminals.Add('-', CssTokenName.Minus);
            s_terminals.Add('*', CssTokenName.Star);
            s_terminals.Add('/', CssTokenName.Divide);
            s_terminals.Add('%', CssTokenName.Percent);
            s_terminals.Add('#', CssTokenName.Sharp);
            s_terminals.Add('~', CssTokenName.Tile);
            s_terminals.Add('.', CssTokenName.Dot);
            s_terminals.Add(':', CssTokenName.Colon);
            s_terminals.Add(';', CssTokenName.SemiColon);
            s_terminals.Add('[', CssTokenName.LBracket);
            s_terminals.Add(']', CssTokenName.RBracket);
            s_terminals.Add('(', CssTokenName.LParen);
            s_terminals.Add(')', CssTokenName.RParen);
            s_terminals.Add('{', CssTokenName.LBrace);
            s_terminals.Add('}', CssTokenName.RBrace);
            s_terminals.Add('<', CssTokenName.LAngle);
            s_terminals.Add('>', CssTokenName.RAngle);
            s_terminals.Add('=', CssTokenName.OpEq);
            s_terminals.Add('|', CssTokenName.OrPipe);
            s_terminals.Add('$', CssTokenName.Dollar);
            s_terminals.Add('^', CssTokenName.Cap);
            //----------------------------------- 
            s_multiCharTokens.Add("|=", CssTokenName.OrPipeAssign);
            s_multiCharTokens.Add("~=", CssTokenName.TileAssign);
            s_multiCharTokens.Add("^=", CssTokenName.CapAssign);
            s_multiCharTokens.Add("$=", CssTokenName.DollarAssign);
            s_multiCharTokens.Add("*=", CssTokenName.StarAssign);
            //----------------------------------- 
        }
    }



    public enum CssTokenName
    {
        Unknown,
        Newline,
        Whitespace,
        At,
        Comma,
        Plus, //+
        Minus,//-
        Star,//*
        Divide,// /
        Percent,// %
        Dot, // .
        Colon, // :
        Cap, //^
        OpEq,//=
        Dollar,//$
        Tile, //~
        SemiColon,
        Sharp, //#
        OrPipe, //|
        LParen,
        RParen,
        LBracket,
        RBracket,
        LBrace,
        RBrace,
        LAngle, //<
        RAngle,  //>

        LessorOrEqual, //<=
        GreaterOrEqual, //>=

        Iden,
        Number,
        NumberUnit,
        LiteralString,
        Comment,
        Quote, //  '
        DoubleQuote,  // "
        //------------------
        DoubleColon, //::
        TileAssign, //~=
        StarAssign,//*=
        CapAssign,//^=
        DollarAssign,//$=  
        OrPipeAssign,//|= 
        //------------------


    }
    public enum CssParseState
    {
        Init,
        MoreBlockName,
        ExpectIdenAfterSpecialBlockNameSymbol,
        BlockBody,
        AfterPropertyName,
        ExpectPropertyValue,
        ExpectPropertyUnit,
        ExpectValueOfHexColor,
        AfterPropertyValue,
        Comment,
        ExpectBlockAttrIden,
        AfterBlockAttrIden,
        AfterAttrName,
        ExpectedBlockAttrValue,
        AfterBlockNameAttr,
        ExpectAtRuleName,
        //@media
        MediaList,
        //@import
        ExpectImportURL,
        ExpectedFuncParameter,
        AfterFuncParameter,
        Page,

    }
}