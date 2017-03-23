using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace base64toUTF8
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            //String base64 = Convert.ToBase64String(bytes);
            byte[] newBytes = Convert.FromBase64String(richTextBox1.Text);
            richTextBox2.Text=Encoding.UTF8.GetString(newBytes);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            byte[] newBytes = Convert.FromBase64String(richTextBox1.Text);
            richTextBox2.Text = Encoding.ASCII.GetString(newBytes);
        }

      
    }
}
