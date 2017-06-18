namespace Wonder
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
            this.openDirectory = new System.Windows.Forms.Button();
            this.inFile = new System.Windows.Forms.TextBox();
            this.outFile = new System.Windows.Forms.TextBox();
            this.saveFile = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.keyFile = new System.Windows.Forms.TextBox();
            this.openKeyFile = new System.Windows.Forms.Button();
            this.generatePackage = new System.Windows.Forms.Button();
            this.generateKeys = new System.Windows.Forms.Button();
            this.generateSignFile = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // openDirectory
            // 
            this.openDirectory.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.openDirectory.Location = new System.Drawing.Point(443, 12);
            this.openDirectory.Name = "openDirectory";
            this.openDirectory.Size = new System.Drawing.Size(29, 23);
            this.openDirectory.TabIndex = 0;
            this.openDirectory.Text = "...";
            this.openDirectory.UseVisualStyleBackColor = true;
            this.openDirectory.Click += new System.EventHandler(this.openDirectory_Click);
            // 
            // inFile
            // 
            this.inFile.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.inFile.Location = new System.Drawing.Point(101, 14);
            this.inFile.Name = "inFile";
            this.inFile.Size = new System.Drawing.Size(336, 19);
            this.inFile.TabIndex = 1;
            this.inFile.TextChanged += new System.EventHandler(this.inFile_TextChanged);
            // 
            // outFile
            // 
            this.outFile.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.outFile.Location = new System.Drawing.Point(101, 43);
            this.outFile.Name = "outFile";
            this.outFile.Size = new System.Drawing.Size(336, 19);
            this.outFile.TabIndex = 2;
            // 
            // saveFile
            // 
            this.saveFile.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.saveFile.Location = new System.Drawing.Point(443, 41);
            this.saveFile.Name = "saveFile";
            this.saveFile.Size = new System.Drawing.Size(29, 23);
            this.saveFile.TabIndex = 3;
            this.saveFile.Text = "...";
            this.saveFile.UseVisualStyleBackColor = true;
            this.saveFile.Click += new System.EventHandler(this.saveFile_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 17);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(78, 12);
            this.label1.TabIndex = 4;
            this.label1.Text = "入力ディレクトリ";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 46);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(63, 12);
            this.label2.TabIndex = 5;
            this.label2.Text = "出力ファイル";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(12, 75);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(83, 12);
            this.label3.TabIndex = 6;
            this.label3.Text = "プライベート キー";
            // 
            // keyFile
            // 
            this.keyFile.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.keyFile.Location = new System.Drawing.Point(101, 72);
            this.keyFile.Name = "keyFile";
            this.keyFile.Size = new System.Drawing.Size(336, 19);
            this.keyFile.TabIndex = 7;
            // 
            // openKeyFile
            // 
            this.openKeyFile.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.openKeyFile.Location = new System.Drawing.Point(443, 70);
            this.openKeyFile.Name = "openKeyFile";
            this.openKeyFile.Size = new System.Drawing.Size(29, 23);
            this.openKeyFile.TabIndex = 8;
            this.openKeyFile.Text = "...";
            this.openKeyFile.UseVisualStyleBackColor = true;
            this.openKeyFile.Click += new System.EventHandler(this.openKeyFile_Click);
            // 
            // generatePackage
            // 
            this.generatePackage.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.generatePackage.Location = new System.Drawing.Point(300, 99);
            this.generatePackage.Name = "generatePackage";
            this.generatePackage.Size = new System.Drawing.Size(172, 23);
            this.generatePackage.TabIndex = 9;
            this.generatePackage.Text = "Generate signed package";
            this.generatePackage.UseVisualStyleBackColor = true;
            this.generatePackage.Click += new System.EventHandler(this.generatePackage_Click);
            // 
            // generateKeys
            // 
            this.generateKeys.Location = new System.Drawing.Point(12, 99);
            this.generateKeys.Name = "generateKeys";
            this.generateKeys.Size = new System.Drawing.Size(112, 23);
            this.generateKeys.TabIndex = 10;
            this.generateKeys.Text = "Generate keys";
            this.generateKeys.UseVisualStyleBackColor = true;
            this.generateKeys.Click += new System.EventHandler(this.generateKeys_Click);
            // 
            // generateSignFile
            // 
            this.generateSignFile.Location = new System.Drawing.Point(130, 99);
            this.generateSignFile.Name = "generateSignFile";
            this.generateSignFile.Size = new System.Drawing.Size(112, 23);
            this.generateSignFile.TabIndex = 11;
            this.generateSignFile.Text = "Get sign for file";
            this.generateSignFile.UseVisualStyleBackColor = true;
            this.generateSignFile.Click += new System.EventHandler(this.generateSignFile_Click);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(484, 131);
            this.Controls.Add(this.generateSignFile);
            this.Controls.Add(this.generateKeys);
            this.Controls.Add(this.generatePackage);
            this.Controls.Add(this.openKeyFile);
            this.Controls.Add(this.keyFile);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.saveFile);
            this.Controls.Add(this.outFile);
            this.Controls.Add(this.inFile);
            this.Controls.Add(this.openDirectory);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "MainForm";
            this.Text = "StarryEyes Patch Generator";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button openDirectory;
        private System.Windows.Forms.TextBox inFile;
        private System.Windows.Forms.TextBox outFile;
        private System.Windows.Forms.Button saveFile;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox keyFile;
        private System.Windows.Forms.Button openKeyFile;
        private System.Windows.Forms.Button generatePackage;
        private System.Windows.Forms.Button generateKeys;
        private System.Windows.Forms.Button generateSignFile;
    }
}

