using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Lab1
{
   public class Token
    {
        private TokenType _type;
        private string _value;


        public TokenType Type { get { return _type; } }

        public string Value { get { return _value; } }


        public Token(TokenType type, string val)
        {
            _type = type;
            _value = val;
        }
    }

    public enum TokenType
    {
        EOX,
        NUMBER,
        PLUS,
        MINUS,
        MULTIPLY,
        DIVIDE,
        MOD,
        DIV,
        LPAREN,
        RPAREN,
        LEQ,
        GEQ,
        NEQ,
        NOT,
        DEC,
        INC,
        CELL,
        ERROR

    }

    public class Tokenizer
    {
        private const char NIX = '\0';
        private const char PLUS = '+';
        private const char MINUS = '-';
        private const char MULTIPLY = '*';
        private const char DIVIDE = '/';
        private const char MOD = '%';
        private const char DIV = ':';
        private const char LPAREN = '(';
        private const char RPAREN = ')';
        private const char POINT = '.';
        private const char NOT = '!';
        private const char LESSER = '<';
        private const char GREATER = '>';
        private const char EQUAL = '=';
        private const string DEC = "DEC";
        private const string INC = "INC";

        private const string ERR_MANY_POINTS ="У введеному чилі більше однієї десяткової крапки";
        private const string ERR_LETTERS = "Введено літери після цифр";
        private const string ERR_CELL = "Некоректний формат імені комірки";
        private const string ERR_INVALID_TOKEN = "Введено некоректний символ";
        private const string ERR_AFTER_POINT = "Відсутні цифри після крапки";

        private List<char> _singleChars = new List<char>()
        { PLUS,MINUS,MULTIPLY,DIVIDE,MOD,DIV, LPAREN, RPAREN,NOT };

        private Dictionary<char, TokenType> _tokenPairs = new Dictionary<char, TokenType>()
        {
            [PLUS] = TokenType.PLUS,
            [MINUS] = TokenType.MINUS,
            [MULTIPLY] = TokenType.MULTIPLY,
            [DIVIDE] = TokenType.DIVIDE,
            [MOD] = TokenType.MOD,
            [DIV] = TokenType.DIV,
            [LPAREN] = TokenType.LPAREN,
            [RPAREN] = TokenType.RPAREN,
            [NOT] = TokenType.NOT,
        };
        
        
        
        private string _expression;
        private char _currentChar;
        private int _pos;
        private string _error;

        
        public string Error { get { return _error; } }
        public char CurrentChar { get { return _currentChar; } }

        
        
        public Tokenizer(string expression)
        {
            _expression = expression;
            _error = "";
            _pos = 0;
            _currentChar = expression[0];
        }

        private void ReadNextChar()
        {
            _pos++;
            _currentChar = _pos > _expression.Length - 1 ? NIX : _expression[_pos];
        }

        private void SkipWhiteSpace()
        {
            while (_currentChar != NIX && Char.IsWhiteSpace(_currentChar))
            {
                ReadNextChar();
            }
        }


        private Token ResolveErronousToken(string error)
        {
            _error = error;
            return new Token(TokenType.ERROR, error);
        }





        private Token ResolvePoint()
        {
            string value = "";

            while (_currentChar != NIX &&(Char.IsDigit(_currentChar)|| _currentChar == POINT))
            {
                value += _currentChar;
                ReadNextChar();

                if (Char.IsLetter(_currentChar))
                {
                    return ResolveErronousToken(ERR_LETTERS);
                }
                if(value.Contains(POINT)&& _currentChar==POINT)
                {
                    return ResolveErronousToken(ERR_MANY_POINTS);
                }
            }return new Token(TokenType.NUMBER, value);
        }


        private Token ResolveSingleCharOperation()
        {
            char tmp = _currentChar;
            ReadNextChar();
            return new Token(_tokenPairs[tmp], tmp.ToString());
        }

        private Token ResolveComparison(TokenType type, char firstChar)
        {
            string tmp = _currentChar.ToString();
            ReadNextChar();
            return new Token(type, firstChar.ToString() + tmp);
        }

        private Token ResolveTwoCharsOperation()
        {
            char firstChar = _currentChar;
            ReadNextChar();

            if (firstChar==LESSER)
            {
                if (_currentChar == GREATER)
                    return ResolveComparison(TokenType.NEQ, firstChar); 
                if(_currentChar == EQUAL)
                    return ResolveComparison(TokenType.LEQ, firstChar);
            }
            if(firstChar == GREATER&&_currentChar==EQUAL)
            {
                return ResolveComparison(TokenType.GEQ, firstChar);
            }
            return ResolveErronousToken(ERR_INVALID_TOKEN);
        }





        private Token ResolveNumber()
        {
            Token result = ResolvePoint();

            if(_expression [_pos -1]== POINT)
            {
                return ResolveErronousToken(ERR_AFTER_POINT);
            }
            return result;
        }

        private Token ResolveCell()
        {
            string result = "";
            while(_currentChar != NIX && Char.IsLetterOrDigit(_currentChar))
            {
                result += _currentChar;
                ReadNextChar();
            }
            var matches = new Regex(@"^R(?<row>\d +)C(?<col>\d+)$").Matches(result);
            if (matches.Count != 1)
            {
                return ResolveErronousToken(ERR_CELL);
            }
            return new Token(TokenType.CELL, matches[0].Groups[0].Value);
        }

        private bool ReadIncDec(char firstchar, out string charSequence)
        {

            charSequence = firstchar.ToString();
            char current;
            int i = 1;
            while ((INC.Contains(charSequence) || DEC.Contains(charSequence)) && i < INC.Length)
            {
                ReadNextChar();

                current = Char.ToUpper(_currentChar);
                if (!(charSequence.Equals(INC.Substring(0, i)) || charSequence.Equals(DEC.Substring(0, i))))
                {
                    return false;
                }
                charSequence += current.ToString();
                i++;
            }
            ReadNextChar();

            return true;
        }




            private Token ResolveLetters()
        {
            char firstChar = Char.ToUpper( _currentChar);

            if(firstChar =='D'|| firstChar =='I')
            {
                return ResolveIncDec(firstChar);
            }
            return ResolveCell();
        }

 
        
        private Token ResolveIncDec(char firstChar)
        {
            if(!ReadIncDec(firstChar, out string charSequence))
            {
                return ResolveErronousToken(ERR_INVALID_TOKEN);
            }
            if (charSequence.Equals(INC))
            {
                return new Token(TokenType.INC, INC);
            }
            if (charSequence.Equals(DEC))
            {
                return new Token(TokenType.DEC, DEC);
            }
            return ResolveErronousToken(ERR_INVALID_TOKEN);

        }






        public Token ReadNextToken()
        {

            while (_currentChar != NIX) {
                SkipWhiteSpace();
                if (Char.IsLetter(_currentChar))
                {
                    return ResolveLetters();
                }
                if (Char.IsDigit(_currentChar) || _currentChar == POINT)
                {
                    return ResolveNumber();
                }

                if (_singleChars.Contains(_currentChar))
                {
                    return ResolveSingleCharOperation();
                }
                return ResolveTwoCharsOperation();
            }
            return new Token(TokenType.EOX, NIX.ToString());
            
        }

    }
}
