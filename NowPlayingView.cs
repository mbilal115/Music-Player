using System;
using System.Drawing;
using System.Windows.Forms;
using NAudio.Wave;

namespace MusicPlayer
{
    public class NowPlayingView : UserControl
    {
        private TableLayoutPanel layoutPanel = null!;
        private Panel leftPanel = null!;
        private CircularPictureBox albumArtBox = null!;
        private Label titleLabel = null!;
        private Label artistLabel = null!;
        private Label albumLabel = null!;

        private Panel rightPanel = null!;
        private Label visualizerTitle = null!;
        private AudioVisualizer visualizer = null!;

        public AudioVisualizer Visualizer => visualizer;

        private System.Windows.Forms.Timer updateTimer = null!;
        private float rotationSpeed = 0.6f;

        public NowPlayingView()
        {
            InitializeComponent();
            
            // Listen to playback state changes to start/stop animations if needed
            AudioManager.Instance.TrackStarted += AudioManager_TrackStarted;
            AudioManager.Instance.PlaybackStateChanged += AudioManager_PlaybackStateChanged;

            // Load initial track if already playing
            if (AudioManager.Instance.CurrentFilePath != null)
            {
                UpdateTrackDetails(AudioManager.Instance.CurrentFilePath);
            }

            // Start visualizer timer
            updateTimer = new System.Windows.Forms.Timer { Interval = 16 }; // ~60 FPS
            updateTimer.Tick += UpdateTimer_Tick;
            updateTimer.Start();
        }

        private void InitializeComponent()
        {
            DoubleBuffered = true;
            Dock = DockStyle.Fill;
            BackColor = Color.FromArgb(12, 12, 14);

            layoutPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.Transparent
            };
            layoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F)); // Left pane: Album art (40%)
            layoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60F)); // Right pane: Visualizer (60%)

            // Left Panel Setup
            leftPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20)
            };

            albumArtBox = new CircularPictureBox
            {
                Size = new Size(220, 220),
                Location = new Point(40, 30),
                BorderSize = 3.5f,
                BorderColor = Color.FromArgb(0, 210, 255),
                Image = null // Uses fallback music note icon initially
            };
            // Dynamic centering in leftPanel
            leftPanel.SizeChanged += (s, e) => {
                albumArtBox.Location = new Point(
                    (leftPanel.Width - albumArtBox.Width) / 2,
                    30
                );
                CenterLabels();
            };

            titleLabel = new Label
            {
                Text = "No Track Playing",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleCenter,
                AutoSize = false,
                Width = 280,
                Height = 35
            };

            artistLabel = new Label
            {
                Text = "Choose a song to start",
                Font = new Font("Segoe UI", 11, FontStyle.Regular),
                ForeColor = Color.FromArgb(0, 210, 255), // neon accent
                TextAlign = ContentAlignment.MiddleCenter,
                AutoSize = false,
                Width = 280,
                Height = 25
            };

            albumLabel = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Italic),
                ForeColor = Color.FromArgb(150, 150, 155),
                TextAlign = ContentAlignment.MiddleCenter,
                AutoSize = false,
                Width = 280,
                Height = 25
            };

            leftPanel.Controls.AddRange(new Control[] { albumArtBox, titleLabel, artistLabel, albumLabel });

            // Right Panel Setup
            rightPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20, 30, 20, 20)
            };

            visualizerTitle = new Label
            {
                Text = "LIVE SPECTRUM ANALYZER",
                Font = new Font("Segoe UI Semibold", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 100, 105),
                Location = new Point(20, 15),
                AutoSize = true
            };

            visualizer = new AudioVisualizer
            {
                Location = new Point(20, 40),
                Size = new Size(400, 280),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                BarCount = 48,
                ActiveColor = Color.FromArgb(0, 210, 255),
                HighlightColor = Color.FromArgb(127, 0, 255)
            };

            rightPanel.Controls.AddRange(new Control[] { visualizerTitle, visualizer });

            layoutPanel.Controls.Add(leftPanel, 0, 0);
            layoutPanel.Controls.Add(rightPanel, 1, 0);

            Controls.Add(layoutPanel);
        }

        private void CenterLabels()
        {
            int centerWidth = leftPanel.Width;
            titleLabel.Width = centerWidth - 40;
            titleLabel.Location = new Point(20, albumArtBox.Bottom + 25);

            artistLabel.Width = centerWidth - 40;
            artistLabel.Location = new Point(20, titleLabel.Bottom + 5);

            albumLabel.Width = centerWidth - 40;
            albumLabel.Location = new Point(20, artistLabel.Bottom + 5);
        }

        private void UpdateTimer_Tick(object? sender, EventArgs e)
        {
            // Update Visualizer magnitudes
            if (AudioManager.Instance.State == PlaybackState.Playing)
            {
                float[] magnitudes = AudioManager.Instance.GetVisualizerData();
                visualizer.UpdateData(magnitudes);

                // Spin the record
                albumArtBox.RotationAngle = (albumArtBox.RotationAngle + rotationSpeed) % 360f;
            }
        }

        private void AudioManager_TrackStarted(object? sender, string filePath)
        {
            SafeInvoke(() => UpdateTrackDetails(filePath));
        }

        private void AudioManager_PlaybackStateChanged(object? sender, PlaybackState state)
        {
            // Optional state response
        }

        private void UpdateTrackDetails(string filePath)
        {
            // Try to load cached song details
            var song = LibraryManager.Instance.Songs.Find(s => s.FilePath.Equals(filePath, StringComparison.OrdinalIgnoreCase));
            if (song != null)
            {
                titleLabel.Text = song.Title;
                artistLabel.Text = song.Artist;
                albumLabel.Text = song.Album;
            }
            else
            {
                titleLabel.Text = System.IO.Path.GetFileNameWithoutExtension(filePath);
                artistLabel.Text = "Unknown Artist";
                albumLabel.Text = "Unknown Album";
            }

            // Load album art
            Image? art = LibraryManager.Instance.GetAlbumArt(filePath);
            
            // Dispose previous image if any
            Image? oldImage = albumArtBox.Image;
            albumArtBox.Image = art;
            oldImage?.Dispose();

            albumArtBox.RotationAngle = 0; // reset spin
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
                updateTimer.Stop();
                updateTimer.Dispose();
                AudioManager.Instance.TrackStarted -= AudioManager_TrackStarted;
                AudioManager.Instance.PlaybackStateChanged -= AudioManager_PlaybackStateChanged;

                // Safely clean up album art bitmap
                Image? art = albumArtBox.Image;
                albumArtBox.Image = null;
                art?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
