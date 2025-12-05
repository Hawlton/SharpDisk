using System.ComponentModel;
using System.Diagnostics;

namespace CDCloser
{
    public partial class MainForm : Form
    {
        private BindingList<SelectedFile> burn_list = new();
        private BindingList<DriveInfo> drive_list = new();
        private long disc_capacity = 0;
        public MainForm()
        {
            InitializeComponent();
            aux_progress.Visible = false;
            burn_speed_box.SelectedIndex = 1;

            file_grid.AutoGenerateColumns = false;
            file_grid.ReadOnly = true;
            file_grid.AllowUserToAddRows = false;
            file_grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            file_grid.MultiSelect = true;
            var name_column = new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(SelectedFile.filename),
                HeaderText = "Filename",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                FillWeight = 80
            };
            var size_column = new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(SelectedFile.readable_size),
                HeaderText = "Size",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                FillWeight = 20
            };
            file_grid.Columns.Add(name_column);
            file_grid.Columns.Add(size_column);
            file_grid.DataSource = burn_list;
            drive_box.DataSource = drive_list;
            drive_box.DisplayMember = nameof(DriveInfo.Name);

            populate_disc_drives();

            if (drive_box.Items.Count > 0) check_drive(0);
            else
            {
                MessageBox.Show("No optical media drives detected", "Drive Selection Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                status_label.Text = "No optical drives found.";
                status_label.ForeColor = Color.Red;
            }
        }

        private async void check_drive(int index)
        {
            status_label.Text = "Checking drive...";
            aux_progress.Visible = true;
            if(drive_list.Count == 0)
            {
                status_label.Text = "No optical drives found.";
                aux_progress.Visible = false;
                status_label.ForeColor = Color.Red;
                return;
            }
            if (index < 0 || index >= drive_list.Count) index = 0;
            DriveInfo drive = drive_list[index];
            bool media_present = await Task.Run(() => Globals.is_media_present(drive.Name));
            disc_capacity = await Task.Run(() => Globals.get_media_capacity(drive.Name));
            disk_cap_label.Text = Globals.to_human_readable(disc_capacity);
            if (media_present && disc_capacity > burn_list.Sum(f => f.byte_size))
            {
                status_label.Text = "Drive is ready.";
                status_label.ForeColor = Color.Green;
                aux_progress.Visible = false;
            }
            else
            {
                status_label.Text = "Drive is not ready.";
                status_label.ForeColor = Color.Red;
                aux_progress.Visible = false;
            }

        }

        //private void check_drive(int index)
        //{
        //    // Guard against invalid index and empty list.
        //    if (drive_box.Items.Count == 0)
        //    {
        //        MessageBox.Show("No optical media drives detected", "Drive Selection Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        //        status_label.Text = "No optical drives found.";
        //        status_label.ForeColor = Color.Red;
        //        return;
        //    }

        //    if (index < 0 || index >= drive_box.Items.Count)
        //    {
        //        index = 0; // fallback to first
        //    }

        //    // Set selection safely.
        //    if (drive_box.SelectedIndex != index)
        //    {
        //        drive_box.SelectedIndex = index;
        //    }

        //    var selectedName = drive_box.SelectedItem as string;
        //    if (string.IsNullOrEmpty(selectedName))
        //    {
        //        status_label.Text = "No drive selected.";
        //        status_label.ForeColor = Color.Red;
        //        return;
        //    }

        //    var selected_drive = DriveInfo.GetDrives()
        //        .FirstOrDefault(d => string.Equals(d.Name, selectedName, StringComparison.OrdinalIgnoreCase));

        //    if (selected_drive != null && selected_drive.IsReady)
        //    {
        //        Debug.WriteLine($"Selected drive: {selected_drive.Name}");
        //        disk_cap_label.Text = to_human_readable(selected_drive.TotalSize);

        //        long totalBytes = burn_list.Sum(f => f.byte_size);
        //        if (selected_drive.TotalSize >= totalBytes)
        //        {
        //            Debug.WriteLine("Drive is ready for burning.");
        //            status_label.Text = "Drive is ready.";
        //            status_label.ForeColor = Color.Green;
        //        }
        //        else
        //        {
        //            status_label.Text = "Not enough space on disc for selected files.";
        //            status_label.ForeColor = Color.Red;
        //        }
        //    }
        //    else
        //    {
        //        status_label.Text = "Drive is not ready. Please insert a disc.";
        //        status_label.ForeColor = Color.Red;
        //    }
        //}

        private void drive_changed(object sender, EventArgs e)
        {
            check_drive(drive_box.SelectedIndex);
        }

        private void browse_clicked(object sender, EventArgs e)
        {
            using var file_dialog = new OpenFileDialog
            {
                Title = "Select files to burn",
                Multiselect = true,
                CheckFileExists = true,
            };
            if (file_dialog.ShowDialog() == DialogResult.OK)
            {
                foreach (var Path in file_dialog.FileNames)
                {
                    if (burn_list.Any(f => f.path == Path))
                    {
                        continue;
                    }

                    var file_info = new FileInfo(Path);
                    burn_list.Add(new SelectedFile
                    {
                        filename = file_info.Name,
                        byte_size = file_info.Length,
                        path = file_info.FullName
                    });
                }
                total_size.Text = Globals.to_human_readable(burn_list.Sum(f => f.byte_size));
                if (burn_list.Sum(f => f.byte_size) > disc_capacity)
                {
                    status_label.Text = "Not enough space on disc for selected files.";
                    status_label.ForeColor = Color.Red;
                }
                else
                {
                    status_label.Text = "Drive is ready.";
                    status_label.ForeColor = Color.Green;
                }
            }
        }

        private void remove_clicked(object sender, EventArgs e)
        {
            foreach (DataGridViewRow row in file_grid.SelectedRows)
            {
                if (row.DataBoundItem is SelectedFile selected_file)
                {
                    burn_list.Remove(selected_file);
                }
            }
            total_size.Text = Globals.to_human_readable(burn_list.Sum(f => f.byte_size));
            if (drive_box.Items.Count > 0)
            {
                check_drive(drive_box.SelectedIndex);
            }
        }
        
        //fix this later
        private void refresh_clicked(object sender, EventArgs e)
        {
            populate_disc_drives();
            if (drive_box.Items.Count > 0)
            {
                check_drive(0);
            }
            else
            {
                status_label.Text = "No optical drives found.";
                status_label.ForeColor = Color.Red;
            }
        }

        private void populate_disc_drives()
        {
            drive_list.Clear();
            foreach(var drive in DriveInfo.GetDrives())
            {
                if (drive.DriveType == DriveType.CDRom)
                {
                    drive_list.Add(drive);
                }
            }
        }



        //private sealed class SelectedFile
        //{
        //    public string filename { get; init; } = string.Empty;
        //    public long byte_size { get; init; }
        //    public string path { get; init; } = string.Empty;
        //    public string readable_size => Globals.to_human_readable(byte_size);
        //}
    }
}
