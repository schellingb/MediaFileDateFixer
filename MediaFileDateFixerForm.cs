/*
  Media File Date Fixer
  Copyright 2018 Bernhard Schelling

  Licensed under the Apache License, Version 2.0 (the "License");
  you may not use this file except in compliance with the License.
  You may obtain a copy of the License at

      http://www.apache.org/licenses/LICENSE-2.0

  Unless required by applicable law or agreed to in writing, software
  distributed under the License is distributed on an "AS IS" BASIS,
  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
  See the License for the specific language governing permissions and
  limitations under the License.
*/

using System;
using System.Drawing;
using System.Windows.Forms;

namespace MediaFileDateFixer
{
    class MediaFileDateFixerForm : Form
    {
        public MediaFileDateFixerForm()
        {
            InitializeComponent();
            this.Icon = System.Drawing.Icon.ExtractAssociatedIcon(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
        }

        internal DataGridView gridMain;
        internal Button btnFilter;
        internal Button btnApply;
        internal Label lblState;
        internal ProgressBar pbProgress;
        internal Button btnEnableAll;
        internal Button btnDisableAll;
        internal NumericUpDown numOffsetHour;
        internal NumericUpDown numOffsetMinute;
        private Label label1;
        private Label label2;
        private Label label3;
        internal Button btnOpen;

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
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.gridMain = new System.Windows.Forms.DataGridView();
            this.btnFilter = new System.Windows.Forms.Button();
            this.btnApply = new System.Windows.Forms.Button();
            this.lblState = new System.Windows.Forms.Label();
            this.pbProgress = new System.Windows.Forms.ProgressBar();
            this.btnEnableAll = new System.Windows.Forms.Button();
            this.btnDisableAll = new System.Windows.Forms.Button();
            this.numOffsetHour = new System.Windows.Forms.NumericUpDown();
            this.numOffsetMinute = new System.Windows.Forms.NumericUpDown();
            this.btnOpen = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.gridMain)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numOffsetHour)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numOffsetMinute)).BeginInit();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.label1.Location = new System.Drawing.Point(597, 462);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(41, 18);
            this.label1.TabIndex = 6;
            this.label1.Text = "Offset:";
            this.label1.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // label2
            // 
            this.label2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.label2.Location = new System.Drawing.Point(683, 462);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(14, 18);
            this.label2.TabIndex = 8;
            this.label2.Text = "h";
            // 
            // label3
            // 
            this.label3.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.label3.Location = new System.Drawing.Point(736, 462);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(24, 18);
            this.label3.TabIndex = 10;
            this.label3.Text = "min";
            // 
            // gridMain
            // 
            this.gridMain.AllowUserToAddRows = false;
            this.gridMain.AllowUserToDeleteRows = false;
            this.gridMain.AllowUserToResizeRows = false;
            this.gridMain.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.gridMain.BackgroundColor = System.Drawing.SystemColors.Window;
            this.gridMain.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.gridMain.GridColor = System.Drawing.SystemColors.Control;
            this.gridMain.Location = new System.Drawing.Point(13, 13);
            this.gridMain.Name = "gridMain";
            this.gridMain.ReadOnly = true;
            this.gridMain.RowHeadersVisible = false;
            this.gridMain.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.gridMain.ShowCellErrors = false;
            this.gridMain.ShowCellToolTips = false;
            this.gridMain.ShowEditingIcon = false;
            this.gridMain.ShowRowErrors = false;
            this.gridMain.Size = new System.Drawing.Size(1038, 438);
            this.gridMain.TabIndex = 0;
            // 
            // btnFilter
            // 
            this.btnFilter.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnFilter.Location = new System.Drawing.Point(109, 458);
            this.btnFilter.Name = "btnFilter";
            this.btnFilter.Size = new System.Drawing.Size(90, 23);
            this.btnFilter.TabIndex = 2;
            this.btnFilter.Text = "Filter Results";
            this.btnFilter.UseVisualStyleBackColor = true;
            // 
            // btnApply
            // 
            this.btnApply.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnApply.Location = new System.Drawing.Point(397, 458);
            this.btnApply.Name = "btnApply";
            this.btnApply.Size = new System.Drawing.Size(194, 23);
            this.btnApply.TabIndex = 5;
            this.btnApply.Tag = "Apply # Meta Date to File Date";
            this.btnApply.Text = "Apply Meta Date to File Date";
            this.btnApply.UseVisualStyleBackColor = true;
            // 
            // lblState
            // 
            this.lblState.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lblState.Location = new System.Drawing.Point(766, 462);
            this.lblState.Name = "lblState";
            this.lblState.Size = new System.Drawing.Size(101, 18);
            this.lblState.TabIndex = 11;
            this.lblState.Text = "Ready";
            this.lblState.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // pbProgress
            // 
            this.pbProgress.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.pbProgress.Location = new System.Drawing.Point(873, 458);
            this.pbProgress.Name = "pbProgress";
            this.pbProgress.Size = new System.Drawing.Size(178, 23);
            this.pbProgress.TabIndex = 12;
            // 
            // btnEnableAll
            // 
            this.btnEnableAll.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnEnableAll.Location = new System.Drawing.Point(205, 458);
            this.btnEnableAll.Name = "btnEnableAll";
            this.btnEnableAll.Size = new System.Drawing.Size(90, 23);
            this.btnEnableAll.TabIndex = 3;
            this.btnEnableAll.Text = "Enable All";
            this.btnEnableAll.UseVisualStyleBackColor = true;
            // 
            // btnDisableAll
            // 
            this.btnDisableAll.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnDisableAll.Location = new System.Drawing.Point(301, 458);
            this.btnDisableAll.Name = "btnDisableAll";
            this.btnDisableAll.Size = new System.Drawing.Size(90, 23);
            this.btnDisableAll.TabIndex = 4;
            this.btnDisableAll.Text = "Disable All";
            this.btnDisableAll.UseVisualStyleBackColor = true;
            // 
            // numOffsetHour
            // 
            this.numOffsetHour.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.numOffsetHour.Location = new System.Drawing.Point(646, 460);
            this.numOffsetHour.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this.numOffsetHour.Minimum = new decimal(new int[] {
            1000,
            0,
            0,
            -2147483648});
            this.numOffsetHour.Name = "numOffsetHour";
            this.numOffsetHour.Size = new System.Drawing.Size(35, 20);
            this.numOffsetHour.TabIndex = 7;
            // 
            // numOffsetMinute
            // 
            this.numOffsetMinute.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.numOffsetMinute.Location = new System.Drawing.Point(700, 460);
            this.numOffsetMinute.Maximum = new decimal(new int[] {
            59,
            0,
            0,
            0});
            this.numOffsetMinute.Name = "numOffsetMinute";
            this.numOffsetMinute.Size = new System.Drawing.Size(35, 20);
            this.numOffsetMinute.TabIndex = 9;
            // 
            // btnOpen
            // 
            this.btnOpen.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnOpen.Location = new System.Drawing.Point(13, 458);
            this.btnOpen.Name = "btnOpen";
            this.btnOpen.Size = new System.Drawing.Size(90, 23);
            this.btnOpen.TabIndex = 1;
            this.btnOpen.Text = "Open Folder";
            this.btnOpen.UseVisualStyleBackColor = true;
            // 
            // MediaFileDateFixerForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1063, 492);
            this.Controls.Add(this.btnOpen);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.numOffsetMinute);
            this.Controls.Add(this.numOffsetHour);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.btnDisableAll);
            this.Controls.Add(this.btnEnableAll);
            this.Controls.Add(this.pbProgress);
            this.Controls.Add(this.lblState);
            this.Controls.Add(this.btnApply);
            this.Controls.Add(this.btnFilter);
            this.Controls.Add(this.gridMain);
            this.Name = "MediaFileDateFixerForm";
            this.Text = "Media File Date Fixer";
            ((System.ComponentModel.ISupportInitialize)(this.gridMain)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numOffsetHour)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numOffsetMinute)).EndInit();
            this.ResumeLayout(false);

        }
        #endregion
  }
}
