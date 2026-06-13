using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace MusicPlayer
{
    public class PlaylistView : UserControl
    {
        private Panel sidebarPanel = null!;
        private Label sidebarTitle = null!;
        private ListBox playlistListBox = null!;
        private Panel sidebarButtonsPanel = null!;
        private ModernButton createPlaylistBtn = null!;
        private ModernButton deletePlaylistBtn = null!;

        private Panel mainPanel = null!;
        private Label playlistTitleLabel = null!;
        private ModernListView songListView = null!;
        private ContextMenuStrip playlistContextMenu = null!;

        private List<SongInfo> displayedSongs = new List<SongInfo>();
        private string selectedPlaylistName = string.Empty;

        public event EventHandler<List<SongInfo>>? PlayRequested;

        public ModernButton CreatePlaylistBtn => createPlaylistBtn;

        public PlaylistView()
        {
            InitializeComponent();
            LibraryManager.Instance.LibraryChanged += (s, e) => SafeInvoke(RefreshPlaylists);
            RefreshPlaylists();
        }

        private void InitializeComponent()
        {
            DoubleBuffered = true;
            Dock = DockStyle.Fill;
            BackColor = Color.FromArgb(12, 12, 14);

            // 1. Sidebar Panel (Playlists List)
            sidebarPanel = new Panel
            {
                Dock = DockStyle.Left,
                Width = 200,
                BackColor = Color.FromArgb(18, 18, 20)
            };

            sidebarTitle = new Label
            {
                Text = "Playlists",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(15, 15),
                AutoSize = true
            };

            playlistListBox = new ListBox
            {
                Location = new Point(0, 50),
                Width = 200,
                Height = 350,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left,
                BackColor = Color.FromArgb(18, 18, 20),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 10),
                DrawMode = DrawMode.OwnerDrawFixed,
                ItemHeight = 35
            };
            playlistListBox.DrawItem += PlaylistListBox_DrawItem;
            playlistListBox.SelectedIndexChanged += PlaylistListBox_SelectedIndexChanged;

            sidebarButtonsPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 100,
                BackColor = Color.FromArgb(18, 18, 20)
            };

            createPlaylistBtn = new ModernButton
            {
                Text = "New Playlist",
                Location = new Point(15, 10),
                Size = new Size(170, 32),
                BorderRadius = 10,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            createPlaylistBtn.Click += CreatePlaylistBtn_Click;

            deletePlaylistBtn = new ModernButton
            {
                Text = "Delete Playlist",
                Location = new Point(15, 50),
                Size = new Size(170, 32),
                BorderRadius = 10,
                BackColor = Color.FromArgb(40, 25, 25),
                BorderColor = Color.FromArgb(255, 75, 75),
                HoverColor = Color.FromArgb(40, 255, 75, 75),
                PressedColor = Color.FromArgb(80, 255, 75, 75),
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            deletePlaylistBtn.Click += DeletePlaylistBtn_Click;

            sidebarButtonsPanel.Controls.AddRange(new Control[] { createPlaylistBtn, deletePlaylistBtn });
            sidebarPanel.Controls.AddRange(new Control[] { sidebarTitle, playlistListBox, sidebarButtonsPanel });

            // 2. Main Panel (Playlist Songs Grid)
            mainPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(12, 12, 14)
            };

            playlistTitleLabel = new Label
            {
                Text = "Select a Playlist",
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(20, 15),
                AutoSize = true
            };

            // Playlist Item Context Menu
            playlistContextMenu = new ContextMenuStrip();
            playlistContextMenu.Items.Add("Play", null, MenuPlay_Click);
            playlistContextMenu.Items.Add("Remove from Playlist", null, MenuRemove_Click);

            songListView = new ModernListView
            {
                Location = new Point(20, 60),
                Size = new Size(Width - 240, Height - 80),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                ContextMenuStrip = playlistContextMenu,
                Font = new Font("Segoe UI", 10)
            };

            songListView.Columns.Add("#", 40);
            songListView.Columns.Add("Title", 220);
            songListView.Columns.Add("Artist", 180);
            songListView.Columns.Add("Album", 180);
            songListView.Columns.Add("Duration", 80);

            songListView.DoubleClick += SongListView_DoubleClick;

            mainPanel.Controls.AddRange(new Control[] { playlistTitleLabel, songListView });

            Controls.AddRange(new Control[] { mainPanel, sidebarPanel });
        }

        private void PlaylistListBox_DrawItem(object? sender, DrawItemEventArgs e)
        {
            if (e.Index < 0 || e.Index >= playlistListBox.Items.Count) return;

            Graphics g = e.Graphics;
            Rectangle rect = e.Bounds;
            string playlistName = playlistListBox.Items[e.Index]?.ToString() ?? "";

            bool isSelected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;

            // Paint background
            Color bg = isSelected ? Color.FromArgb(30, 0, 210, 255) : Color.FromArgb(18, 18, 20);
            using (SolidBrush bgBrush = new SolidBrush(bg))
            {
                g.FillRectangle(bgBrush, rect);
            }

            // Paint border line for items
            using (Pen borderPen = new Pen(Color.FromArgb(10, 255, 255, 255)))
            {
                g.DrawLine(borderPen, rect.Left, rect.Bottom - 1, rect.Right, rect.Bottom - 1);
            }

            // Draw text
            Color fg = isSelected ? Color.FromArgb(0, 210, 255) : Color.FromArgb(190, 190, 195);
            TextFormatFlags flags = TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis;
            Rectangle textRect = new Rectangle(rect.X + 20, rect.Y, rect.Width - 30, rect.Height);

            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            TextRenderer.DrawText(g, playlistName, playlistListBox.Font, textRect, fg, flags);
        }

        private void PlaylistListBox_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (playlistListBox.SelectedItem != null)
            {
                selectedPlaylistName = playlistListBox.SelectedItem.ToString() ?? "";
                playlistTitleLabel.Text = selectedPlaylistName;
                LoadPlaylistSongs(selectedPlaylistName);
            }
            else
            {
                selectedPlaylistName = string.Empty;
                playlistTitleLabel.Text = "Select a Playlist";
                songListView.Items.Clear();
                displayedSongs.Clear();
            }
        }

        private void RefreshPlaylists()
        {
            playlistListBox.BeginUpdate();
            string? currentSelection = playlistListBox.SelectedItem?.ToString();
            playlistListBox.Items.Clear();

            foreach (var playlist in LibraryManager.Instance.Playlists)
            {
                playlistListBox.Items.Add(playlist.Name);
            }

            if (currentSelection != null && playlistListBox.Items.Contains(currentSelection))
            {
                playlistListBox.SelectedItem = currentSelection;
            }
            else if (playlistListBox.Items.Count > 0)
            {
                playlistListBox.SelectedIndex = 0;
            }
            playlistListBox.EndUpdate();
        }

        private void LoadPlaylistSongs(string playlistName)
        {
            var playlist = LibraryManager.Instance.Playlists.FirstOrDefault(p => p.Name.Equals(playlistName, StringComparison.OrdinalIgnoreCase));
            if (playlist == null) return;

            songListView.BeginUpdate();
            songListView.Items.Clear();
            displayedSongs.Clear();

            int index = 1;
            foreach (var path in playlist.SongPaths)
            {
                // Find song tags in Library
                var song = LibraryManager.Instance.Songs.FirstOrDefault(s => s.FilePath.Equals(path, StringComparison.OrdinalIgnoreCase));
                
                // If song metadata is not scanned yet, make a temporary entry
                if (song == null)
                {
                    song = new SongInfo
                    {
                        FilePath = path,
                        Title = System.IO.Path.GetFileNameWithoutExtension(path),
                        Artist = "Unknown Artist",
                        Album = "Unknown Album",
                        Duration = 0
                    };
                }

                displayedSongs.Add(song);

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
                    int index = displayedSongs.IndexOf(song);
                    if (index >= 0)
                    {
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

        private void MenuRemove_Click(object? sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(selectedPlaylistName) && songListView.SelectedItems.Count > 0)
            {
                foreach (ListViewItem item in songListView.SelectedItems)
                {
                    if (item.Tag is SongInfo song)
                    {
                        LibraryManager.Instance.RemoveSongFromPlaylist(selectedPlaylistName, song.FilePath);
                    }
                }
                LoadPlaylistSongs(selectedPlaylistName);
            }
        }

        private void CreatePlaylistBtn_Click(object? sender, EventArgs e)
        {
            string name = PromptDialog.ShowDialog("Enter playlist name:", "New Playlist");
            if (!string.IsNullOrWhiteSpace(name))
            {
                LibraryManager.Instance.CreatePlaylist(name);
                RefreshPlaylists();
                playlistListBox.SelectedItem = name;
            }
        }

        private void DeletePlaylistBtn_Click(object? sender, EventArgs e)
        {
            if (playlistListBox.SelectedItem != null)
            {
                string name = playlistListBox.SelectedItem.ToString() ?? "";
                var confirmResult = MessageBox.Show(
                    $"Are you sure you want to delete the playlist '{name}'?",
                    "Confirm Playlist Deletion",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (confirmResult == DialogResult.Yes)
                {
                    LibraryManager.Instance.DeletePlaylist(name);
                    RefreshPlaylists();
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
}
