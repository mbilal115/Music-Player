using System;
using System.Drawing;
using System.Windows.Forms;

namespace MusicPlayer
{
    public class SettingsView : UserControl
    {
        private Label titleLabel = null!;
        private Label foldersTitle = null!;
        private ListBox foldersListBox = null!;
        private ModernButton addFolderBtn = null!;
        private ModernButton removeFolderBtn = null!;

        private Label themeTitle = null!;
        private ComboBox themeComboBox = null!;
        private ModernButton scanNowBtn = null!;
        private Label scanStatusLabel = null!;

        public event EventHandler<string>? ThemeChanged;

        public ModernButton AddFolderBtn => addFolderBtn;

        public SettingsView()
        {
            InitializeComponent();
            LibraryManager.Instance.LibraryChanged += (s, e) => SafeInvoke(LoadFoldersList);
            LibraryManager.Instance.ScanProgress += (s, msg) => SafeInvoke(() => scanStatusLabel.Text = msg);

            LoadFoldersList();
            
            // Set initial theme selection
            themeComboBox.SelectedItem = LibraryManager.Instance.CurrentTheme;
        }

        private void InitializeComponent()
        {
            DoubleBuffered = true;
            Dock = DockStyle.Fill;
            BackColor = Color.FromArgb(12, 12, 14);

            titleLabel = new Label
            {
                Text = "Settings",
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(20, 15),
                AutoSize = true
            };

            foldersTitle = new Label
            {
                Text = "Monitored Music Folders",
                Font = new Font("Segoe UI Semibold", 11, FontStyle.Bold),
                ForeColor = Color.FromArgb(200, 200, 205),
                Location = new Point(20, 60),
                AutoSize = true
            };

            foldersListBox = new ListBox
            {
                Location = new Point(20, 90),
                Width = 450,
                Height = 150,
                BackColor = Color.FromArgb(18, 18, 20),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 9.5f),
                ItemHeight = 25
            };

            addFolderBtn = new ModernButton
            {
                Text = "Add Folder",
                Location = new Point(480, 90),
                Size = new Size(130, 32),
                BorderRadius = 10,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            addFolderBtn.Click += AddFolderBtn_Click;

            removeFolderBtn = new ModernButton
            {
                Text = "Remove Folder",
                Location = new Point(480, 130),
                Size = new Size(130, 32),
                BorderRadius = 10,
                BackColor = Color.FromArgb(40, 25, 25),
                BorderColor = Color.FromArgb(255, 75, 75),
                HoverColor = Color.FromArgb(40, 255, 75, 75),
                PressedColor = Color.FromArgb(80, 255, 75, 75),
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            removeFolderBtn.Click += RemoveFolderBtn_Click;

            themeTitle = new Label
            {
                Text = "Color Accent Theme",
                Font = new Font("Segoe UI Semibold", 11, FontStyle.Bold),
                ForeColor = Color.FromArgb(200, 200, 205),
                Location = new Point(20, 260),
                AutoSize = true
            };

            themeComboBox = new ComboBox
            {
                Location = new Point(20, 290),
                Width = 200,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.FromArgb(18, 18, 20),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10)
            };
            themeComboBox.Items.AddRange(new object[] { "Neon Sapphire", "Emerald Green", "Ruby Red", "Amethyst Purple" });
            themeComboBox.SelectedIndexChanged += ThemeComboBox_SelectedIndexChanged;

            scanNowBtn = new ModernButton
            {
                Text = "Force Full Re-Scan",
                Location = new Point(20, 340),
                Size = new Size(160, 35),
                BorderRadius = 12,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                BorderColor = Color.FromArgb(127, 0, 255), // purple border
                HoverColor = Color.FromArgb(40, 127, 0, 255),
                PressedColor = Color.FromArgb(80, 127, 0, 255)
            };
            scanNowBtn.Click += ScanNowBtn_Click;

            scanStatusLabel = new Label
            {
                ForeColor = Color.FromArgb(150, 150, 155),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Italic),
                Location = new Point(190, 348),
                AutoSize = true,
                Text = ""
            };

            Controls.AddRange(new Control[] {
                titleLabel, foldersTitle, foldersListBox, addFolderBtn, removeFolderBtn,
                themeTitle, themeComboBox, scanNowBtn, scanStatusLabel
            });
        }

        private void LoadFoldersList()
        {
            foldersListBox.BeginUpdate();
            foldersListBox.Items.Clear();
            foreach (var folder in LibraryManager.Instance.MusicFolders)
            {
                foldersListBox.Items.Add(folder);
            }
            foldersListBox.EndUpdate();
        }

        private void AddFolderBtn_Click(object? sender, EventArgs e)
        {
            using (var fbd = new FolderBrowserDialog())
            {
                fbd.Description = "Select a folder containing your music files:";
                fbd.UseDescriptionForTitle = true;
                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    LibraryManager.Instance.AddMusicFolder(fbd.SelectedPath);
                    LibraryManager.Instance.ScanLibraryAsync(); // Auto-scan new folder
                }
            }
        }

        private void RemoveFolderBtn_Click(object? sender, EventArgs e)
        {
            if (foldersListBox.SelectedItem != null)
            {
                string path = foldersListBox.SelectedItem.ToString() ?? "";
                var confirmResult = MessageBox.Show(
                    $"Are you sure you want to stop monitoring this folder? Songs inside it will be removed from your Library.",
                    "Remove Monitored Folder",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (confirmResult == DialogResult.Yes)
                {
                    LibraryManager.Instance.RemoveMusicFolder(path);
                }
            }
        }

        private void ThemeComboBox_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (themeComboBox.SelectedItem != null)
            {
                string theme = themeComboBox.SelectedItem.ToString() ?? "Neon Sapphire";
                LibraryManager.Instance.CurrentTheme = theme;
                ThemeChanged?.Invoke(this, theme);
            }
        }

        private void ScanNowBtn_Click(object? sender, EventArgs e)
        {
            LibraryManager.Instance.ScanLibraryAsync();
        }

        private void SafeInvoke(Action action)
        {
            if (InvokeRequired)
            {
                BeginInvoke(action);
            }
            else
            {
                action();
            }
        }
    }
}
