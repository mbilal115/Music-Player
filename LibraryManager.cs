using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Drawing;

namespace MusicPlayer
{
    public class SongInfo
    {
        public string FilePath { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Artist { get; set; } = string.Empty;
        public string Album { get; set; } = string.Empty;
        public double Duration { get; set; }
        public string Genre { get; set; } = string.Empty;
        public int Year { get; set; }
        public bool HasAlbumArt { get; set; }

        public string DisplayDuration
        {
            get
            {
                TimeSpan t = TimeSpan.FromSeconds(Duration);
                return t.Hours > 0 
                    ? $"{t.Hours}:{t.Minutes:D2}:{t.Seconds:D2}" 
                    : $"{t.Minutes}:{t.Seconds:D2}";
            }
        }
    }

    public class PlaylistInfo
    {
        public string Name { get; set; } = string.Empty;
        public List<string> SongPaths { get; set; } = new List<string>();
    }

    public class LibraryData
    {
        public List<string> MusicFolders { get; set; } = new List<string>();
        public List<SongInfo> Songs { get; set; } = new List<SongInfo>();
        public List<PlaylistInfo> Playlists { get; set; } = new List<PlaylistInfo>();
        public string Theme { get; set; } = "Dark";
    }

    public class LibraryManager
    {
        private static LibraryManager? instance;
        public static LibraryManager Instance => instance ??= new LibraryManager();

        private LibraryData data = new LibraryData();
        private readonly string dbPath;

        public List<SongInfo> Songs => data.Songs;
        public List<PlaylistInfo> Playlists => data.Playlists;
        public List<string> MusicFolders => data.MusicFolders;
        public string CurrentTheme
        {
            get => data.Theme;
            set { data.Theme = value; SaveDatabase(); }
        }

        public event EventHandler? LibraryChanged;
        public event EventHandler<string>? ScanProgress;

        private LibraryManager()
        {
     
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string appFolder = Path.Combine(appData, "Harmonix");
            Directory.CreateDirectory(appFolder);
            dbPath = Path.Combine(appFolder, "library.json");

            LoadDatabase();

          
            if (data.MusicFolders.Count == 0)
            {
                string defaultMusic = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
                if (Directory.Exists(defaultMusic))
                {
                    data.MusicFolders.Add(defaultMusic);
                }
            }
        }

        public void LoadDatabase()
        {
            try
            {
                if (File.Exists(dbPath))
                {
                    string json = File.ReadAllText(dbPath);
                    var loaded = JsonSerializer.Deserialize<LibraryData>(json);
                    if (loaded != null)
                    {
                        data = loaded;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load library: {ex.Message}");
                data = new LibraryData();
            }
        }

        public void SaveDatabase()
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(data, options);
                File.WriteAllText(dbPath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save library: {ex.Message}");
            }
        }

        public void AddMusicFolder(string path)
        {
            if (Directory.Exists(path) && !data.MusicFolders.Contains(path))
            {
                data.MusicFolders.Add(path);
                SaveDatabase();
                LibraryChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public void RemoveMusicFolder(string path)
        {
            if (data.MusicFolders.Remove(path))
            {
               
                data.Songs.RemoveAll(s => s.FilePath.StartsWith(path, StringComparison.OrdinalIgnoreCase));
                
               
                foreach (var playlist in data.Playlists)
                {
                    playlist.SongPaths.RemoveAll(p => p.StartsWith(path, StringComparison.OrdinalIgnoreCase));
                }

                SaveDatabase();
                LibraryChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public void ScanLibraryAsync()
        {
            System.Threading.Tasks.Task.Run(() =>
            {
                ScanLibrary();
            });
        }

        public void ScanLibrary()
        {
            ScanProgress?.Invoke(this, "Starting library scan...");
            
            var existingPaths = new HashSet<string>(data.Songs.Select(s => s.FilePath), StringComparer.OrdinalIgnoreCase);
            var scannedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var newSongs = new List<SongInfo>();

            string[] extensions = { ".mp3", ".wav", ".m4a", ".wma", ".flac", ".aac" };

            int filesProcessed = 0;

            foreach (var folder in data.MusicFolders)
            {
                if (!Directory.Exists(folder)) continue;

                try
                {
                    var files = Directory.EnumerateFiles(folder, "*.*", SearchOption.AllDirectories)
                        .Where(file => extensions.Contains(Path.GetExtension(file).ToLower()));

                    foreach (var file in files)
                    {
                        scannedPaths.Add(file);
                        filesProcessed++;

                        if (filesProcessed % 10 == 0)
                        {
                            ScanProgress?.Invoke(this, $"Scanning: Found {filesProcessed} songs...");
                        }

                        if (existingPaths.Contains(file))
                        {
                            // Keep existing scanned song from cache
                            var cachedSong = data.Songs.First(s => s.FilePath.Equals(file, StringComparison.OrdinalIgnoreCase));
                            newSongs.Add(cachedSong);
                        }
                        else
                        {
                            // Scan new song tags
                            var song = ParseSongTags(file);
                            if (song != null)
                            {
                                newSongs.Add(song);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed scanning folder {folder}: {ex.Message}");
                }
            }

            // Keep only files that still exist
            data.Songs = newSongs;

            // Remove non-existent files from playlists too
            foreach (var playlist in data.Playlists)
            {
                playlist.SongPaths.RemoveAll(p => !File.Exists(p));
            }

            SaveDatabase();
            ScanProgress?.Invoke(this, $"Scan finished! {data.Songs.Count} tracks loaded.");
            LibraryChanged?.Invoke(this, EventArgs.Empty);
        }

        private SongInfo? ParseSongTags(string filePath)
        {
            try
            {
                var song = new SongInfo { FilePath = filePath };
                
                using (var tfile = TagLib.File.Create(filePath))
                {
                    song.Title = string.IsNullOrEmpty(tfile.Tag.Title) 
                        ? Path.GetFileNameWithoutExtension(filePath) 
                        : tfile.Tag.Title;

                    song.Artist = string.IsNullOrEmpty(tfile.Tag.FirstArtist) 
                        ? "Unknown Artist" 
                        : tfile.Tag.FirstArtist;

                    song.Album = string.IsNullOrEmpty(tfile.Tag.Album) 
                        ? "Unknown Album" 
                        : tfile.Tag.Album;

                    song.Duration = tfile.Properties.Duration.TotalSeconds;
                    song.Genre = string.IsNullOrEmpty(tfile.Tag.FirstGenre) 
                        ? "Unknown" 
                        : tfile.Tag.FirstGenre;

                    song.Year = (int)tfile.Tag.Year;
                    song.HasAlbumArt = tfile.Tag.Pictures != null && tfile.Tag.Pictures.Length > 0;
                }

                if (song.Duration <= 0)
                {
                    // If Duration couldn't be parsed, try NAudio fallback
                    try
                    {
                        using (var reader = new NAudio.Wave.AudioFileReader(filePath))
                        {
                            song.Duration = reader.TotalTime.TotalSeconds;
                        }
                    }
                    catch { }
                }

                return song;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to parse tags for {filePath}: {ex.Message}");
                // Safe fallback
                return new SongInfo
                {
                    FilePath = filePath,
                    Title = Path.GetFileNameWithoutExtension(filePath),
                    Artist = "Unknown Artist",
                    Album = "Unknown Album",
                    Duration = 0,
                    Genre = "Unknown",
                    HasAlbumArt = false
                };
            }
        }

        public Image? GetAlbumArt(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    using (var tfile = TagLib.File.Create(filePath))
                    {
                        if (tfile.Tag.Pictures != null && tfile.Tag.Pictures.Length > 0)
                        {
                            var pic = tfile.Tag.Pictures[0];
                            using (var ms = new MemoryStream(pic.Data.Data))
                            {
                                using (var tempImg = Image.FromStream(ms))
                                {
                                    // Make a deep copy to detach from the stream
                                    return new Bitmap(tempImg);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error retrieving album art: {ex.Message}");
            }
            return null;
        }

        // Playlist Management
        public void CreatePlaylist(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return;
            name = name.Trim();

            if (!data.Playlists.Any(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            {
                data.Playlists.Add(new PlaylistInfo { Name = name });
                SaveDatabase();
                LibraryChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public void DeletePlaylist(string name)
        {
            var playlist = data.Playlists.FirstOrDefault(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (playlist != null)
            {
                data.Playlists.Remove(playlist);
                SaveDatabase();
                LibraryChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public void AddSongToPlaylist(string playlistName, string songPath)
        {
            var playlist = data.Playlists.FirstOrDefault(p => p.Name.Equals(playlistName, StringComparison.OrdinalIgnoreCase));
            if (playlist != null)
            {
                if (!playlist.SongPaths.Contains(songPath))
                {
                    playlist.SongPaths.Add(songPath);
                    SaveDatabase();
                    LibraryChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public void RemoveSongFromPlaylist(string playlistName, string songPath)
        {
            var playlist = data.Playlists.FirstOrDefault(p => p.Name.Equals(playlistName, StringComparison.OrdinalIgnoreCase));
            if (playlist != null)
            {
                if (playlist.SongPaths.Remove(songPath))
                {
                    SaveDatabase();
                    LibraryChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }
    }
}
