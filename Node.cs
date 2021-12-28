using OfficeOpenXml.FormulaParsing.LexicalAnalysis;
using System;
using System.Collections.Generic;
using System.Text;

namespace Lab1
{
    abstract class Node
    {
        public abstract double GetValue();
    }

    class NumberNode : Node
    {
        private double _value;
        private Token _token;
        public NumberNode(Token token)
        {
            _token = token;
            _value = double.Parse(token.Value);
        }
        public override double GetValue()
        {
            return _value;
        }
    }

    class ArithmeticOperationNode : Node
    {
        private Node _leftSide;
        private Node _rightSide;
        private Token _operation;

        public ArithmeticOperationNode(Node leftSide, Token operation, Node rightSide)
        {
            _leftSide = leftSide;
            _rightSide = rightSide;
            _operation = operation;
        }
        public override double GetValue()
        {
            switch (_operation.Type)
            {
                case TokenType.PLUS:
                    return _leftSide.GetValue() + _rightSide.GetValue();
                case TokenType.MINUS:
                    return _leftSide.GetValue() - _rightSide.GetValue();
                case TokenType.MULTIPLY:
                    return _leftSide.GetValue() * _rightSide.GetValue();
                case TokenType.DIVIDE:
                    return _leftSide.GetValue() / _rightSide.GetValue();
                case TokenType.MOD:
                    return _leftSide.GetValue() % _rightSide.GetValue();
                case TokenType.DIV:
                    return (int)_leftSide.GetValue() % _rightSide.GetValue();
                default: return 0;
            }
        }
    }


    class LogicalOperationNode : Node
    {
        private Node _leftSide;
        private Node _rightSide;
        private Token _operation;
        public LogicalOperationNode(Node leftSide, Token operation, Node rightSide)
        {
            _leftSide = leftSide;
            _rightSide = rightSide;
            _operation = operation;
        }
        public override double GetValue()
        {
            switch (_operation.Type)
            {
                case TokenType.GEQ:
                    return _leftSide.GetValue() >= _rightSide.GetValue() ? 1 : 0;
                case TokenType.LEQ:
                    return _leftSide.GetValue() <= _rightSide.GetValue() ? 1 : 0;
                case TokenType.NEQ:
                    return _leftSide.GetValue() != _rightSide.GetValue() ? 1 : 0;
                default: return 0;
            }
        }
    }

    class NegationNode : Node
    {
        private Node _node;
        public NegationNode(Node node)
        {
            _node = node;
        }
        public override double GetValue()
        {
            return _node.GetValue() == 0 ? 1 : 0;
        }
    }

    class IncDecNode : Node
    {

        private Node _node;
        private TokenType _operation;
        private string _invokerName;

        public IncDecNode(Node node, TokenType operation, string invokerName)
        {
            _node = node;
            _operation = operation;
            _invokerName = invokerName;
        }
        public override double GetValue()
        {
            if (_node is CellNode)
            {
                CellNode cellNode = (CellNode)_node;
                if (_invokerName.Equals(CellManager.Instance.CurrentCell.Name))
                {
                    cellNode.AddedValue += _operation == TokenType.INC ? 1 : -1;
                    return cellNode.GetValue();
                }
            }
            return _operation == TokenType.INC ? _node.GetValue() + 1 : _node.GetValue() - 1;
        }
    }

    class CellNode : Node
    {
        private string _name;
        private Cell _cell;

        public int AddedValue { get; set; }
        public CellNode(string name)
        {
            AddedValue = 0;
            _name = name;
            _cell = CellManager.Instance.GetCell(_name);
        }
            public override double GetValue() { 
            double result = Convert.ToDouble(_cell.Value) + AddedValue;
           _cell.Value=result.ToString();
            return result;
        }
    }
}
