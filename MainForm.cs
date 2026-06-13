using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using NAudio.Wave;

namespace MusicPlayer
{
    public class MainForm : Form
    {
        // Window Dragging Support
        private bool isDraggingWindow = false;
        private Point dragCursorPoint;
        private Point dragFormPoint;

        // UI Panels
        private Panel topHeaderPanel = null!;
        private Label appLogoLabel = null!;
        private Label appTitleLabel = null!;
        private Panel windowControlsPanel = null!;
        private Button minimizeBtn = null!;
        private Button maximizeBtn = null!;
        private Button closeBtn = null!;

        // Fullscreen & Maximize State
        private bool isFullscreen = false;
        private Size normalSize;
        private Point normalLocation;

        private Panel sidebarPanel = null!;
        private Panel navIndicator = null!;
        private ModernButton navNowPlayingBtn = null!;
        private ModernButton navLibraryBtn = null!;
        private ModernButton navPlaylistsBtn = null!;
        private ModernButton navSettingsBtn = null!;

        private Panel contentPanel = null!;
        private Panel bottomPlaybackPanel = null!;

        // Bottom Deck Controls
        private CircularPictureBox miniAlbumArt = null!;
        private Label miniTitleLabel = null!;
        private Label miniArtistLabel = null!;
        private Button playPauseBtn = null!;
        private Button nextBtn = null!;
        private Button prevBtn = null!;
        private Button shuffleBtn = null!;
        private Button repeatBtn = null!;
        private ModernSlider trackProgressBar = null!;
        private Label timeElapsedLabel = null!;
        private Label timeTotalLabel = null!;
        private Label volumeIcon = null!;
        private ModernSlider volumeSlider = null!;

        // Sub-pages (Views) - lazy initialized
        private NowPlayingView? nowPlayingView;
        private LibraryView? libraryView;
        private PlaylistView? playlistView;
        private SettingsView? settingsView;

        // Playback Queue State
        private List<SongInfo> playQueue = new List<SongInfo>();
        private int queueIndex = -1;
        private bool isShuffle = false;
        private bool isRepeat = false;

        private System.Windows.Forms.Timer playbackProgressTimer = null!;
        private Color themeAccentColor = Color.FromArgb(0, 210, 255); // Sapphire default

        public MainForm()
        {
            InitializeComponent();
            
            // Wire AudioManager events
            AudioManager.Instance.PlaybackStateChanged += AudioManager_PlaybackStateChanged;
            AudioManager.Instance.TrackStarted += AudioManager_TrackStarted;
            AudioManager.Instance.PlaybackError += AudioManager_PlaybackError;
            AudioManager.Instance.TrackFinished += AudioManager_TrackFinished;

            // Timer for progress bar updates
            playbackProgressTimer = new System.Windows.Forms.Timer { Interval = 250 };
            playbackProgressTimer.Tick += PlaybackProgressTimer_Tick;
            playbackProgressTimer.Start();

            // Set initial page
            nowPlayingView = new NowPlayingView();
            ShowPage(nowPlayingView, navNowPlayingBtn);

            // Load theme color
            ApplyTheme(LibraryManager.Instance.CurrentTheme);

            // Scan library automatically on startup
            LibraryManager.Instance.ScanLibraryAsync();
        }

        private void InitializeComponent()
        {
            // Set Form Properties
            Size = new Size(920, 600);
            MinimumSize = new Size(880, 560);
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.FromArgb(12, 12, 14);
            Text = "Harmonix Music Player";
            DoubleBuffered = true;
            KeyPreview = true;

            // 1. Top Header Bar (Draggable)
            topHeaderPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 35,
                BackColor = Color.FromArgb(18, 18, 20)
            };
            topHeaderPanel.MouseDown += Header_MouseDown;
            topHeaderPanel.MouseMove += Header_MouseMove;
            topHeaderPanel.MouseUp += Header_MouseUp;
            topHeaderPanel.DoubleClick += Header_DoubleClick;

            appLogoLabel = new Label
            {
                Text = "⚡",
                ForeColor = themeAccentColor,
                Font = new Font("Segoe UI Semibold", 10.5f, FontStyle.Bold),
                Location = new Point(12, 8),
                AutoSize = true
            };

            appTitleLabel = new Label
            {
                Text = "HARMONIX",
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold),
                Location = new Point(32, 9),
                AutoSize = true
            };
            appTitleLabel.MouseDown += Header_MouseDown;
            appTitleLabel.MouseMove += Header_MouseMove;
            appTitleLabel.MouseUp += Header_MouseUp;
            appTitleLabel.DoubleClick += Header_DoubleClick;

            windowControlsPanel = new Panel
            {
                Dock = DockStyle.Right,
                Width = 115,
                BackColor = Color.Transparent
            };

            minimizeBtn = new Button
            {
                Text = "—",
                Size = new Size(35, 35),
                Location = new Point(5, 0),
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.DarkGray,
                Font = new Font("Segoe UI", 9)
            };
            minimizeBtn.FlatAppearance.BorderSize = 0;
            minimizeBtn.FlatAppearance.MouseOverBackColor = Color.FromArgb(40, 255, 255, 255);
            minimizeBtn.Click += (s, e) => WindowState = FormWindowState.Minimized;

            maximizeBtn = new Button
            {
                Text = "⬜",
                Size = new Size(35, 35),
                Location = new Point(40, 0),
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.DarkGray,
                Font = new Font("Segoe UI", 8.5f)
            };
            maximizeBtn.FlatAppearance.BorderSize = 0;
            maximizeBtn.FlatAppearance.MouseOverBackColor = Color.FromArgb(40, 255, 255, 255);
            maximizeBtn.Click += (s, e) => ToggleMaximize();

            closeBtn = new Button
            {
                Text = "✕",
                Size = new Size(35, 35),
                Location = new Point(75, 0),
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.DarkGray,
                Font = new Font("Segoe UI", 9.5f)
            };
            closeBtn.FlatAppearance.BorderSize = 0;
            closeBtn.FlatAppearance.MouseOverBackColor = Color.FromArgb(240, 70, 70);
            closeBtn.FlatAppearance.MouseDownBackColor = Color.FromArgb(200, 50, 50);
            closeBtn.Click += (s, e) => Close();

            windowControlsPanel.Controls.AddRange(new Control[] { minimizeBtn, maximizeBtn, closeBtn });
            topHeaderPanel.Controls.AddRange(new Control[] { appLogoLabel, appTitleLabel, windowControlsPanel });

            // 2. Sidebar Panel (Left Side Navigation)
            sidebarPanel = new Panel
            {
                Dock = DockStyle.Left,
                Width = 180,
                BackColor = Color.FromArgb(16, 16, 18)
            };

            navIndicator = new Panel
            {
                Width = 4,
                Height = 35,
                BackColor = themeAccentColor,
                Location = new Point(0, 50)
            };

            navNowPlayingBtn = CreateSidebarBtn("Now Playing", 50);
            navNowPlayingBtn.Click += (s, e) => {
                nowPlayingView ??= new NowPlayingView();
                ShowPage(nowPlayingView, navNowPlayingBtn);
            };

            navLibraryBtn = CreateSidebarBtn("Music Library", 95);
            navLibraryBtn.Click += (s, e) => {
                if (libraryView == null)
                {
                    libraryView = new LibraryView();
                    libraryView.PlayRequested += View_PlayRequested;
                    libraryView.ScanButton.BorderColor = themeAccentColor;
                }
                ShowPage(libraryView, navLibraryBtn);
            };

            navPlaylistsBtn = CreateSidebarBtn("Playlists", 140);
            navPlaylistsBtn.Click += (s, e) => {
                if (playlistView == null)
                {
                    playlistView = new PlaylistView();
                    playlistView.PlayRequested += View_PlayRequested;
                    playlistView.CreatePlaylistBtn.BorderColor = themeAccentColor;
                }
                ShowPage(playlistView, navPlaylistsBtn);
            };

            navSettingsBtn = CreateSidebarBtn("Settings", 185);
            navSettingsBtn.Click += (s, e) => {
                if (settingsView == null)
                {
                    settingsView = new SettingsView();
                    settingsView.ThemeChanged += (s, theme) => ApplyTheme(theme);
                    settingsView.AddFolderBtn.BorderColor = themeAccentColor;
                }
                ShowPage(settingsView, navSettingsBtn);
            };

            sidebarPanel.Controls.AddRange(new Control[] {
                navIndicator, navNowPlayingBtn, navLibraryBtn, navPlaylistsBtn, navSettingsBtn
            });

            // 3. Bottom Playback Panel
            bottomPlaybackPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 90,
                BackColor = Color.FromArgb(20, 20, 24)
            };

            // Left: Mini Track Information
            miniAlbumArt = new CircularPictureBox
            {
                Size = new Size(50, 50),
                Location = new Point(15, 20),
                BorderSize = 1.5f,
                BorderColor = themeAccentColor
            };

            miniTitleLabel = new Label
            {
                Text = "Not Playing",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(75, 25),
                AutoSize = true,
                MaximumSize = new Size(160, 20)
            };

            miniArtistLabel = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.FromArgb(150, 150, 155),
                Location = new Point(75, 45),
                AutoSize = true,
                MaximumSize = new Size(160, 20)
            };

            // Center: Playback Controls Deck
            int centerStartX = 300;
            prevBtn = CreateDeckBtn("⏮", centerStartX, 15, 30);
            prevBtn.Click += (s, e) => PrevTrack();

            playPauseBtn = CreateDeckBtn("▶", centerStartX + 40, 7, 45);
            playPauseBtn.Font = new Font("Segoe UI", 12.5f, FontStyle.Bold);
            playPauseBtn.Click += (s, e) => TogglePlayPause();

            nextBtn = CreateDeckBtn("⏭", centerStartX + 95, 15, 30);
            nextBtn.Click += (s, e) => NextTrack();

            shuffleBtn = CreateDeckBtn("🔀", centerStartX - 40, 18, 25);
            shuffleBtn.ForeColor = Color.FromArgb(120, 120, 125);
            shuffleBtn.Click += ShuffleBtn_Click;

            repeatBtn = CreateDeckBtn("🔁", centerStartX + 135, 18, 25);
            repeatBtn.ForeColor = Color.FromArgb(120, 120, 125);
            repeatBtn.Click += RepeatBtn_Click;

            // Center: Track Timeline Progress
            timeElapsedLabel = new Label
            {
                Text = "0:00",
                Font = new Font("Consolas", 8.5f),
                ForeColor = Color.DarkGray,
                Location = new Point(275, 60),
                Size = new Size(40, 15),
                TextAlign = ContentAlignment.MiddleRight
            };

            trackProgressBar = new ModernSlider
            {
                Location = new Point(320, 60),
                Width = 280,
                Height = 15,
                Minimum = 0,
                Maximum = 100,
                Value = 0,
                ProgressColor = themeAccentColor
            };
            trackProgressBar.Scroll += TrackProgressBar_Scroll;

            timeTotalLabel = new Label
            {
                Text = "0:00",
                Font = new Font("Consolas", 8.5f),
                ForeColor = Color.DarkGray,
                Location = new Point(605, 60),
                Size = new Size(40, 15),
                TextAlign = ContentAlignment.MiddleLeft
            };

            // Right: Volume Bar Controls
            volumeIcon = new Label
            {
                Text = "🔊",
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = Color.DarkGray,
                Location = new Point(740, 37),
                AutoSize = true
            };

            volumeSlider = new ModernSlider
            {
                Location = new Point(765, 35),
                Width = 100,
                Height = 15,
                Minimum = 0,
                Maximum = 1f,
                Value = AudioManager.Instance.Volume,
                ProgressColor = themeAccentColor
            };
            volumeSlider.ValueChanged += (s, e) => AudioManager.Instance.Volume = volumeSlider.Value;

            bottomPlaybackPanel.Controls.AddRange(new Control[] {
                miniAlbumArt, miniTitleLabel, miniArtistLabel,
                prevBtn, playPauseBtn, nextBtn, shuffleBtn, repeatBtn,
                timeElapsedLabel, trackProgressBar, timeTotalLabel,
                volumeIcon, volumeSlider
            });

            // 4. Content Frame (Center Panel)
            contentPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(12, 12, 14)
            };

            // Sub-views are lazy-loaded on navigation click

            // Add Panels to Window Container
            Controls.AddRange(new Control[] { contentPanel, sidebarPanel, bottomPlaybackPanel, topHeaderPanel });
        }

        private ModernButton CreateSidebarBtn(string text, int top)
        {
            return new ModernButton
            {
                Text = text,
                Location = new Point(10, top),
                Size = new Size(160, 35),
                BorderRadius = 10,
                ShowBorder = false,
                BackColor = Color.Transparent,
                ForeColor = Color.FromArgb(180, 180, 185),
                Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold),
                HoverColor = Color.FromArgb(12, 255, 255, 255),
                PressedColor = Color.FromArgb(20, 255, 255, 255)
            };
        }

        private Button CreateDeckBtn(string text, int left, int top, int size)
        {
            var btn = new Button
            {
                Text = text,
                Location = new Point(left, top),
                Size = new Size(size, size),
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10.5f)
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(20, 255, 255, 255);
            btn.FlatAppearance.MouseDownBackColor = Color.FromArgb(40, 255, 255, 255);
            return btn;
        }

        private void ShowPage(UserControl page, ModernButton activeNavBtn)
        {
            contentPanel.Controls.Clear();
            page.Dock = DockStyle.Fill;
            contentPanel.Controls.Add(page);

            // Reset navigation labels style
            navNowPlayingBtn.ForeColor = Color.FromArgb(180, 180, 185);
            navLibraryBtn.ForeColor = Color.FromArgb(180, 180, 185);
            navPlaylistsBtn.ForeColor = Color.FromArgb(180, 180, 185);
            navSettingsBtn.ForeColor = Color.FromArgb(180, 180, 185);

            activeNavBtn.ForeColor = themeAccentColor;
            
            // Animate Navigation Indicator Position
            navIndicator.Location = new Point(0, activeNavBtn.Location.Y);
        }

        private void View_PlayRequested(object? sender, List<SongInfo> queue)
        {
            if (queue == null || queue.Count == 0) return;

            playQueue = queue;
            queueIndex = 0;

            PlayCurrentTrack();
        }

        private void PlayCurrentTrack()
        {
            if (queueIndex >= 0 && queueIndex < playQueue.Count)
            {
                var track = playQueue[queueIndex];
                
                // Switch focus to Now Playing screen
                nowPlayingView ??= new NowPlayingView();
                ShowPage(nowPlayingView, navNowPlayingBtn);

                AudioManager.Instance.Play(track.FilePath);
            }
        }

        private void TogglePlayPause()
        {
            if (AudioManager.Instance.CurrentFilePath == null)
            {
                // Play first song in library if no song loaded
                if (LibraryManager.Instance.Songs.Count > 0)
                {
                    playQueue = new List<SongInfo>(LibraryManager.Instance.Songs);
                    queueIndex = 0;
                    PlayCurrentTrack();
                }
                return;
            }

            if (AudioManager.Instance.State == PlaybackState.Playing)
            {
                AudioManager.Instance.Pause();
            }
            else
            {
                AudioManager.Instance.Resume();
            }
        }

        private void NextTrack()
        {
            if (playQueue.Count == 0) return;

            if (isRepeat)
            {
                // Replay current song
                PlayCurrentTrack();
                return;
            }

            if (isShuffle)
            {
                // Generate a random queue index
                var rand = new Random();
                queueIndex = rand.Next(playQueue.Count);
            }
            else
            {
                queueIndex++;
                if (queueIndex >= playQueue.Count)
                {
                    queueIndex = 0; // Loop queue
                }
            }

            PlayCurrentTrack();
        }

        private void PrevTrack()
        {
            if (playQueue.Count == 0) return;

            // Restart track if played past 3 seconds
            if (AudioManager.Instance.CurrentTime > 3.0)
            {
                AudioManager.Instance.CurrentTime = 0;
                return;
            }

            queueIndex--;
            if (queueIndex < 0)
            {
                queueIndex = playQueue.Count - 1; // Loop to end
            }

            PlayCurrentTrack();
        }

        private void ShuffleBtn_Click(object? sender, EventArgs e)
        {
            isShuffle = !isShuffle;
            shuffleBtn.ForeColor = isShuffle ? themeAccentColor : Color.FromArgb(120, 120, 125);
        }

        private void RepeatBtn_Click(object? sender, EventArgs e)
        {
            isRepeat = !isRepeat;
            repeatBtn.ForeColor = isRepeat ? themeAccentColor : Color.FromArgb(120, 120, 125);
        }

        private void TrackProgressBar_Scroll(object? sender, EventArgs e)
        {
            // Update media playback timeline when user drags slider
            if (AudioManager.Instance.CurrentFilePath != null)
            {
                AudioManager.Instance.CurrentTime = trackProgressBar.Value;
            }
        }

        private void PlaybackProgressTimer_Tick(object? sender, EventArgs e)
        {
            if (AudioManager.Instance.CurrentFilePath != null && !trackProgressBar.IsDragging)
            {
                double current = AudioManager.Instance.CurrentTime;
                double total = AudioManager.Instance.TotalTime;

                trackProgressBar.Maximum = (float)total;
                trackProgressBar.Value = (float)current;

                timeElapsedLabel.Text = FormatTime(current);
                timeTotalLabel.Text = FormatTime(total);
            }
        }

        private string FormatTime(double seconds)
        {
            TimeSpan t = TimeSpan.FromSeconds(seconds);
            return t.Hours > 0 
                ? $"{t.Hours}:{t.Minutes:D2}:{t.Seconds:D2}" 
                : $"{t.Minutes}:{t.Seconds:D2}";
        }

        private void AudioManager_PlaybackStateChanged(object? sender, PlaybackState state)
        {
            SafeInvoke(() =>
            {
                if (state == PlaybackState.Playing)
                {
                    playPauseBtn.Text = "⏸";
                }
                else
                {
                    playPauseBtn.Text = "▶";
                }
            });
        }

        private void AudioManager_TrackStarted(object? sender, string filePath)
        {
            SafeInvoke(() =>
            {
                var song = LibraryManager.Instance.Songs.Find(s => s.FilePath.Equals(filePath, StringComparison.OrdinalIgnoreCase));
                if (song != null)
                {
                    miniTitleLabel.Text = song.Title;
                    miniArtistLabel.Text = song.Artist;
                }
                else
                {
                    miniTitleLabel.Text = Path.GetFileNameWithoutExtension(filePath);
                    miniArtistLabel.Text = "Unknown Artist";
                }

                // Update mini artwork thumbnail
                Image? art = LibraryManager.Instance.GetAlbumArt(filePath);
                
                Image? oldImg = miniAlbumArt.Image;
                miniAlbumArt.Image = art;
                oldImg?.Dispose();
            });
        }

        private void AudioManager_PlaybackError(object? sender, string message)
        {
            SafeInvoke(() => MessageBox.Show(message, "Harmonix Audio Error", MessageBoxButtons.OK, MessageBoxIcon.Error));
        }

        private void AudioManager_TrackFinished(object? sender, EventArgs e)
        {
            SafeInvoke(NextTrack);
        }

        private void ApplyTheme(string themeName)
        {
            switch (themeName)
            {
                case "Emerald Green":
                    themeAccentColor = Color.FromArgb(0, 245, 212);
                    break;
                case "Ruby Red":
                    themeAccentColor = Color.FromArgb(255, 0, 127);
                    break;
                case "Amethyst Purple":
                    themeAccentColor = Color.FromArgb(189, 0, 255);
                    break;
                default: // Neon Sapphire
                    themeAccentColor = Color.FromArgb(0, 210, 255);
                    break;
            }

            // Update app header highlight
            appLogoLabel.ForeColor = themeAccentColor;

            // Update bottom control bar elements
            miniAlbumArt.BorderColor = themeAccentColor;
            trackProgressBar.ProgressColor = themeAccentColor;
            volumeSlider.ProgressColor = themeAccentColor;

            if (isShuffle) shuffleBtn.ForeColor = themeAccentColor;
            if (isRepeat) repeatBtn.ForeColor = themeAccentColor;

            // Propagate to page navigation indicator
            navIndicator.BackColor = themeAccentColor;
            
            // Reset active button color styling
            if (contentPanel.Controls.Count > 0)
            {
                Control currentControl = contentPanel.Controls[0];
                if (nowPlayingView != null && currentControl == nowPlayingView) navNowPlayingBtn.ForeColor = themeAccentColor;
                else if (libraryView != null && currentControl == libraryView) navLibraryBtn.ForeColor = themeAccentColor;
                else if (playlistView != null && currentControl == playlistView) navPlaylistsBtn.ForeColor = themeAccentColor;
                else if (settingsView != null && currentControl == settingsView) navSettingsBtn.ForeColor = themeAccentColor;
            }

            // Propagate theme properties to sub-views
            if (nowPlayingView != null) nowPlayingView.Visualizer.ActiveColor = themeAccentColor;
            if (libraryView != null) libraryView.ScanButton.BorderColor = themeAccentColor;
            if (playlistView != null) playlistView.CreatePlaylistBtn.BorderColor = themeAccentColor;
            if (settingsView != null) settingsView.AddFolderBtn.BorderColor = themeAccentColor;
        }

        // Draggable Titlebar Logic
        private void Header_MouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                isDraggingWindow = true;
                dragCursorPoint = Cursor.Position;
                dragFormPoint = Location;
            }
        }

        private void Header_MouseMove(object? sender, MouseEventArgs e)
        {
            if (isDraggingWindow)
            {
                Point diff = Point.Subtract(Cursor.Position, new Size(dragCursorPoint));
                Location = Point.Add(dragFormPoint, new Size(diff));
            }
        }

        private void Header_MouseUp(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                isDraggingWindow = false;
            }
        }

        private void Header_DoubleClick(object? sender, EventArgs e)
        {
            ToggleMaximize();
        }

        private void ToggleMaximize()
        {
            if (WindowState == FormWindowState.Maximized)
            {
                WindowState = FormWindowState.Normal;
                maximizeBtn.Text = "⬜";
            }
            else
            {
                MaximizedBounds = Screen.FromControl(this).WorkingArea;
                WindowState = FormWindowState.Maximized;
                maximizeBtn.Text = "❐";
            }
        }

        private void ToggleFullscreen()
        {
            if (!isFullscreen)
            {
                isFullscreen = true;
                normalSize = Size;
                normalLocation = Location;

                // Hide sidebar and top header for immersive fullscreen
                topHeaderPanel.Visible = false;
                sidebarPanel.Visible = false;
                bottomPlaybackPanel.Visible = false; // Hide bottom deck for pure visualizer
                
                // Show Now Playing page exclusively
                nowPlayingView ??= new NowPlayingView();
                ShowPage(nowPlayingView, navNowPlayingBtn);

                WindowState = FormWindowState.Normal; // Reset first
                FormBorderStyle = FormBorderStyle.None;
                Bounds = Screen.FromControl(this).Bounds;
            }
            else
            {
                isFullscreen = false;
                topHeaderPanel.Visible = true;
                sidebarPanel.Visible = true;
                bottomPlaybackPanel.Visible = true;
                
                Bounds = new Rectangle(normalLocation, normalSize);
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F11)
            {
                ToggleFullscreen();
                e.Handled = true;
            }
            base.OnKeyDown(e);
        }

        protected override void WndProc(ref Message m)
        {
            const int WM_NCHITTEST = 0x84;
            const int HTCLIENT = 1;
            const int HTLEFT = 10;
            const int HTRIGHT = 11;
            const int HTTOP = 12;
            const int HTTOPLEFT = 13;
            const int HTTOPRIGHT = 14;
            const int HTBOTTOM = 15;
            const int HTBOTTOMLEFT = 16;
            const int HTBOTTOMRIGHT = 17;

            if (m.Msg == WM_NCHITTEST && WindowState == FormWindowState.Normal && !isFullscreen)
            {
                base.WndProc(ref m);
                if (m.Result.ToInt32() == HTCLIENT)
                {
                    Point pos = PointToClient(Cursor.Position);
                    int resizeBorder = 6;

                    if (pos.X <= resizeBorder && pos.Y <= resizeBorder)
                        m.Result = new IntPtr(HTTOPLEFT);
                    else if (pos.X >= ClientSize.Width - resizeBorder && pos.Y <= resizeBorder)
                        m.Result = new IntPtr(HTTOPRIGHT);
                    else if (pos.X <= resizeBorder && pos.Y >= ClientSize.Height - resizeBorder)
                        m.Result = new IntPtr(HTBOTTOMLEFT);
                    else if (pos.X >= ClientSize.Width - resizeBorder && pos.Y >= ClientSize.Height - resizeBorder)
                        m.Result = new IntPtr(HTBOTTOMRIGHT);
                    else if (pos.X <= resizeBorder)
                        m.Result = new IntPtr(HTLEFT);
                    else if (pos.X >= ClientSize.Width - resizeBorder)
                        m.Result = new IntPtr(HTRIGHT);
                    else if (pos.Y <= resizeBorder)
                        m.Result = new IntPtr(HTTOP);
                    else if (pos.Y >= ClientSize.Height - resizeBorder)
                        m.Result = new IntPtr(HTBOTTOM);
                }
                return;
            }
            base.WndProc(ref m);
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

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                playbackProgressTimer.Stop();
                playbackProgressTimer.Dispose();

                AudioManager.Instance.PlaybackStateChanged -= AudioManager_PlaybackStateChanged;
                AudioManager.Instance.TrackStarted -= AudioManager_TrackStarted;
                AudioManager.Instance.PlaybackError -= AudioManager_PlaybackError;
                AudioManager.Instance.TrackFinished -= AudioManager_TrackFinished;

                nowPlayingView?.Dispose();
                libraryView?.Dispose();
                playlistView?.Dispose();
                settingsView?.Dispose();

                // Clean up mini art resources
                Image? art = miniAlbumArt.Image;
                miniAlbumArt.Image = null;
                art?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
