
namespace SqlProcedureConverterWinForm
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            txtInput = new System.Windows.Forms.TextBox();
            txtProcName = new System.Windows.Forms.TextBox();
            txtOutput = new System.Windows.Forms.TextBox();
            btnConvert = new System.Windows.Forms.Button();
            SuspendLayout();
            // 
            // txtInput
            // 
            txtInput.Location = new System.Drawing.Point(12, 12);
            txtInput.Multiline = true;
            txtInput.Name = "txtInput";
            txtInput.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            txtInput.Size = new System.Drawing.Size(560, 120);
            txtInput.TabIndex = 0;
            // 
            // txtProcName
            // 
            txtProcName.Location = new System.Drawing.Point(12, 140);
            txtProcName.Name = "txtProcName";
            txtProcName.Size = new System.Drawing.Size(200, 23);
            txtProcName.TabIndex = 1;
            txtProcName.Text = "usp_MyProcedure";
            // 
            // txtOutput
            // 
            txtOutput.Location = new System.Drawing.Point(12, 190);
            txtOutput.Multiline = true;
            txtOutput.Name = "txtOutput";
            txtOutput.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            txtOutput.Size = new System.Drawing.Size(560, 200);
            txtOutput.TabIndex = 2;
            // 
            // btnConvert
            // 
            btnConvert.Location = new System.Drawing.Point(480, 140);
            btnConvert.Name = "btnConvert";
            btnConvert.Size = new System.Drawing.Size(92, 30);
            btnConvert.TabIndex = 3;
            btnConvert.Text = "轉換";
            btnConvert.UseVisualStyleBackColor = true;
            btnConvert.Click += btnConvert_Click;
            // 
            // MainForm
            // 
            ClientSize = new System.Drawing.Size(584, 411);
            Controls.Add(btnConvert);
            Controls.Add(txtOutput);
            Controls.Add(txtProcName);
            Controls.Add(txtInput);
            Name = "MainForm";
            Text = "SQL Procedure 轉換器";
            ResumeLayout(false);
            PerformLayout();
        }

        private System.Windows.Forms.TextBox txtInput;
        private System.Windows.Forms.TextBox txtProcName;
        private System.Windows.Forms.TextBox txtOutput;
        private System.Windows.Forms.Button btnConvert;
    }
}
