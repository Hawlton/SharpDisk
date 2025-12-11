using System.ComponentModel;
using System.Diagnostics;

namespace CDCloser
{
    public partial class MainForm : Form
    {
        private BindingList<SelectedFile> burn_list = new();
        private BindingList<DriveInfo> drive_list = new();
        private BurnLogic burner = new BurnLogic();
        private CancellationTokenSource? burn_cts;
        private long disc_capacity = 0;
        private bool drive_ready = false;
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

            if(drive_list.Count == 0)
            {
                MessageBox.Show("No optical media drives detected", "Drive Selection Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                status_label.Text = "No optical drives found.";
                status_label.ForeColor = Color.Red;
            }
        }

        private async void start_burn(object sender, EventArgs e)
        {
            if (!drive_ready) return;
            set_ui_enabled(false);
            burn_cts = new CancellationTokenSource();
            cancel_button.Enabled = true;

            try
            {
                var recorders = burner.get_recorders();
                var recorder_id = recorders.Where(d => drive_box.SelectedItem.ToString() == d.Key).Select(i => i.Value).FirstOrDefault();
                if (String.IsNullOrEmpty(recorder_id)) throw new ArgumentNullException("Recorder ID should never be null here");
                burner.select_recorder(recorder_id);

                main_prog.Visible = true;
                main_prog.Style = ProgressBarStyle.Continuous;
                main_prog.Value = 0;
                status_label.Text = "Burning...";
                status_label.ForeColor = Color.Orange;
                
                //idk how this works honestly
                var progress = new Progress<BurnProgress>(p =>
                {
                    main_prog.Value = Math.Min(p.percent, 100);
                    status_label.Text = p.completed ? "Burn Complete" : $"Burning...{p.percent}%";
                });

                await burner.burn_files(burn_list.ToList(), disc_label_box.Text, close_media_checkbox.Checked, progress, burn_cts.Token);

                status_label.Text = "Burn Complete";
                status_label.ForeColor = Color.Green;
                MessageBox.Show("Burn Complete", "Success");
            }
            catch(OperationCanceledException)
            {
                status_label.Text = "Burn Cancelled";
                status_label.ForeColor = Color.Orange;
            }
            catch(Exception ex)
            {
                status_label.Text = "Burn Failed";
                status_label.ForeColor = Color.Red;
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Debug.WriteLine($"Error: {ex.Message}");
            }
            finally
            {
                cancel_button.Enabled = false;
                main_prog.Value = 0;
                burn_cts?.Dispose();
                burn_cts = null;
                set_ui_enabled(true);
            }

        }

        private void cancel_burn_clicked(object sender, EventArgs e)
        {
            burn_cts?.Cancel();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            burn_cts?.Cancel();
            burner.Dispose();
            base.OnFormClosing(e);
        }

        private void set_ui_enabled(bool enabled)
        {
            browse_button.Enabled = enabled;
            remove_button.Enabled = enabled;
            drive_refresh.Enabled = enabled;
            drive_box.Enabled = enabled;
            burn_button.Enabled = enabled;
            disc_label_box.Enabled = enabled;
            burn_speed_box.Enabled = enabled;
            file_grid.Enabled = enabled;
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
            if(!media_present)
            {
                status_label.Text = "Media not present.";
                status_label.ForeColor = Color.Red;
                aux_progress.Visible = false;
                return;
                
            }
            disc_capacity = await Task.Run(() => Globals.get_media_capacity(drive.Name));
            disk_cap_label.Text = Globals.to_human_readable(disc_capacity);
            if (disc_capacity > burn_list.Sum(f => f.byte_size))
            {
                status_label.Text = "Drive is ready.";
                status_label.ForeColor = Color.Green;
                aux_progress.Visible = false;
                drive_ready = true;
            }
            else
            {
                status_label.Text = "Drive is not ready.";
                status_label.ForeColor = Color.Red;
                aux_progress.Visible = false;
            }

        }

        

        private void drive_changed(object sender, EventArgs e)
        {
            drive_ready = false;
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
                if(disc_capacity == 0) check_drive(drive_box.SelectedIndex);
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



    }
}
