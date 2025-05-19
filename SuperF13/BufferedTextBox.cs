using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SuperF13
{
    public class BufferedTextBox : TextBox
    {
        // idk if this works for buffering actually
        public BufferedTextBox()
        {
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            this.UpdateStyles();

            // other useful
            this.BackColor = Color.FromArgb(0xb6, 0xcb, 0xe4);
            this.KeyDown += TextBox_KeyDown;
        }

        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            TextBox box = (TextBox)sender;
            // todo fix ctrl + left and ctrl + right on text "qwd.qwd.qwd.qwd"
            if (e.KeyData == (Keys.Back | Keys.Control))
            {
                if (!box.ReadOnly && box.SelectionLength == 0)
                {
                    RemoveWord(box);
                }
                if (box.SelectionLength != 0)
                {
                    int selectionStart = box.SelectionStart;
                    string after = box.Text.Substring(box.SelectionStart + box.SelectionLength);
                    box.Text = box.Text.Substring(0, box.SelectionStart) + after;
                    box.SelectionStart = selectionStart;
                }
                e.SuppressKeyPress = true;
            }
        }

        private void RemoveWord(TextBox box)
        {
            string text = Regex.Replace(box.Text.Substring(0, box.SelectionStart), @"(^\W)?\w*\W*$", "");
            box.Text = text + box.Text.Substring(box.SelectionStart);
            box.SelectionStart = text.Length;
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x02000000;  // WS_EX_COMPOSITED
                return cp;
            }
        }
    }
}
