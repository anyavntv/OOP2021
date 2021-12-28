using Lab1;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

class Cell
{
    private DataGridViewCell _parent;
    private string _value;
    private string _formula;
    private string _error;
    private string _name;

    public string Value
    {
        get { return _value; }
        set { _value = value.Equals("0") ? "0" : "1"; }
    }

    public string Formula
    {
        get { return _formula; }
        set { _formula = value; }
    }

    public DataGridViewCell Parent { get { return _parent; } }
    public string Name { get { return _name; } }

    public string Error
    {   
        get { return _error; }
        set { _error = value;}
    }
    public List<Cell> CellReferences { get; set; }


    public Cell(DataGridViewCell parent, string name, string formula)
    {
        _parent = parent;
        _name = name;
        _formula = formula;
        _value = "0";
        _error = "";
        CellReferences = new List<Cell>();
    }

    public Cell()
    {
        _name = "";
    }

    public string Evaluate()
    {
        Interpreter interpreter = new Interpreter(_formula, _name);

        Value = interpreter.EvaluateExpression();

        if (!(_error = interpreter.Error).Equals(""))
        {
            _value = "";
        }

        return _value;
    }
}