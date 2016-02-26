using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using gcl2;

namespace testide
{
    public partial class Main : Form
    {
        private readonly CodeParser _codeParser;
        private readonly ErrorList _errorList;
        private DateTime _lastParse;

        public Main()
        {
            InitializeComponent();
            var sourceCode = File.ReadAllText(@"SourceCode.txt");
            var sourceTokens = File.ReadAllText(@"Tokens.txt");
            var grammarCode = File.ReadAllText(@"GrammarGCL.txt");
            var grammarTokens = File.ReadAllText(@"GrammarTokens.txt");
            _codeParser = new CodeParser(sourceTokens, grammarTokens, grammarCode);
            _errorList = new ErrorList();
            _lastParse = DateTime.Now;
        }

        private void Main_Load(object sender, EventArgs e)
        {
            _errorList.Show(this);
            _codeParser.OnLexicalError += message => _errorList.errorTreeView.Nodes.Add(message);
            _codeParser.OnSintacticalError += message => _errorList.errorTreeView.Nodes.Add(message);
        }

        private void codeTextBox_TextChanged(object sender, EventArgs e)
        {
            if ((DateTime.Now - _lastParse).TotalMilliseconds > 10)
            {
                var s = sender as RichTextBox;
                _errorList.errorTreeView.Nodes.Clear();
                if (_codeParser != null && s != null)
                {
                    _codeParser.Parse(s.Text);
                }
                _lastParse = DateTime.Now;
            }
            
            
        }
    }
}
