
using System;
using System.Windows.Forms;

namespace SqlProcedureConverterWinForm
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        private void btnConvert_Click(object sender, EventArgs e)
        {
            var converter = new SqlToStoredProcedureConverter();
            string sql = txtInput.Text;
            string procName = txtProcName.Text;

            string result = converter.ConvertToStoredProcedure(sql, procName);
            txtOutput.Text = result;
        }
    }
}
