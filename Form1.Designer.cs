namespace CDCloser
{
    partial class MainForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            drive_select_label = new Label();
            drive_box = new ComboBox();
            label2 = new Label();
            burn_speed_box = new ComboBox();
            main_prog = new ProgressBar();
            burn_button = new Button();
            drive_refresh = new Button();
            browse_button = new Button();
            disc_label_box = new TextBox();
            label1 = new Label();
            file_grid = new DataGridView();
            remove_button = new Button();
            stat_bar = new StatusStrip();
            status = new ToolStripStatusLabel();
            status_label = new ToolStripStatusLabel();
            size = new ToolStripStatusLabel();
            total_size = new ToolStripStatusLabel();
            disk = new ToolStripStatusLabel();
            disk_cap_label = new ToolStripStatusLabel();
            aux_progress = new ToolStripProgressBar();
            close_media_checkbox = new CheckBox();
            cancel_button = new Button();
            ((System.ComponentModel.ISupportInitialize)file_grid).BeginInit();
            stat_bar.SuspendLayout();
            SuspendLayout();
            // 
            // drive_select_label
            // 
            drive_select_label.AutoSize = true;
            drive_select_label.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            drive_select_label.Location = new Point(13, 45);
            drive_select_label.Name = "drive_select_label";
            drive_select_label.Size = new Size(68, 15);
            drive_select_label.TabIndex = 0;
            drive_select_label.Text = "Select Drive";
            // 
            // drive_box
            // 
            drive_box.FormattingEnabled = true;
            drive_box.Location = new Point(86, 41);
            drive_box.Name = "drive_box";
            drive_box.Size = new Size(82, 23);
            drive_box.TabIndex = 1;
            drive_box.SelectedIndexChanged += drive_changed;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            label2.Location = new Point(13, 75);
            label2.Name = "label2";
            label2.Size = new Size(67, 15);
            label2.TabIndex = 4;
            label2.Text = "Burn Speed";
            // 
            // burn_speed_box
            // 
            burn_speed_box.FormattingEnabled = true;
            burn_speed_box.Items.AddRange(new object[] { "4x", "8x", "16x", "24x" });
            burn_speed_box.Location = new Point(86, 72);
            burn_speed_box.Name = "burn_speed_box";
            burn_speed_box.Size = new Size(82, 23);
            burn_speed_box.TabIndex = 5;
            // 
            // main_prog
            // 
            main_prog.Location = new Point(11, 101);
            main_prog.Name = "main_prog";
            main_prog.Size = new Size(509, 23);
            main_prog.TabIndex = 6;
            // 
            // burn_button
            // 
            burn_button.Location = new Point(523, 101);
            burn_button.Name = "burn_button";
            burn_button.Size = new Size(127, 23);
            burn_button.TabIndex = 8;
            burn_button.Text = "Start";
            burn_button.UseVisualStyleBackColor = true;
            burn_button.Click += start_burn;
            // 
            // drive_refresh
            // 
            drive_refresh.Location = new Point(174, 41);
            drive_refresh.Name = "drive_refresh";
            drive_refresh.Size = new Size(73, 23);
            drive_refresh.TabIndex = 9;
            drive_refresh.Text = "Refresh";
            drive_refresh.UseVisualStyleBackColor = true;
            drive_refresh.Click += refresh_clicked;
            // 
            // browse_button
            // 
            browse_button.Location = new Point(523, 10);
            browse_button.Name = "browse_button";
            browse_button.Size = new Size(127, 23);
            browse_button.TabIndex = 10;
            browse_button.Text = "Browse File(s)";
            browse_button.UseVisualStyleBackColor = true;
            browse_button.Click += browse_clicked;
            // 
            // disc_label_box
            // 
            disc_label_box.Location = new Point(86, 10);
            disc_label_box.Name = "disc_label_box";
            disc_label_box.Size = new Size(289, 23);
            disc_label_box.TabIndex = 11;
            disc_label_box.Text = "Label";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(13, 14);
            label1.Name = "label1";
            label1.Size = new Size(60, 15);
            label1.TabIndex = 13;
            label1.Text = "Disc Label";
            // 
            // file_grid
            // 
            file_grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            file_grid.Location = new Point(11, 130);
            file_grid.Name = "file_grid";
            file_grid.Size = new Size(639, 274);
            file_grid.TabIndex = 14;
            // 
            // remove_button
            // 
            remove_button.Location = new Point(523, 36);
            remove_button.Name = "remove_button";
            remove_button.Size = new Size(127, 23);
            remove_button.TabIndex = 15;
            remove_button.Text = "Remove Selected";
            remove_button.UseVisualStyleBackColor = true;
            remove_button.Click += remove_clicked;
            // 
            // stat_bar
            // 
            stat_bar.Items.AddRange(new ToolStripItem[] { status, status_label, size, total_size, disk, disk_cap_label, aux_progress });
            stat_bar.Location = new Point(0, 414);
            stat_bar.Name = "stat_bar";
            stat_bar.Size = new Size(662, 22);
            stat_bar.TabIndex = 16;
            stat_bar.Text = "statusStrip1";
            // 
            // status
            // 
            status.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            status.Name = "status";
            status.Size = new Size(45, 17);
            status.Text = "Status:";
            // 
            // status_label
            // 
            status_label.Name = "status_label";
            status_label.Size = new Size(118, 17);
            status_label.Text = "toolStripStatusLabel1";
            // 
            // size
            // 
            size.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            size.Name = "size";
            size.Size = new Size(85, 17);
            size.Text = "File Size Total:";
            // 
            // total_size
            // 
            total_size.Name = "total_size";
            total_size.Size = new Size(23, 17);
            total_size.Text = "0 B";
            // 
            // disk
            // 
            disk.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            disk.Name = "disk";
            disk.Size = new Size(83, 17);
            disk.Text = "Disk Capacity:";
            // 
            // disk_cap_label
            // 
            disk_cap_label.Name = "disk_cap_label";
            disk_cap_label.Size = new Size(29, 17);
            disk_cap_label.Text = "N/A";
            // 
            // aux_progress
            // 
            aux_progress.Name = "aux_progress";
            aux_progress.Size = new Size(100, 16);
            aux_progress.Style = ProgressBarStyle.Marquee;
            // 
            // close_media_checkbox
            // 
            close_media_checkbox.AutoSize = true;
            close_media_checkbox.Checked = true;
            close_media_checkbox.CheckState = CheckState.Checked;
            close_media_checkbox.Location = new Point(174, 75);
            close_media_checkbox.Name = "close_media_checkbox";
            close_media_checkbox.Size = new Size(91, 19);
            close_media_checkbox.TabIndex = 17;
            close_media_checkbox.Text = "Close Media";
            close_media_checkbox.UseVisualStyleBackColor = true;
            // 
            // cancel_button
            // 
            cancel_button.Enabled = false;
            cancel_button.Location = new Point(523, 75);
            cancel_button.Name = "cancel_button";
            cancel_button.Size = new Size(127, 23);
            cancel_button.TabIndex = 18;
            cancel_button.Text = "Cancel";
            cancel_button.UseVisualStyleBackColor = true;
            cancel_button.Click += cancel_burn_clicked;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(662, 436);
            Controls.Add(cancel_button);
            Controls.Add(close_media_checkbox);
            Controls.Add(stat_bar);
            Controls.Add(remove_button);
            Controls.Add(file_grid);
            Controls.Add(label1);
            Controls.Add(disc_label_box);
            Controls.Add(browse_button);
            Controls.Add(drive_refresh);
            Controls.Add(burn_button);
            Controls.Add(main_prog);
            Controls.Add(burn_speed_box);
            Controls.Add(label2);
            Controls.Add(drive_box);
            Controls.Add(drive_select_label);
            Name = "MainForm";
            Text = "CD Closer";
            ((System.ComponentModel.ISupportInitialize)file_grid).EndInit();
            stat_bar.ResumeLayout(false);
            stat_bar.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label drive_select_label;
        private ComboBox drive_box;
        private Label label2;
        private ComboBox burn_speed_box;
        private ProgressBar main_prog;
        private Label label3;
        private Button burn_button;
        private Button drive_refresh;
        private Button browse_button;
        private TextBox disc_label_box;
        private Label label1;
        private DataGridView file_grid;
        private Button remove_button;
        private StatusStrip stat_bar;
        private ToolStripStatusLabel status;
        private ToolStripStatusLabel status_label;
        private ToolStripStatusLabel size;
        private ToolStripStatusLabel total_size;
        private ToolStripStatusLabel disk;
        private ToolStripStatusLabel disk_cap_label;
        private ToolStripProgressBar aux_progress;
        private CheckBox close_media_checkbox;
        private Button cancel_button;
    }
}
