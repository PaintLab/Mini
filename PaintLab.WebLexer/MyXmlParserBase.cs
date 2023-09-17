//MIT, 2018-present, WinterDev

using System;
using System.Collections.Generic;
using LayoutFarm.WebLexer;

namespace LayoutFarm.WebDom.Parser
{   
    public abstract class XmlParserBase
    {
        int _parseState = 0;
        protected TextSnapshot _textSnapshot;
        MyXmlLexer _myXmlLexer = new MyXmlLexer();
        string _waitingAttrName;
        string _currentNodeName;
        Stack<string> _openEltStack = new Stack<string>();

        TextSpan _nodeNamePrefix;
        bool _hasNodeNamePrefix;

        TextSpan _attrName;
        TextSpan _attrPrefix;
        bool _hasAttrPrefix;

        protected struct TextSpan
        {
            public readonly int startIndex;
            public readonly int len;
            public TextSpan(int startIndex, int len)
            {
                this.startIndex = startIndex;
                this.len = len;
            }
#if DEBUG
            public override string ToString()
            {
                return startIndex + "," + len;
            }
#endif
            public static readonly TextSpan Empty = new TextSpan();
        }


        public XmlParserBase()
        {
            _myXmlLexer.LexStateChanged += MyXmlLexer_LexStateChanged;
        }

        void MyXmlLexer_LexStateChanged(XmlLexerEvent lexEvent, int startIndex, int len)
        {

            switch (lexEvent)
            {
                default:
                    {
                        throw new NotSupportedException();
                    }
                case XmlLexerEvent.VisitOpenAngle:
                    {
                        //enter new context
                    }
                    break;
                case XmlLexerEvent.CommentContent:
                    {

                    }
                    break;
                case XmlLexerEvent.NamePrefix:
                    {
                        //name prefix of 

#if DEBUG
                        string testStr = _textSnapshot.Substring(startIndex, len);
#endif

                        switch (_parseState)
                        {
                            default:
                                throw new NotSupportedException();
                            case 0:
                                _nodeNamePrefix = new TextSpan(startIndex, len);
                                _hasNodeNamePrefix = true;
                                break;
                            case 1:
                                //attribute part
                                _attrPrefix = new TextSpan(startIndex, len);
                                _hasAttrPrefix = true;
                                break;
                            case 2: //   </a
                                _nodeNamePrefix = new TextSpan(startIndex, len);
                                _hasNodeNamePrefix = true;
                                break;
                        }
                    }
                    break;
                case XmlLexerEvent.FromContentPart:
                    {

                        //text content of the element 
                        OnTextNode(new TextSpan(startIndex, len));
                    }
                    break;
                case XmlLexerEvent.AttributeValueAsLiteralString:
                    {
                        //assign value and add to parent
                        //string attrValue = textSnapshot.Substring(startIndex, len);
                        if (_parseState == 11)
                        {
                            //doctype node
                            //add to its parameter
                        }
                        else
                        {
                            //add value to current attribute node
                            _parseState = 1;
                            if (_hasAttrPrefix)
                            {
                                OnAttribute(_attrPrefix, _attrName, new TextSpan(startIndex, len));
                                _hasAttrPrefix = false;
                            }
                            else
                            {
                                OnAttribute(_attrName, new TextSpan(startIndex, len));
                            }

                        }
                    }
                    break;
                case XmlLexerEvent.Attribute:
                    {
                        //create attribute node and wait for its value
                        _attrName = new TextSpan(startIndex, len);
                        //string attrName = textSnapshot.Substring(startIndex, len);
                    }
                    break;
                case XmlLexerEvent.NodeNameOrAttribute:
                    {
                        //the lexer dose not store state of element name or attribute name
                        //so we use parseState to decide here

                        string name = _textSnapshot.Substring(startIndex, len);
                        switch (_parseState)
                        {
                            case 0:
                                {
                                    //element name=> create element 
                                    if (_currentNodeName != null)
                                    {
                                        OnEnteringElementBody();
                                        _openEltStack.Push(_currentNodeName);
                                    }

                                    _currentNodeName = name;
                                    //enter new node
                                    if (_hasNodeNamePrefix)
                                    {
                                        OnVisitNewElement(_nodeNamePrefix, new TextSpan(startIndex, len));
                                        _hasNodeNamePrefix = false;
                                    }
                                    else
                                    {
                                        OnVisitNewElement(new TextSpan(startIndex, len));
                                    }


                                    _parseState = 1; //enter attribute 
                                    _waitingAttrName = null;
                                }
                                break;
                            case 1:
                                {
                                    //wait for attr value 
                                    if (_waitingAttrName != null)
                                    {
                                        //push waiting attr
                                        //create new attribute

                                        //eg. in html
                                        //but this is not valid in Xml

                                        throw new NotSupportedException();
                                    }
                                    _waitingAttrName = name;
                                }
                                break;
                            case 2:
                                {
                                    //****
                                    //node name after open slash  </
                                    //TODO: review here,avoid direct string comparison
                                    if (_currentNodeName == name)
                                    {
                                        OnExitingElementBody();

                                        if (_openEltStack.Count > 0)
                                        {
                                            _waitingAttrName = null;
                                            _currentNodeName = _openEltStack.Pop();
                                        }
                                        _parseState = 3;
                                    }
                                    else
                                    {
                                        //eg. in html
                                        //but this is not valid in Xml
                                        //not match open-close tag
                                        throw new NotSupportedException();
                                    }
                                }
                                break;
                            case 4:
                                {
                                    //attribute value as id ***
                                    //eg. in Html, but not for general Xml
                                    throw new NotSupportedException();
                                }

                            case 10:
                                {
                                    //eg <! 
                                    _parseState = 11;
                                }
                                break;
                            case 11:
                                {
                                    //comment node

                                }
                                break;
                            default:
                                {
                                }
                                break;
                        }
                    }
                    break;
                case XmlLexerEvent.VisitCloseAngle:
                    {
                        //close angle of current new node
                        //enter into its content 
                        if (_parseState == 11)
                        {
                            //add doctype to html 
                        }
                        else
                        {

                        }
                        _waitingAttrName = null;
                        _parseState = 0;
                    }
                    break;
                case XmlLexerEvent.VisitAttrAssign:
                    {

                        _parseState = 4;
                    }
                    break;
                case XmlLexerEvent.VisitOpenSlashAngle:
                    {
                        _parseState = 2;
                    }
                    break;
                case XmlLexerEvent.VisitCloseSlashAngle:
                    {
                        //   />
                        if (_openEltStack.Count > 0)
                        {
                            OnExitingElementBody();
                            //curTextNode = null;
                            //curAttr = null;
                            _waitingAttrName = null;
                            _currentNodeName = _openEltStack.Pop();
                        }
                        _parseState = 0;
                    }
                    break;
                case XmlLexerEvent.VisitOpenAngleExclimation:
                    {
                        _parseState = 10;
                    }
                    break;

            }
        }

        public virtual void ParseDocument(TextSnapshot textSnapshot)
        {
            _textSnapshot = textSnapshot;


            OnBegin();
            //reset
            _openEltStack.Clear();
            _waitingAttrName = null;
            _currentNodeName = null;
            _parseState = 0;

            //

            _myXmlLexer.BeginLex();
            _myXmlLexer.Analyze(textSnapshot);
            _myXmlLexer.EndLex();

            OnFinish();
        }

        protected virtual void OnBegin()
        {

        }
        public virtual void OnFinish()
        {

        }


        //-------------------
        protected virtual void OnTextNode(TextSpan text) { }
        protected virtual void OnAttribute(TextSpan localAttr, TextSpan value) { }
        protected virtual void OnAttribute(TextSpan ns, TextSpan localAttr, TextSpan value) { }

        protected virtual void OnVisitNewElement(TextSpan ns, TextSpan localName) { }
        protected virtual void OnVisitNewElement(TextSpan localName) { }

        protected virtual void OnEnteringElementBody() { }
        protected virtual void OnExitingElementBody() { }
    }



}