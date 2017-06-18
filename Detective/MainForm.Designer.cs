using System.Drawing;
namespace Detective
{
    partial class MainForm
    {
        /// <summary>
        /// 必要なデザイナー変数です。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 使用中のリソースをすべてクリーンアップします。
        /// </summary>
        /// <param name="disposing">マネージ リソースが破棄される場合 true、破棄されない場合は false です。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows フォーム デザイナーで生成されたコード

        /// <summary>
        /// デザイナー サポートに必要なメソッドです。このメソッドの内容を
        /// コード エディターで変更しないでください。
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.mainInstructionLabel = new System.Windows.Forms.Label();
            this.mainPanel = new System.Windows.Forms.Panel();
            this.isSendFeedback = new System.Windows.Forms.CheckBox();
            this.restartButton = new System.Windows.Forms.Button();
            this.exitButton = new System.Windows.Forms.Button();
            this.errorPanel = new System.Windows.Forms.Panel();
            this.errorText = new System.Windows.Forms.TextBox();
            this.subInstructionLabel = new System.Windows.Forms.Label();
            this.feedbackLabel = new System.Windows.Forms.Label();
            this.mainPanel.SuspendLayout();
            this.errorPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // mainInstructionLabel
            // 
            resources.ApplyResources(this.mainInstructionLabel, "mainInstructionLabel");
            this.mainInstructionLabel.ForeColor = System.Drawing.Color.White;
            this.mainInstructionLabel.Name = "mainInstructionLabel";
            // 
            // mainPanel
            // 
            resources.ApplyResources(this.mainPanel, "mainPanel");
            this.mainPanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(162)))), ((int)(((byte)(0)))), ((int)(((byte)(37)))));
            this.mainPanel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.mainPanel.Controls.Add(this.isSendFeedback);
            this.mainPanel.Controls.Add(this.restartButton);
            this.mainPanel.Controls.Add(this.exitButton);
            this.mainPanel.Controls.Add(this.errorPanel);
            this.mainPanel.Controls.Add(this.subInstructionLabel);
            this.mainPanel.Controls.Add(this.mainInstructionLabel);
            this.mainPanel.Name = "mainPanel";
            // 
            // isSendFeedback
            // 
            resources.ApplyResources(this.isSendFeedback, "isSendFeedback");
            this.isSendFeedback.ForeColor = System.Drawing.Color.White;
            this.isSendFeedback.Name = "isSendFeedback";
            this.isSendFeedback.UseVisualStyleBackColor = true;
            // 
            // restartButton
            // 
            resources.ApplyResources(this.restartButton, "restartButton");
            this.restartButton.ForeColor = System.Drawing.Color.White;
            this.restartButton.Name = "restartButton";
            this.restartButton.UseVisualStyleBackColor = true;
            this.restartButton.Click += new System.EventHandler(this.restartButton_Click);
            // 
            // exitButton
            // 
            resources.ApplyResources(this.exitButton, "exitButton");
            this.exitButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.exitButton.ForeColor = System.Drawing.Color.White;
            this.exitButton.Name = "exitButton";
            this.exitButton.UseVisualStyleBackColor = true;
            this.exitButton.Click += new System.EventHandler(this.exitButton_Click);
            // 
            // errorPanel
            // 
            resources.ApplyResources(this.errorPanel, "errorPanel");
            this.errorPanel.BackColor = System.Drawing.SystemColors.Control;
            this.errorPanel.Controls.Add(this.errorText);
            this.errorPanel.Name = "errorPanel";
            // 
            // errorText
            // 
            resources.ApplyResources(this.errorText, "errorText");
            this.errorText.BackColor = System.Drawing.SystemColors.Control;
            this.errorText.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.errorText.Name = "errorText";
            this.errorText.ReadOnly = true;
            // 
            // subInstructionLabel
            // 
            resources.ApplyResources(this.subInstructionLabel, "subInstructionLabel");
            this.subInstructionLabel.ForeColor = System.Drawing.Color.White;
            this.subInstructionLabel.Name = "subInstructionLabel";
            // 
            // feedbackLabel
            // 
            resources.ApplyResources(this.feedbackLabel, "feedbackLabel");
            this.feedbackLabel.BackColor = System.Drawing.Color.Black;
            this.feedbackLabel.ForeColor = System.Drawing.Color.White;
            this.feedbackLabel.Name = "feedbackLabel";
            // 
            // MainForm
            // 
            this.AcceptButton = this.restartButton;
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.Control;
            this.CancelButton = this.exitButton;
            this.ControlBox = false;
            this.Controls.Add(this.mainPanel);
            this.Controls.Add(this.feedbackLabel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "MainForm";
            this.mainPanel.ResumeLayout(false);
            this.mainPanel.PerformLayout();
            this.errorPanel.ResumeLayout(false);
            this.errorPanel.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label mainInstructionLabel;
        private System.Windows.Forms.Panel mainPanel;
        private System.Windows.Forms.Label subInstructionLabel;
        private System.Windows.Forms.Panel errorPanel;
        private System.Windows.Forms.TextBox errorText;
        private System.Windows.Forms.CheckBox isSendFeedback;
        private System.Windows.Forms.Button restartButton;
        private System.Windows.Forms.Button exitButton;
        private System.Windows.Forms.Label feedbackLabel;
    }
}

