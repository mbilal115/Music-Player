using System;
using System.IO;
using NAudio.Wave;

namespace MusicPlayer
{
    public class AudioManager : IDisposable
    {
        private static AudioManager? instance;
        public static AudioManager Instance => instance ??= new AudioManager();

        private WaveOutEvent? outputDevice;
        private AudioFileReader? fileReader;
        private VisualizerSampleProvider? visualizerProvider;

        private PlaybackState state = PlaybackState.Stopped;
        private string? currentFilePath;
        private bool isDisposed = false;
        private bool userStopped = false;

        public event EventHandler<PlaybackState>? PlaybackStateChanged;
        public event EventHandler<string>? TrackStarted;
        public event EventHandler? TrackFinished;
        public event EventHandler<string>? PlaybackError;

        public PlaybackState State
        {
            get => state;
            private set
            {
                if (state != value)
                {
                    state = value;
                    PlaybackStateChanged?.Invoke(this, state);
                }
            }
        }

        public string? CurrentFilePath => currentFilePath;

        public double CurrentTime
        {
            get => fileReader?.CurrentTime.TotalSeconds ?? 0;
            set
            {
                if (fileReader != null)
                {
                    fileReader.CurrentTime = TimeSpan.FromSeconds(Math.Clamp(value, 0, TotalTime));
                }
            }
        }

        public double TotalTime => fileReader?.TotalTime.TotalSeconds ?? 0;

        public float Volume
        {
            get => fileReader?.Volume ?? 0.5f;
            set
            {
                if (fileReader != null)
                {
                    fileReader.Volume = Math.Clamp(value, 0f, 1f);
                }
                else if (outputDevice != null)
                {
                    outputDevice.Volume = Math.Clamp(value, 0f, 1f);
                }
            }
        }

        private AudioManager()
        {
        }

        public bool Play(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    PlaybackError?.Invoke(this, $"File does not exist: {Path.GetFileName(filePath)}");
                    return false;
                }

                userStopped = true; 
                StopAndCleanup();

                userStopped = false;
                currentFilePath = filePath;

                outputDevice = new WaveOutEvent();
                outputDevice.PlaybackStopped += OnPlaybackStopped;

                fileReader = new AudioFileReader(filePath);
                visualizerProvider = new VisualizerSampleProvider(fileReader);

                outputDevice.Init(visualizerProvider);
                outputDevice.Play();

                State = PlaybackState.Playing;
                TrackStarted?.Invoke(this, filePath);
                return true;
            }
            catch (Exception ex)
            {
                PlaybackError?.Invoke(this, $"Error playing file: {ex.Message}");
                StopAndCleanup();
                return false;
            }
        }

        public void Pause()
        {
            if (outputDevice != null && State == PlaybackState.Playing)
            {
                outputDevice.Pause();
                State = PlaybackState.Paused;
            }
        }

        public void Resume()
        {
            if (outputDevice != null && State == PlaybackState.Paused)
            {
                outputDevice.Play();
                State = PlaybackState.Playing;
            }
            else if (currentFilePath != null && State == PlaybackState.Stopped)
            {
                Play(currentFilePath);
            }
        }

        public void Stop()
        {
            userStopped = true;
            if (outputDevice != null)
            {
                outputDevice.Stop();
            }
            State = PlaybackState.Stopped;
        }

        public float[] GetVisualizerData()
        {
            if (State == PlaybackState.Playing && visualizerProvider != null)
            {
                return visualizerProvider.GetMagnitudes();
            }
            return new float[256];
        }

        private void OnPlaybackStopped(object? sender, StoppedEventArgs e)
        {
            if (e.Exception != null)
            {
                PlaybackError?.Invoke(this, $"Playback error: {e.Exception.Message}");
            }

            
            bool naturallyFinished = false;
            if (fileReader != null && fileReader.Position >= fileReader.Length)
            {
                naturallyFinished = true;
            }

            if (!userStopped && naturallyFinished)
            {
                State = PlaybackState.Stopped;
                
                System.Threading.Tasks.Task.Run(() => {
                    TrackFinished?.Invoke(this, EventArgs.Empty);
                });
            }
            else if (userStopped)
            {
                State = PlaybackState.Stopped;
            }
        }

        private void StopAndCleanup()
        {
            if (outputDevice != null)
            {
                outputDevice.PlaybackStopped -= OnPlaybackStopped;
                outputDevice.Stop();
                outputDevice.Dispose();
                outputDevice = null;
            }

            if (fileReader != null)
            {
                fileReader.Dispose();
                fileReader = null;
            }

            visualizerProvider = null;
        }

        public void Dispose()
        {
            if (!isDisposed)
            {
                userStopped = true;
                StopAndCleanup();
                isDisposed = true;
            }
            GC.SuppressFinalize(this);
        }
    }

   
    public class VisualizerSampleProvider : ISampleProvider
    {
        private readonly ISampleProvider source;
        private readonly float[] fftBuffer;
        private int fftPos;
        private readonly float[] outputMagnitudes;
        private readonly object lockObj = new object();

        public WaveFormat WaveFormat => source.WaveFormat;

        public VisualizerSampleProvider(ISampleProvider source)
        {
            this.source = source;
            this.fftBuffer = new float[512]; 
            this.outputMagnitudes = new float[256]; 
        }

        public int Read(float[] buffer, int offset, int count)
        {
            int samplesRead = source.Read(buffer, offset, count);

            lock (lockObj)
            {
                for (int i = 0; i < samplesRead; i++)
                {
                    fftBuffer[fftPos] = buffer[offset + i];
                    fftPos++;
                    if (fftPos >= fftBuffer.Length)
                    {
                        fftPos = 0;
                        AnalyzeFFT();
                    }
                }
            }

            return samplesRead;
        }

        public float[] GetMagnitudes()
        {
            lock (lockObj)
            {
                float[] result = new float[outputMagnitudes.Length];
                Array.Copy(outputMagnitudes, result, result.Length);
                return result;
            }
        }

        private void AnalyzeFFT()
        {
            int n = fftBuffer.Length;
            Complex[] complexBuffer = new Complex[n];
            for (int i = 0; i < n; i++)
            {
               
                double window = 0.5 * (1.0 - Math.Cos(2.0 * Math.PI * i / (n - 1)));
                complexBuffer[i] = new Complex(fftBuffer[i] * window, 0);
            }

            
            Fft.Transform(complexBuffer);

           
            int m = n / 2;
            for (int i = 0; i < m; i++)
            {
                double re = complexBuffer[i].Real;
                double im = complexBuffer[i].Imaginary;
                
                
                float mag = (float)Math.Sqrt(re * re + im * im);
                
               
                float multiplier = 1.0f + (float)i / m * 2.0f;
                outputMagnitudes[i] = mag * multiplier;
            }
        }
    }

    public struct Complex
    {
        public double Real;
        public double Imaginary;

        public Complex(double real, double imaginary)
        {
            Real = real;
            Imaginary = imaginary;
        }

        public static Complex operator +(Complex a, Complex b) => new Complex(a.Real + b.Real, a.Imaginary + b.Imaginary);
        public static Complex operator -(Complex a, Complex b) => new Complex(a.Real - b.Real, a.Imaginary - b.Imaginary);
        public static Complex operator *(Complex a, Complex b) => new Complex(
            a.Real * b.Real - a.Imaginary * b.Imaginary,
            a.Real * b.Imaginary + a.Imaginary * b.Real
        );
    }

    public static class Fft
    {
        public static void Transform(Complex[] buffer)
        {
            int n = buffer.Length;
            int bits = (int)Math.Log2(n);

            
            for (int i = 0; i < n; i++)
            {
                int j = BitReverse(i, bits);
                if (j > i)
                {
                    var temp = buffer[i];
                    buffer[i] = buffer[j];
                    buffer[j] = temp;
                }
            }

            for (int len = 2; len <= n; len <<= 1)
            {
                double angle = -2 * Math.PI / len;
                Complex wlen = new Complex(Math.Cos(angle), Math.Sin(angle));
                for (int i = 0; i < n; i += len)
                {
                    Complex w = new Complex(1, 0);
                    for (int j = 0; j < len / 2; j++)
                    {
                        Complex u = buffer[i + j];
                        Complex v = buffer[i + j + len / 2] * w;
                        buffer[i + j] = u + v;
                        buffer[i + j + len / 2] = u - v;
                        w = w * wlen;
                    }
                }
            }
        }

        private static int BitReverse(int n, int bits)
        {
            int reversed = 0;
            for (int i = 0; i < bits; i++)
            {
                if ((n & (1 << i)) != 0)
                {
                    reversed |= 1 << (bits - 1 - i);
                }
            }
            return reversed;
        }
    }
}
