using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace MusicPlayer
{
    public class LibraryView : UserControl
    {
        private Panel headerPanel = null!;
        private Label titleLabel = null!;
        private Panel searchContainer = null!;
        private TextBox searchBox = null!;
        private ModernButton scanButton = null!;
        private ModernListView songListView = null!;
        private ContextMenuStrip contextMenu = null!;
        private Label statusLabel = null!;

        private List<SongInfo> displayedSongs = new List<SongInfo>();

        public event EventHandler<List<SongInfo>>? PlayRequested;

        public ModernButton ScanButton => scanButton;

        public LibraryView()
        {
            InitializeComponent();
            
            LibraryManager.Instance.LibraryChanged += (s, e) => SafeInvoke(RefreshLibrary);
            LibraryManager.Instance.ScanProgress += (s, msg) => SafeInvoke(() => statusLabel.Text = msg);

            RefreshLibrary();
        }

        private void InitializeComponent()
        {
            DoubleBuffered = true;
            Dock = DockStyle.Fill;
            BackColor = Color.FromArgb(12, 12, 14);

            // Header Panel
            headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                BackColor = Color.FromArgb(12, 12, 14)
            };

            titleLabel = new Label
            {
                Text = "Your Library",
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(20, 15),
                AutoSize = true
            };

            // Custom Styled Search Box Container
            searchContainer = new Panel
            {
                BackColor = Color.FromArgb(24, 24, 28),
                Size = new Size(250, 30),
                Location = new Point(190, 15),
                Padding = new Padding(8, 6, 8, 6)
            };

            searchBox = new TextBox
            {
                BorderStyle = BorderStyle.None,
                BackColor = Color.FromArgb(24, 24, 28),
                ForeColor = Color.FromArgb(200, 200, 200),
                Font = new Font("Segoe UI", 10),
                Width = 234,
                Text = "Search library..."
            };
            searchBox.GotFocus += SearchBox_GotFocus;
            searchBox.LostFocus += SearchBox_LostFocus;
            searchBox.TextChanged += SearchBox_TextChanged;
            searchContainer.Controls.Add(searchBox);

            scanButton = new ModernButton
            {
                Text = "Scan Library",
                Location = new Point(460, 12),
                Size = new Size(110, 32),
                BorderRadius = 10,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            scanButton.Click += ScanButton_Click;

            statusLabel = new Label
            {
                ForeColor = Color.FromArgb(120, 120, 125),
                Font = new Font("Segoe UI", 9, FontStyle.Italic),
                Location = new Point(585, 20),
                AutoSize = true,
                Text = ""
            };

            headerPanel.Controls.AddRange(new Control[] { titleLabel, searchContainer, scanButton, statusLabel });

            // Context Menu
            contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("Play", null, MenuPlay_Click);
            
            // Add to Playlist Submenu
            var addToPlaylistMenu = new ToolStripMenuItem("Add to Playlist");
            addToPlaylistMenu.DropDownOpening += AddToPlaylistMenu_DropDownOpening;
            contextMenu.Items.Add(addToPlaylistMenu);
            
            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add("Delete from System", null, MenuDelete_Click);

            // Modern ListView
            songListView = new ModernListView
            {
                Dock = DockStyle.Fill,
                ContextMenuStrip = contextMenu,
                Font = new Font("Segoe UI", 10)
            };
            
            songListView.Columns.Add("#", 40);
            songListView.Columns.Add("Title", 220);
            songListView.Columns.Add("Artist", 180);
            songListView.Columns.Add("Album", 180);
            songListView.Columns.Add("Duration", 80);
            
            songListView.DoubleClick += SongListView_DoubleClick;

            Controls.AddRange(new Control[] { songListView, headerPanel });
        }

        private void SearchBox_GotFocus(object? sender, EventArgs e)
        {
            if (searchBox.Text == "Search library...")
            {
                searchBox.Text = "";
                searchBox.ForeColor = Color.White;
            }
        }

        private void SearchBox_LostFocus(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(searchBox.Text))
            {
                searchBox.Text = "Search library...";
                searchBox.ForeColor = Color.FromArgb(120, 120, 125);
            }
        }

        private void SearchBox_TextChanged(object? sender, EventArgs e)
        {
            FilterSongs();
        }

        private void ScanButton_Click(object? sender, EventArgs e)
        {
            LibraryManager.Instance.ScanLibraryAsync();
        }

        private void FilterSongs()
        {
            string query = searchBox.Text.Trim().ToLower();
            if (query == "search library..." || string.IsNullOrWhiteSpace(query))
            {
                PopulateListView(LibraryManager.Instance.Songs);
                return;
            }

            var filtered = LibraryManager.Instance.Songs.Where(s =>
                s.Title.ToLower().Contains(query) ||
                s.Artist.ToLower().Contains(query) ||
                s.Album.ToLower().Contains(query)
            ).ToList();

            PopulateListView(filtered);
        }

        private void RefreshLibrary()
        {
            FilterSongs();
        }

        private void PopulateListView(List<SongInfo> songs)
        {
            songListView.BeginUpdate();
            songListView.Items.Clear();
            displayedSongs = songs;

            int index = 1;
            foreach (var song in songs)
            {
                var item = new ListViewItem(index.ToString());
                item.SubItems.Add(song.Title);
                item.SubItems.Add(song.Artist);
                item.SubItems.Add(song.Album);
                item.SubItems.Add(song.DisplayDuration);
                item.Tag = song;

                songListView.Items.Add(item);
                index++;
            }
            songListView.EndUpdate();
        }

        private void SongListView_DoubleClick(object? sender, EventArgs e)
        {
            TriggerSelectedPlay();
        }

        private void TriggerSelectedPlay()
        {
            if (songListView.SelectedItems.Count > 0)
            {
                var selectedItem = songListView.SelectedItems[0];
                if (selectedItem.Tag is SongInfo song)
                {
                    // Find index in displayed songs list
                    int index = displayedSongs.IndexOf(song);
                    if (index >= 0)
                    {
                        // Play the selected song and pass the remaining queue to the request
                        var playQueue = displayedSongs.Skip(index).ToList();
                        PlayRequested?.Invoke(this, playQueue);
                    }
                }
            }
        }

        private void MenuPlay_Click(object? sender, EventArgs e)
        {
            TriggerSelectedPlay();
        }

        private void AddToPlaylistMenu_DropDownOpening(object? sender, EventArgs e)
        {
            if (sender is ToolStripMenuItem menuItem)
            {
                menuItem.DropDownItems.Clear();

                // Dynamic listing of playlists
                var playlists = LibraryManager.Instance.Playlists;
                foreach (var playlist in playlists)
                {
                    var item = new ToolStripMenuItem(playlist.Name);
                    item.Click += (s, ev) => AddSelectedToPlaylist(playlist.Name);
                    menuItem.DropDownItems.Add(item);
                }

                if (menuItem.DropDownItems.Count > 0)
                {
                    menuItem.DropDownItems.Add(new ToolStripSeparator());
                }

                var createNewItem = new ToolStripMenuItem("New Playlist...");
                createNewItem.Click += CreateNewPlaylistAndAddSelected;
                menuItem.DropDownItems.Add(createNewItem);
            }
        }

        private void AddSelectedToPlaylist(string playlistName)
        {
            if (songListView.SelectedItems.Count > 0)
            {
                foreach (ListViewItem item in songListView.SelectedItems)
                {
                    if (item.Tag is SongInfo song)
                    {
                        LibraryManager.Instance.AddSongToPlaylist(playlistName, song.FilePath);
                    }
                }
                MessageBox.Show($"Songs added to playlist '{playlistName}'", "Harmonix", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void CreateNewPlaylistAndAddSelected(object? sender, EventArgs e)
        {
            string name = PromptDialog.ShowDialog("Enter new playlist name:", "New Playlist");
            if (!string.IsNullOrWhiteSpace(name))
            {
                LibraryManager.Instance.CreatePlaylist(name);
                AddSelectedToPlaylist(name);
            }
        }

        private void MenuDelete_Click(object? sender, EventArgs e)
        {
            if (songListView.SelectedItems.Count > 0)
            {
                var confirmResult = MessageBox.Show(
                    "Are you sure you want to delete the selected song(s) from your hard drive? This cannot be undone.",
                    "Confirm Deletion",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (confirmResult == DialogResult.Yes)
                {
                    foreach (ListViewItem item in songListView.SelectedItems)
                    {
                        if (item.Tag is SongInfo song)
                        {
                            try
                            {
                                // Stop playback if current song is being deleted
                                if (AudioManager.Instance.CurrentFilePath == song.FilePath)
                                {
                                    AudioManager.Instance.Stop();
                                }

                                if (File.Exists(song.FilePath))
                                {
                                    File.Delete(song.FilePath);
                                }
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show($"Error deleting file: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        }
                    }

                    // Re-scan library to synchronize DB
                    LibraryManager.Instance.ScanLibrary();
                }
            }
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

    // Helper class for displaying a fast modal string prompt
    public static class PromptDialog
    {
        public static string ShowDialog(string text, string caption)
        {
            Form prompt = new Form()
            {
                Width = 400,
                Height = 160,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = caption,
                StartPosition = FormStartPosition.CenterParent,
                BackColor = Color.FromArgb(20, 20, 24),
                ForeColor = Color.White
            };

            Label textLabel = new Label() { Left = 20, Top = 15, Text = text, Width = 350, Font = new Font("Segoe UI", 10) };
            TextBox textBox = new TextBox() { Left = 20, Top = 45, Width = 340, Font = new Font("Segoe UI", 10), BackColor = Color.FromArgb(30, 30, 35), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
            Button confirmation = new Button() { Text = "Ok", Left = 260, Width = 100, Top = 80, DialogResult = DialogResult.OK, Font = new Font("Segoe UI", 9) };
            
            confirmation.Click += (sender, e) => { prompt.Close(); };
            prompt.Controls.Add(textBox);
            prompt.Controls.Add(confirmation);
            prompt.Controls.Add(textLabel);
            prompt.AcceptButton = confirmation;

            return prompt.ShowDialog() == DialogResult.OK ? textBox.Text : "";
        }
    }
}
