//BSD, 2014-present, WinterDev

using System;
using System.Collections.Generic;


namespace LayoutFarm.WebDom.Parser
{

    public class CssParser
    {
        CssLexer _lexer;
        char[] _textBuffer;
        CssParseState _parseState;
        CssDocument _cssDocument;
        Stack<CssAtMedia> _mediaStack = new Stack<CssAtMedia>();

        CssAtMedia _currentAtMedia;
        CssRuleSet _currentRuleSet;

        CssAttributeSelectorExpression _currentSelectorAttr;
        CssSimpleElementSelector _currentSelectorExpr;
        CssPropertyDeclaration _currentProperty;
        CssValueExpression _latestPropertyValue;

        public CssParser()
        {
            _lexer = new CssLexer(LexerEmitHandler);
        }

        public void ParseCssStyleSheet(char[] textBuffer)
        {
            _textBuffer = textBuffer;
            Reset();
            _parseState = CssParseState.Init;
            _lexer.Lex(textBuffer);
            //-----------------------------
            //expand some compound property             
            foreach (CssDocMember mb in _cssDocument.GetCssDocMemberIter())
            {
                switch (mb.MemberKind)
                {
                    case WebDom.CssDocMemberKind.RuleSet:
                    case WebDom.CssDocMemberKind.Page:
                        EvaluateRuleSet((WebDom.CssRuleSet)mb);
                        break;
                    case WebDom.CssDocMemberKind.Media:
                        EvaluateMedia((WebDom.CssAtMedia)mb);
                        break;
                    default:

                        throw new NotSupportedException();
                }
            }
            //-----------------------------
        }
        public CssRuleSet ParseCssPropertyDeclarationList(char[] textBuffer)
        {
            _textBuffer = textBuffer;
            Reset();
            //------------- 
            _currentRuleSet = new CssRuleSet();
            //-------------
            _parseState = CssParseState.BlockBody;
            _lexer.Lex(textBuffer);
            EvaluateRuleSet(_currentRuleSet);
            return _currentRuleSet;
        }

        void Reset()
        {
            _cssDocument = new CssDocument();
            _currentAtMedia = new CssAtMedia();
            _cssDocument.Add(_currentAtMedia);
            _mediaStack.Clear();
            _currentSelectorAttr = null;
            _currentSelectorExpr = null;
            _currentProperty = null;
            _latestPropertyValue = null;
        }

        void LexerEmitHandler(CssTokenName tkname, int start, int len)
        {
            if (tkname == CssTokenName.Whitespace) { return; }

            switch (_parseState)
            {
                default:
                    {
                        throw new NotSupportedException();
                    }
                case CssParseState.Init:
                    {
                        switch (tkname)
                        {
                            case CssTokenName.Comment:
                                {
                                    //comment token  
                                }
                                break;
                            case CssTokenName.RBrace:
                                {
                                    //exit from current ruleset block
                                    if (_mediaStack.Count > 0)
                                    {
                                        _currentAtMedia = _mediaStack.Pop();
                                    }
                                }
                                break;
                            case CssTokenName.Star:
                                {
                                    //start new code block 
                                    CssRuleSet newblock;
                                    _currentAtMedia.AddRuleSet(_currentRuleSet = newblock = new CssRuleSet());
                                    newblock.AddSelector(_currentSelectorExpr = new CssSimpleElementSelector(SimpleElementSelectorKind.All));
                                    _parseState = CssParseState.MoreBlockName;
                                }
                                break;
                            case CssTokenName.At:
                                {
                                    //at rule                                    
                                    _parseState = CssParseState.ExpectAtRuleName;
                                }
                                break;
                            //--------------------------------------------------
                            //1.
                            case CssTokenName.Colon:
                                {
                                    CssRuleSet newblock;
                                    _currentAtMedia.AddRuleSet(_currentRuleSet = newblock = new CssRuleSet());
                                    newblock.AddSelector(_currentSelectorExpr = new CssSimpleElementSelector(SimpleElementSelectorKind.PseudoClass));
                                    _parseState = CssParseState.ExpectIdenAfterSpecialBlockNameSymbol;
                                }
                                break;
                            //2.
                            case CssTokenName.Dot:
                                {
                                    CssRuleSet newblock;
                                    _currentAtMedia.AddRuleSet(_currentRuleSet = newblock = new CssRuleSet());
                                    newblock.AddSelector(_currentSelectorExpr = new CssSimpleElementSelector(SimpleElementSelectorKind.ClassName));
                                    _parseState = CssParseState.ExpectIdenAfterSpecialBlockNameSymbol;
                                }
                                break;
                            //3. 
                            case CssTokenName.DoubleColon:
                                {
                                    CssRuleSet newblock;
                                    _currentAtMedia.AddRuleSet(_currentRuleSet = newblock = new CssRuleSet());
                                    newblock.AddSelector(_currentSelectorExpr = new CssSimpleElementSelector(SimpleElementSelectorKind.Extend));
                                    _parseState = CssParseState.ExpectIdenAfterSpecialBlockNameSymbol;
                                }
                                break;
                            //4.
                            case CssTokenName.Iden:
                                {
                                    //block name
                                    CssRuleSet newblock;
                                    _currentAtMedia.AddRuleSet(_currentRuleSet = newblock = new CssRuleSet());
                                    newblock.AddSelector(_currentSelectorExpr = new CssSimpleElementSelector());
                                    _currentSelectorExpr.Name = new string(_textBuffer, start, len);
                                    _parseState = CssParseState.MoreBlockName;
                                }
                                break;
                            //5. 
                            case CssTokenName.Sharp:
                                {
                                    CssRuleSet newblock;
                                    _currentAtMedia.AddRuleSet(_currentRuleSet = newblock = new CssRuleSet());
                                    newblock.AddSelector(_currentSelectorExpr = new CssSimpleElementSelector(SimpleElementSelectorKind.Id));
                                    _parseState = CssParseState.ExpectIdenAfterSpecialBlockNameSymbol;
                                }
                                break;
                        }
                    }
                    break;
                case CssParseState.MoreBlockName:
                    {
                        //more 
                        switch (tkname)
                        {
                            case CssTokenName.LBrace:
                                {
                                    //block body
                                    _parseState = CssParseState.BlockBody;
                                }
                                break;
                            case CssTokenName.LBracket:
                                {
                                    //element attr
                                    _parseState = CssParseState.ExpectBlockAttrIden;
                                }
                                break;
                            //1. 
                            case CssTokenName.Colon:
                                {
                                    //wait iden after colon
                                    var cssSelector = new CssSimpleElementSelector();
                                    cssSelector._selectorType = SimpleElementSelectorKind.PseudoClass;
                                    _currentRuleSet.AddSelector(cssSelector);
                                    _currentSelectorExpr = cssSelector;
                                    _parseState = CssParseState.ExpectIdenAfterSpecialBlockNameSymbol;
                                }
                                break;
                            //2. 
                            case CssTokenName.Dot:
                                {
                                    var cssSelector = new CssSimpleElementSelector();
                                    cssSelector._selectorType = SimpleElementSelectorKind.ClassName;
                                    _currentRuleSet.AddSelector(cssSelector);
                                    _currentSelectorExpr = cssSelector;
                                    _parseState = CssParseState.ExpectIdenAfterSpecialBlockNameSymbol;
                                }
                                break;
                            //3. 
                            case CssTokenName.DoubleColon:
                                {
                                    var cssSelector = new CssSimpleElementSelector();
                                    cssSelector._selectorType = SimpleElementSelectorKind.Extend;
                                    _currentRuleSet.AddSelector(cssSelector);
                                    _currentSelectorExpr = cssSelector;
                                    _parseState = CssParseState.ExpectIdenAfterSpecialBlockNameSymbol;
                                }
                                break;
                            //4. 
                            case CssTokenName.Iden:
                                {
                                    //add more block name                                     
                                    var cssSelector = new CssSimpleElementSelector();
                                    cssSelector._selectorType = SimpleElementSelectorKind.TagName;
                                    cssSelector.Name = new string(_textBuffer, start, len);
                                    _currentRuleSet.AddSelector(cssSelector);
                                    _currentSelectorExpr = cssSelector;
                                }
                                break;
                            //5. 
                            case CssTokenName.Sharp:
                                {
                                    //id
                                    var cssSelector = new CssSimpleElementSelector();
                                    cssSelector._selectorType = SimpleElementSelectorKind.Id;
                                    _currentRuleSet.AddSelector(cssSelector);
                                    _currentSelectorExpr = cssSelector;
                                    _parseState = CssParseState.ExpectIdenAfterSpecialBlockNameSymbol;
                                }
                                break;
                            //----------------------------------------------------

                            //element combinator operators
                            case CssTokenName.Comma:
                                {
                                    _currentRuleSet.PrepareExpression(CssCombinatorOperator.List);
                                }
                                break;
                            case CssTokenName.Star:
                                {
                                    //TODO:
                                }
                                break;
                            case CssTokenName.RAngle:
                                {
                                    //TODO:
                                }
                                break;
                            case CssTokenName.Plus:
                                {//TODO:
                                }
                                break;
                            case CssTokenName.Tile:
                                {
                                    //TODO:
                                }
                                break;
                            case CssTokenName.Whitespace:
                                break;
                            default:
                                {
                                    throw new NotSupportedException();
                                }
                        }
                    }
                    break;
                case CssParseState.ExpectIdenAfterSpecialBlockNameSymbol:
                    {
                        switch (tkname)
                        {
                            case CssTokenName.Iden:
                                {
                                    _currentSelectorExpr.Name = new string(_textBuffer, start, len);
                                    _parseState = CssParseState.MoreBlockName;
                                }
                                break;
                            case CssTokenName.Number:
                                {
                                    string tt = new string(_textBuffer, start, len);
                                    _parseState = CssParseState.ExpectPropertyUnit;
                                }
                                break;
                            default:
                                {
                                    string tt = new string(_textBuffer, start, len);
                                    throw new NotSupportedException();
                                }
                        }
                    }
                    break;
                case CssParseState.ExpectPropertyUnit:
                    {
                        switch (tkname)
                        {
                            case CssTokenName.NumberUnit:
                                {
                                    //append number unit
                                    string tt = new string(_textBuffer, start, len);
                                    _parseState = CssParseState.ExpectPropertyValue;
                                }
                                break;
                            default:
                                {

                                }
                                break;
                        }
                    }
                    break;
                case CssParseState.ExpectBlockAttrIden:
                    {
                        switch (tkname)
                        {
                            case CssTokenName.Iden:
                                {
                                    //attribute  
                                    _parseState = CssParseState.AfterAttrName;
                                    _currentSelectorExpr.AddAttribute(_currentSelectorAttr = new CssAttributeSelectorExpression());
                                    _currentSelectorAttr.AttributeName = new string(_textBuffer, start, len);
                                }
                                break;
                            default:
                                {
                                    throw new NotSupportedException();
                                }
                                break;
                        }
                    }
                    break;
                case CssParseState.AfterAttrName:
                    {
                        switch (tkname)
                        {
                            case CssTokenName.OpEq:
                                {
                                    _parseState = CssParseState.ExpectedBlockAttrValue;
                                    //expected  attr value
                                }
                                break;
                            case CssTokenName.RBracket:
                                {
                                    //no attr value
                                    _parseState = CssParseState.MoreBlockName;
                                }
                                break;
                            default:
                                {
                                    throw new NotSupportedException();
                                }
                                break;
                        }
                    }
                    break;
                case CssParseState.ExpectedBlockAttrValue:
                    {
                        switch (tkname)
                        {
                            case CssTokenName.LiteralString:
                                {
                                    _currentSelectorAttr.valueExpression = _latestPropertyValue =
                                        new CssPrimitiveValueExpression(new string(_textBuffer, start, len), CssValueHint.LiteralString);
                                    _currentSelectorAttr = null;
                                }
                                break;
                            default:
                                {
                                }
                                break;
                        }
                        _parseState = CssParseState.AfterBlockNameAttr;
                    }
                    break;
                case CssParseState.AfterBlockNameAttr:
                    {
                        switch (tkname)
                        {
                            default:
                                {
                                }
                                break;
                            case CssTokenName.RBracket:
                                {
                                    _parseState = CssParseState.MoreBlockName;
                                    _currentSelectorAttr = null;
                                }
                                break;
                        }
                    }
                    break;
                case CssParseState.BlockBody:
                    {
                        switch (tkname)
                        {
                            case CssTokenName.Iden:
                                {
                                    //block name

                                    //create css property 
                                    string cssPropName = new string(_textBuffer, start, len).ToLower();
                                    LayoutFarm.WebDom.WellknownCssPropertyName wellknownName = UserMapUtil.GetWellKnownPropName(cssPropName);
                                    if (wellknownName == WellknownCssPropertyName.Unknown)
                                    {
                                        _currentRuleSet.AddCssCodeProperty(_currentProperty =
                                            new CssPropertyDeclaration(cssPropName));
                                    }
                                    else
                                    {
                                        _currentRuleSet.AddCssCodeProperty(_currentProperty =
                                            new CssPropertyDeclaration(wellknownName));
                                    }
                                    _latestPropertyValue = null;
                                    _parseState = CssParseState.AfterPropertyName;
                                }
                                break;
                            case CssTokenName.RBrace:
                                {
                                    //close current block
                                    _currentProperty = null;
                                    _currentSelectorAttr = null;
                                    _currentSelectorExpr = null;
                                    _parseState = CssParseState.Init;
                                }
                                break;
                            case CssTokenName.SemiColon:
                                {
                                    //another semi colon just skip
                                }
                                break;
                            default:
                                {
                                    throw new NotSupportedException();
                                }
                                break;
                        }
                    }
                    break;
                case CssParseState.AfterPropertyName:
                    {
                        if (tkname == CssTokenName.Colon)
                        {
                            _parseState = CssParseState.ExpectPropertyValue;
                        }

                        else
                        {
                            throw new NotSupportedException();
                        }
                    }
                    break;
                case CssParseState.ExpectPropertyValue:
                    {
                        switch (tkname)
                        {
                            default:
                                {
                                    throw new NotSupportedException();
                                }
                            case CssTokenName.Sharp:
                                {
                                    //follow by hex color value
                                    _parseState = CssParseState.ExpectValueOfHexColor;
                                }
                                break;
                            case CssTokenName.LiteralString:
                                {
                                    _currentProperty.AddValue(_latestPropertyValue =
                                         new CssPrimitiveValueExpression(new string(_textBuffer, start, len), CssValueHint.LiteralString));
                                    _parseState = CssParseState.AfterPropertyValue;
                                }
                                break;
                            case CssTokenName.Number:
                                {
                                    float number = float.Parse(new string(_textBuffer, start, len), System.Globalization.CultureInfo.InvariantCulture);
                                    _currentProperty.AddValue(_latestPropertyValue =
                                          new CssPrimitiveValueExpression(number));
                                    _parseState = CssParseState.AfterPropertyValue;
                                }
                                break;
                            case CssTokenName.NumberUnit:
                                {
                                }
                                break;
                            case CssTokenName.Iden:
                                {
                                    //property value
                                    _currentProperty.AddValue(_latestPropertyValue =
                                        new CssPrimitiveValueExpression(new string(_textBuffer, start, len), CssValueHint.Iden));
                                    _parseState = CssParseState.AfterPropertyValue;
                                }
                                break;
                        }
                    }
                    break;
                case CssParseState.ExpectValueOfHexColor:
                    {
                        switch (tkname)
                        {
                            case CssTokenName.Iden:
                                {
                                    _currentProperty.AddValue(_latestPropertyValue =
                                       new CssPrimitiveValueExpression("#" + new string(_textBuffer, start, len),
                                           CssValueHint.HexColor));
                                }
                                break;
                            case CssTokenName.Number:
                                {
                                    _currentProperty.AddValue(_latestPropertyValue =
                                         new CssPrimitiveValueExpression("#" + new string(_textBuffer, start, len),
                                             CssValueHint.HexColor));
                                }
                                break;
                            default:
                                {
                                    throw new NotSupportedException();
                                }
                        }
                        _parseState = CssParseState.AfterPropertyValue;
                    }
                    break;
                case CssParseState.AfterPropertyValue:
                    {
                        switch (tkname)
                        {
                            default:
                                {
                                    throw new NotSupportedException();
                                }
                            case CssTokenName.Comma:
                                {
                                    //skip comma
                                }
                                break;
                            case CssTokenName.Percent:
                                {
                                    if (_latestPropertyValue is CssPrimitiveValueExpression)
                                    {
                                        ((CssPrimitiveValueExpression)_latestPropertyValue).Unit = "%";
                                    }
                                }
                                break;
                            case CssTokenName.Divide:
                                {
                                    //eg. font: style variant weight size/line-height family;

                                    CssBinaryExpression codeBinaryOpExpr = new CssBinaryExpression();
                                    codeBinaryOpExpr.OpName = CssValueOpName.Divide;
                                    codeBinaryOpExpr.Left = _latestPropertyValue;
                                    //replace previous add value ***
                                    int valueCount = _currentProperty.ValueCount;
                                    //replace
                                    _currentProperty.ReplaceValue(valueCount - 1, codeBinaryOpExpr);
                                    _latestPropertyValue = codeBinaryOpExpr;
                                }
                                break;

                            case CssTokenName.LParen:
                                {
                                    //function 
                                    _parseState = CssParseState.ExpectedFuncParameter;
                                    //make current prop value as func
                                    CssFunctionCallExpression funcCallExpr = new CssFunctionCallExpression(
                                        _latestPropertyValue.ToString());
                                    int valueCount = _currentProperty.ValueCount;
                                    _currentProperty.ReplaceValue(valueCount - 1, funcCallExpr);
                                    _latestPropertyValue = funcCallExpr;
                                }
                                break;
                            case CssTokenName.RBrace:
                                {
                                    //close block
                                    _parseState = CssParseState.Init;
                                }
                                break;
                            case CssTokenName.SemiColon:
                                {
                                    //start new proeprty
                                    _parseState = CssParseState.BlockBody;
                                    _currentProperty = null;
                                }
                                break;
                            case CssTokenName.LiteralString:
                                {
                                    //TODO: review css
                                    //<p style="font: 20px '1 Smoothy DNA'">
                                    var literalValue = new string(_textBuffer, start, len);
                                    _currentProperty.AddValue(_latestPropertyValue =
                                        new CssPrimitiveValueExpression(literalValue, CssValueHint.LiteralString));
                                }
                                break;
                            case CssTokenName.Iden:
                                {
                                    //another property value                                     
                                    _currentProperty.AddValue(_latestPropertyValue =
                                        new CssPrimitiveValueExpression(new string(_textBuffer, start, len), CssValueHint.Iden));
                                }
                                break;
                            case CssTokenName.Number:
                                {
                                    //another property value
                                    float number = float.Parse(new string(_textBuffer, start, len), System.Globalization.CultureInfo.InvariantCulture);
                                    _currentProperty.AddValue(_latestPropertyValue =
                                        new CssPrimitiveValueExpression(number));
                                }
                                break;
                            case CssTokenName.NumberUnit:
                                {
                                    //number unit 
                                    if (_latestPropertyValue is CssPrimitiveValueExpression)
                                    {
                                        ((CssPrimitiveValueExpression)_latestPropertyValue).Unit = new string(_textBuffer, start, len);
                                    }
                                }
                                break;
                            case CssTokenName.Sharp:
                                {
                                    _parseState = CssParseState.ExpectValueOfHexColor;
                                }
                                break;
                        }
                    }
                    break;
                case CssParseState.ExpectAtRuleName:
                    {
                        //iden
                        switch (tkname)
                        {
                            default:
                                {
                                    throw new NotSupportedException();
                                }
                            case CssTokenName.Iden:
                                {
                                    string iden = new string(_textBuffer, start, len);
                                    //create new rule
                                    _currentRuleSet = null;
                                    _currentProperty = null;
                                    _currentSelectorAttr = null;
                                    _currentSelectorExpr = null;
                                    switch (iden)
                                    {
                                        case "page":
                                            _parseState = CssParseState.Page;
                                            break;
                                        case "media":
                                            {
                                                _parseState = CssParseState.MediaList;
                                                //store previous media 
                                                if (_currentAtMedia != null)
                                                {
                                                    _mediaStack.Push(_currentAtMedia);
                                                }

                                                //begin new media list
                                                _cssDocument.Add(_currentAtMedia = new CssAtMedia());
                                            }
                                            break;
                                        case "import":
                                            {
                                                _parseState = CssParseState.ExpectImportURL;
                                            }
                                            break;
                                        default:
                                            {
                                                throw new NotSupportedException();
                                            }
                                    }
                                }
                                break;
                        }
                    }
                    break;
                case CssParseState.MediaList:
                    {
                        //medialist sep by comma
                        switch (tkname)
                        {
                            default:
                                {
                                    throw new NotSupportedException();
                                }
                            case CssTokenName.Iden:
                                {
                                    //media name                                     
                                    _currentAtMedia.AddMedia(new string(_textBuffer, start, len));
                                }
                                break;
                            case CssTokenName.Comma:
                                {
                                    //wait for another media
                                }
                                break;
                            case CssTokenName.LBrace:
                                {
                                    //begin rule set part
                                    _parseState = CssParseState.Init;
                                }
                                break;
                        }
                    }
                    break;
                case CssParseState.Page:
                    {
                        switch (tkname)
                        {
                            case CssTokenName.LBrace:
                                {
                                    _parseState = CssParseState.BlockBody;
                                    _currentRuleSet = new CssAtPage();
                                    _cssDocument.Add(_currentRuleSet);
                                }
                                break;
                        }
                    }
                    break;
                case CssParseState.ExpectedFuncParameter:
                    {
                        string funcArg = new string(_textBuffer, start, len);
                        switch (tkname)
                        {
                            default:
                                {
                                    throw new NotSupportedException();
                                }
                            case CssTokenName.RParen:
                                {
                                    _parseState = CssParseState.AfterPropertyValue;
                                }
                                break;
                            case CssTokenName.LiteralString:
                                {
                                    ((CssFunctionCallExpression)_latestPropertyValue).AddFuncArg(
                                        new CssPrimitiveValueExpression(funcArg, CssValueHint.LiteralString));
                                    _parseState = CssParseState.AfterFuncParameter;
                                }
                                break;
                            case CssTokenName.Number:
                                {
                                    float number = float.Parse(funcArg, System.Globalization.CultureInfo.InvariantCulture);
                                    ((CssFunctionCallExpression)_latestPropertyValue).AddFuncArg(
                                          new CssPrimitiveValueExpression(number));
                                    _parseState = CssParseState.AfterFuncParameter;
                                }
                                break;
                            case CssTokenName.Iden:
                                {
                                    ((CssFunctionCallExpression)_latestPropertyValue).AddFuncArg(
                                        new CssPrimitiveValueExpression(funcArg, CssValueHint.Iden));
                                    _parseState = CssParseState.AfterFuncParameter;
                                }
                                break;
                        }
                    }
                    break;
                case CssParseState.AfterFuncParameter:
                    {
                        switch (tkname)
                        {
                            default:
                                {
                                    throw new NotSupportedException();
                                }
                            case CssTokenName.RParen:
                                {
                                    _parseState = CssParseState.AfterPropertyValue;
                                }
                                break;
                            case CssTokenName.Comma:
                                {
                                    _parseState = CssParseState.ExpectedFuncParameter;
                                }
                                break;
                        }
                    }
                    break;
            }
        }
        void ExpandFontProperty(CssPropertyDeclaration decl, List<CssPropertyDeclaration> newProps)
        {
            //may has more than one prop value
            int valCount = decl.ValueCount;
            for (int i = 0; i < valCount; ++i)
            {
                CssPrimitiveValueExpression value = decl.GetPropertyValue(i) as CssPrimitiveValueExpression;
                //what this prop mean 
                if (value == null)
                {
                    continue;
                }
                //------------------------------
                switch (value.Hint)
                {
                    case CssValueHint.Iden:
                        {
                            //font style
                            //font vairant
                            //font weight
                            //font named size         
                            //font family
                            if (UserMapUtil.IsFontStyle(value.Value))
                            {
                                newProps.Add(new CssPropertyDeclaration(WellknownCssPropertyName.FontStyle, value));
                                continue;
                            }

                            if (UserMapUtil.IsFontVariant(value.Value))
                            {
                                newProps.Add(new CssPropertyDeclaration(WellknownCssPropertyName.FontVariant, value));
                                continue;
                            }
                            //----------
                            if (UserMapUtil.IsFontWeight(value.Value))
                            {
                                newProps.Add(new CssPropertyDeclaration(WellknownCssPropertyName.FontWeight, value));
                                continue;
                            }
                            newProps.Add(new CssPropertyDeclaration(WellknownCssPropertyName.FontFamily, value));
                        }
                        break;
                    case CssValueHint.Number:
                        {
                            //font size ?
                            newProps.Add(new CssPropertyDeclaration(WellknownCssPropertyName.FontSize, value));
                        }
                        break;
                    default:
                        {
                        }
                        break;
                }
            }
        }

        void EvaluateRuleSet(WebDom.CssRuleSet ruleset)
        {
            //only some prop need to be alter
            List<CssPropertyDeclaration> newProps = null;
            foreach (CssPropertyDeclaration decl in ruleset.GetAssignmentIter())
            {
                switch (decl.WellknownPropertyName)
                {
                    case WellknownCssPropertyName.Font:
                        {
                            if (newProps == null) newProps = new List<CssPropertyDeclaration>();
                            ExpandFontProperty(decl, newProps);
                            decl.IsExpand = true;
                        }
                        break;
                    case WellknownCssPropertyName.Border:
                        {
                            if (newProps == null) newProps = new List<CssPropertyDeclaration>();
                            ExpandBorderProperty(decl, BorderDirection.All, newProps);
                            decl.IsExpand = true;
                        }
                        break;
                    case WellknownCssPropertyName.BorderLeft:
                        {
                            if (newProps == null) newProps = new List<CssPropertyDeclaration>();
                            ExpandBorderProperty(decl, BorderDirection.Left, newProps);
                            decl.IsExpand = true;
                        }
                        break;
                    case WellknownCssPropertyName.BorderRight:
                        {
                            if (newProps == null) newProps = new List<CssPropertyDeclaration>();
                            ExpandBorderProperty(decl, BorderDirection.Right, newProps);
                            decl.IsExpand = true;
                        }
                        break;
                    case WellknownCssPropertyName.BorderTop:
                        {
                            if (newProps == null) newProps = new List<CssPropertyDeclaration>();
                            ExpandBorderProperty(decl, BorderDirection.Top, newProps);
                            decl.IsExpand = true;
                        }
                        break;
                    case WellknownCssPropertyName.BorderBottom:
                        {
                            if (newProps == null) newProps = new List<CssPropertyDeclaration>();
                            ExpandBorderProperty(decl, BorderDirection.Bottom, newProps);
                            decl.IsExpand = true;
                        }
                        break;
                    //---------------------------
                    case WellknownCssPropertyName.BorderStyle:
                        {
                            if (newProps == null) newProps = new List<CssPropertyDeclaration>();
                            ExpandCssEdgeProperty(decl,
                                WellknownCssPropertyName.BorderLeftStyle,
                                WellknownCssPropertyName.BorderTopStyle,
                                WellknownCssPropertyName.BorderRightStyle,
                                WellknownCssPropertyName.BorderBottomStyle,
                                newProps);
                            decl.IsExpand = true;
                        }
                        break;
                    case WellknownCssPropertyName.BorderWidth:
                        {
                            if (newProps == null) newProps = new List<CssPropertyDeclaration>();
                            ExpandCssEdgeProperty(decl,
                                 WellknownCssPropertyName.BorderLeftWidth,
                                 WellknownCssPropertyName.BorderTopWidth,
                                 WellknownCssPropertyName.BorderRightWidth,
                                 WellknownCssPropertyName.BorderBottomWidth,
                                 newProps);
                            decl.IsExpand = true;
                        }
                        break;
                    case WellknownCssPropertyName.BorderColor:
                        {
                            if (newProps == null) newProps = new List<CssPropertyDeclaration>();
                            ExpandCssEdgeProperty(decl,
                                WellknownCssPropertyName.BorderLeftColor,
                                WellknownCssPropertyName.BorderTopColor,
                                WellknownCssPropertyName.BorderRightColor,
                                WellknownCssPropertyName.BorderBottomColor,
                                newProps);
                            decl.IsExpand = true;
                        }
                        break;
                    //---------------------------
                    case WellknownCssPropertyName.Margin:
                        {
                            if (newProps == null) newProps = new List<CssPropertyDeclaration>();
                            ExpandCssEdgeProperty(decl,
                                WellknownCssPropertyName.MarginLeft,
                                WellknownCssPropertyName.MarginTop,
                                WellknownCssPropertyName.MarginRight,
                                WellknownCssPropertyName.MarginBottom,
                                newProps);
                            decl.IsExpand = true;
                        }
                        break;
                    case WellknownCssPropertyName.Padding:
                        {
                            if (newProps == null) newProps = new List<CssPropertyDeclaration>();
                            ExpandCssEdgeProperty(decl,
                                WellknownCssPropertyName.PaddingLeft,
                                WellknownCssPropertyName.PaddingTop,
                                WellknownCssPropertyName.PaddingRight,
                                WellknownCssPropertyName.PaddingBottom,
                                newProps);
                            decl.IsExpand = true;
                        }
                        break;
                }
            }
            //--------------------
            //add new prop to ruleset
            if (newProps == null)
            {
                return;
            }
            //------------
            int newPropCount = newProps.Count;
            for (int i = 0; i < newPropCount; ++i)
            {
                //add new prop to ruleset
                ruleset.AddCssCodeProperty(newProps[i]);
            }
        }

        enum BorderDirection
        {
            All, Left, Right, Top, Bottom
        }

        void ExpandBorderProperty(CssPropertyDeclaration decl, BorderDirection borderDirection, List<CssPropertyDeclaration> newProps)
        {
            int j = decl.ValueCount;
            for (int i = 0; i < j; ++i)
            {
                CssPrimitiveValueExpression cssCodePropertyValue = decl.GetPropertyValue(i) as CssPrimitiveValueExpression;
                if (cssCodePropertyValue == null)
                {
                    continue;
                }
                //what value means ?
                //border width/ style / color
                if (cssCodePropertyValue.Hint == CssValueHint.Number ||
                     UserMapUtil.IsNamedBorderWidth(cssCodePropertyValue.Value))
                {
                    //border width
                    switch (borderDirection)
                    {
                        default:
                        case BorderDirection.All:
                            {
                                newProps.Add(CloneProp(WellknownCssPropertyName.BorderLeftWidth, cssCodePropertyValue));
                                newProps.Add(CloneProp(WellknownCssPropertyName.BorderTopWidth, cssCodePropertyValue));
                                newProps.Add(CloneProp(WellknownCssPropertyName.BorderRightWidth, cssCodePropertyValue));
                                newProps.Add(CloneProp(WellknownCssPropertyName.BorderBottomWidth, cssCodePropertyValue));
                            }
                            break;
                        case BorderDirection.Left:
                            {
                                newProps.Add(CloneProp(WellknownCssPropertyName.BorderLeftWidth, cssCodePropertyValue));
                            }
                            break;
                        case BorderDirection.Right:
                            {
                                newProps.Add(CloneProp(WellknownCssPropertyName.BorderRightWidth, cssCodePropertyValue));
                            }
                            break;
                        case BorderDirection.Top:
                            {
                                newProps.Add(CloneProp(WellknownCssPropertyName.BorderTopWidth, cssCodePropertyValue));
                            }
                            break;
                        case BorderDirection.Bottom:
                            {
                                newProps.Add(CloneProp(WellknownCssPropertyName.BorderBottomWidth, cssCodePropertyValue));
                            }
                            break;
                    }
                    continue;
                }

                //------
                if (UserMapUtil.IsBorderStyle(cssCodePropertyValue.Value))
                {
                    //border style
                    switch (borderDirection)
                    {
                        default:
                        case BorderDirection.All:
                            {
                                newProps.Add(CloneProp(WellknownCssPropertyName.BorderLeftStyle, cssCodePropertyValue));
                                newProps.Add(CloneProp(WellknownCssPropertyName.BorderRightStyle, cssCodePropertyValue));
                                newProps.Add(CloneProp(WellknownCssPropertyName.BorderTopStyle, cssCodePropertyValue));
                                newProps.Add(CloneProp(WellknownCssPropertyName.BorderBottomStyle, cssCodePropertyValue));
                            }
                            break;
                        case BorderDirection.Left:
                            {
                                newProps.Add(CloneProp(WellknownCssPropertyName.BorderLeftStyle, cssCodePropertyValue));
                            }
                            break;
                        case BorderDirection.Right:
                            {
                                newProps.Add(CloneProp(WellknownCssPropertyName.BorderRightStyle, cssCodePropertyValue));
                            }
                            break;
                        case BorderDirection.Top:
                            {
                                newProps.Add(CloneProp(WellknownCssPropertyName.BorderTopStyle, cssCodePropertyValue));
                            }
                            break;
                        case BorderDirection.Bottom:
                            {
                                newProps.Add(CloneProp(WellknownCssPropertyName.BorderBottomStyle, cssCodePropertyValue));
                            }
                            break;
                    }

                    continue;
                }

                string value = cssCodePropertyValue.ToString();
                if (value.StartsWith("#"))
                {
                    //expand border color
                    switch (borderDirection)
                    {
                        default:
                        case BorderDirection.All:
                            {
                                newProps.Add(CloneProp(WellknownCssPropertyName.BorderLeftColor, cssCodePropertyValue));
                                newProps.Add(CloneProp(WellknownCssPropertyName.BorderTopColor, cssCodePropertyValue));
                                newProps.Add(CloneProp(WellknownCssPropertyName.BorderRightColor, cssCodePropertyValue));
                                newProps.Add(CloneProp(WellknownCssPropertyName.BorderBottomColor, cssCodePropertyValue));
                            }
                            break;
                        case BorderDirection.Left:
                            {
                                newProps.Add(CloneProp(WellknownCssPropertyName.BorderLeftColor, cssCodePropertyValue));
                            }
                            break;
                        case BorderDirection.Right:
                            {
                                newProps.Add(CloneProp(WellknownCssPropertyName.BorderRightColor, cssCodePropertyValue));
                            }
                            break;
                        case BorderDirection.Top:
                            {
                                newProps.Add(CloneProp(WellknownCssPropertyName.BorderTopColor, cssCodePropertyValue));
                            }
                            break;
                        case BorderDirection.Bottom:
                            {
                                newProps.Add(CloneProp(WellknownCssPropertyName.BorderBottomColor, cssCodePropertyValue));
                            }
                            break;
                    }

                    continue;
                }
            }
        }

        static CssPropertyDeclaration CloneProp(WellknownCssPropertyName newName, CssValueExpression prop)
        {
            return new CssPropertyDeclaration(newName, prop);
        }

        void ExpandCssEdgeProperty(CssPropertyDeclaration decl,
            WellknownCssPropertyName left,
            WellknownCssPropertyName top,
            WellknownCssPropertyName right,
            WellknownCssPropertyName bottom,
            List<CssPropertyDeclaration> newProps)
        {
            switch (decl.ValueCount)
            {
                case 0:
                    {
                    }
                    break;
                case 1:
                    {
                        CssValueExpression value = decl.GetPropertyValue(0);
                        newProps.Add(CloneProp(left, value));
                        newProps.Add(CloneProp(top, value));
                        newProps.Add(CloneProp(right, value));
                        newProps.Add(CloneProp(bottom, value));
                    }
                    break;
                case 2:
                    {
                        newProps.Add(CloneProp(top, decl.GetPropertyValue(0)));
                        newProps.Add(CloneProp(bottom, decl.GetPropertyValue(0)));
                        newProps.Add(CloneProp(left, decl.GetPropertyValue(1)));
                        newProps.Add(CloneProp(right, decl.GetPropertyValue(1)));
                    }
                    break;
                case 3:
                    {
                        newProps.Add(CloneProp(top, decl.GetPropertyValue(0)));
                        newProps.Add(CloneProp(left, decl.GetPropertyValue(1)));
                        newProps.Add(CloneProp(right, decl.GetPropertyValue(1)));
                        newProps.Add(CloneProp(bottom, decl.GetPropertyValue(2)));
                    }
                    break;
                default://4 or more
                    {
                        newProps.Add(CloneProp(top, decl.GetPropertyValue(0)));
                        newProps.Add(CloneProp(right, decl.GetPropertyValue(1)));
                        newProps.Add(CloneProp(bottom, decl.GetPropertyValue(2)));
                        newProps.Add(CloneProp(left, decl.GetPropertyValue(3)));
                    }
                    break;
            }
        }
        void EvaluateMedia(WebDom.CssAtMedia atMedia)
        {
            foreach (var ruleset in atMedia.GetRuleSetIter())
            {
                EvaluateRuleSet(ruleset);
            }
        }
        public CssDocument OutputCssDocument => _cssDocument;
    }
}