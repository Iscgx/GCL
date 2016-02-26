namespace testide
{
    partial class ErrorList
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.errorTabControl = new System.Windows.Forms.TabControl();
            this.Errors = new System.Windows.Forms.TabPage();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.errorTreeView = new System.Windows.Forms.TreeView();
            this.errorTabControl.SuspendLayout();
            this.Errors.SuspendLayout();
            this.SuspendLayout();
            // 
            // errorTabControl
            // 
            this.errorTabControl.Controls.Add(this.Errors);
            this.errorTabControl.Controls.Add(this.tabPage2);
            this.errorTabControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.errorTabControl.Location = new System.Drawing.Point(0, 0);
            this.errorTabControl.Name = "errorTabControl";
            this.errorTabControl.SelectedIndex = 0;
            this.errorTabControl.Size = new System.Drawing.Size(275, 372);
            this.errorTabControl.TabIndex = 0;
            // 
            // Errors
            // 
            this.Errors.Controls.Add(this.errorTreeView);
            this.Errors.Location = new System.Drawing.Point(4, 22);
            this.Errors.Name = "Errors";
            this.Errors.Padding = new System.Windows.Forms.Padding(3);
            this.Errors.Size = new System.Drawing.Size(267, 346);
            this.Errors.TabIndex = 0;
            this.Errors.Text = "Error List";
            this.Errors.UseVisualStyleBackColor = true;
            // 
            // tabPage2
            // 
            this.tabPage2.Location = new System.Drawing.Point(4, 22);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage2.Size = new System.Drawing.Size(267, 346);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "tabPage2";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // errorTreeView
            // 
            this.errorTreeView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.errorTreeView.Location = new System.Drawing.Point(3, 3);
            this.errorTreeView.Name = "errorTreeView";
            this.errorTreeView.Size = new System.Drawing.Size(261, 340);
            this.errorTreeView.TabIndex = 1;
            // 
            // ErrorList
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(275, 372);
            this.Controls.Add(this.errorTabControl);
            this.Name = "ErrorList";
            this.Text = "ErrorList";
            this.errorTabControl.ResumeLayout(false);
            this.Errors.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TabPage Errors;
        private System.Windows.Forms.TabPage tabPage2;
        public System.Windows.Forms.TabControl errorTabControl;
        public System.Windows.Forms.TreeView errorTreeView;
    }
}