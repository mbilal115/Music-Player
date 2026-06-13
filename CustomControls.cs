using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace MusicPlayer
{
	public class ModernButton : Button
	{
		public Color HoverColor { get; set; } = Color.FromArgb(40, 0, 210, 255);
		public Color PressedColor { get; set; } = Color.FromArgb(80, 0, 210, 255);
		public Color BorderColor { get; set; } = Color.FromArgb(0, 210, 255);
		public int BorderRadius { get; set; } = 15;
		public bool ShowBorder { get; set; } = true;

		private bool isHovered = false;
		private bool isPressed = false;

		public ModernButton()
		{
			SetStyle(ControlStyles.AllPaintingInWmPaint |
					 ControlStyles.UserPaint |
					 ControlStyles.OptimizedDoubleBuffer |
					 ControlStyles.ResizeRedraw |
					 ControlStyles.SupportsTransparentBackColor, true);

			BackColor = Color.FromArgb(30, 30, 35);
			ForeColor = Color.White;
			FlatStyle = FlatStyle.Flat;
			FlatAppearance.BorderSize = 0;
			Size = new Size(120, 40);
		}

		protected override void OnMouseEnter(EventArgs e)
		{
			isHovered = true;
			Invalidate();
			base.OnMouseEnter(e);
		}

		protected override void OnMouseLeave(EventArgs e)
		{
			isHovered = false;
			isPressed = false;
			Invalidate();
			base.OnMouseLeave(e);
		}

		protected override void OnMouseDown(MouseEventArgs mevent)
		{
			isPressed = true;
			Invalidate();
			base.OnMouseDown(mevent);
		}

		protected override void OnMouseUp(MouseEventArgs mevent)
		{
			isPressed = false;
			Invalidate();
			base.OnMouseUp(mevent);
		}

		protected override void OnPaint(PaintEventArgs pevent)
		{
			Graphics g = pevent.Graphics;
			g.SmoothingMode = SmoothingMode.AntiAlias;
			g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

			Rectangle rect = ClientRectangle;
			rect.Width -= 1;
			rect.Height -= 1;

	
			Color currentBg = BackColor;
			if (isPressed)
				currentBg = Color.FromArgb(Math.Min(255, BackColor.R + 20), Math.Min(255, BackColor.G + 20), Math.Min(255, BackColor.B + 25));
			else if (isHovered)
				currentBg = Color.FromArgb(Math.Min(255, BackColor.R + 10), Math.Min(255, BackColor.G + 10), Math.Min(255, BackColor.B + 15));

			using (GraphicsPath path = GetRoundedRectanglePath(rect, BorderRadius))
			{
				
				using (SolidBrush brush = new SolidBrush(currentBg))
				{
					g.FillPath(brush, path);
				}

				
				if (isPressed)
				{
					using (SolidBrush brush = new SolidBrush(PressedColor))
						g.FillPath(brush, path);
				}
				else if (isHovered)
				{
					using (SolidBrush brush = new SolidBrush(HoverColor))
						g.FillPath(brush, path);
				}

				
				if (ShowBorder)
				{
					Color currentBorder = isHovered || isPressed ? BorderColor : Color.FromArgb(60, BorderColor);
					using (Pen pen = new Pen(currentBorder, 1.5f))
					{
						g.DrawPath(pen, path);
					}
				}
			}

		
			TextFormatFlags flags = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis;
			TextRenderer.DrawText(g, Text, Font, ClientRectangle, ForeColor, flags);
		}

		private GraphicsPath GetRoundedRectanglePath(Rectangle rect, int radius)
		{
			GraphicsPath path = new GraphicsPath();
			int diameter = radius * 2;

			if (diameter > rect.Width) diameter = rect.Width;
			if (diameter > rect.Height) diameter = rect.Height;

			Size size = new Size(diameter, diameter);
			Rectangle arc = new Rectangle(rect.Location, size);

			if (radius == 0)
			{
				path.AddRectangle(rect);
				return path;
			}

		
			path.AddArc(arc, 180, 90);

		
			arc.X = rect.Right - diameter;
			path.AddArc(arc, 270, 90);

			arc.Y = rect.Bottom - diameter;
			path.AddArc(arc, 0, 90);

		
			arc.X = rect.Left;
			path.AddArc(arc, 90, 90);

			path.CloseFigure();
			return path;
		}
	}

	public class ModernSlider : Control
	{
		public event EventHandler? ValueChanged;
		public event EventHandler? Scroll;

		private float min = 0f;
		private float max = 1f;
		private float val = 0f;

		private bool isDragging = false;
		private bool isHovered = false;

		public float Minimum
		{
			get => min;
			set { min = value; Invalidate(); }
		}

		public float Maximum
		{
			get => max;
			set { max = value; Invalidate(); }
		}

		public float Value
		{
			get => val;
			set
			{
				float newVal = Math.Clamp(value, min, max);
				if (Math.Abs(val - newVal) > 0.0001f)
				{
					val = newVal;
					Invalidate();
					ValueChanged?.Invoke(this, EventArgs.Empty);
				}
			}
		}

		public bool IsDragging => isDragging;

		public Color TrackColor { get; set; } = Color.FromArgb(45, 45, 50);
		public Color ProgressColor { get; set; } = Color.FromArgb(0, 210, 255);
		public Color ThumbColor { get; set; } = Color.White;
		public int ThumbRadius { get; set; } = 6;
		public int TrackHeight { get; set; } = 4;

		public ModernSlider()
		{
			SetStyle(ControlStyles.AllPaintingInWmPaint |
					 ControlStyles.UserPaint |
					 ControlStyles.OptimizedDoubleBuffer |
					 ControlStyles.ResizeRedraw |
					 ControlStyles.SupportsTransparentBackColor, true);

			Height = 20;
			Width = 150;
			BackColor = Color.Transparent;
		}

		protected override void OnMouseEnter(EventArgs e)
		{
			isHovered = true;
			Invalidate();
			base.OnMouseEnter(e);
		}

		protected override void OnMouseLeave(EventArgs e)
		{
			isHovered = false;
			Invalidate();
			base.OnMouseLeave(e);
		}

		protected override void OnMouseDown(MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left)
			{
				isDragging = true;
				UpdateValueFromMouse(e.X);
				Scroll?.Invoke(this, EventArgs.Empty);
			}
			base.OnMouseDown(e);
		}

		protected override void OnMouseMove(MouseEventArgs e)
		{
			if (isDragging)
			{
				UpdateValueFromMouse(e.X);
				Scroll?.Invoke(this, EventArgs.Empty);
			}
			base.OnMouseMove(e);
		}

		protected override void OnMouseUp(MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left)
			{
				isDragging = false;
				Invalidate();
			}
			base.OnMouseUp(e);
		}

		private void UpdateValueFromMouse(int mouseX)
		{
			float percentage = (float)mouseX / Width;
			percentage = Math.Clamp(percentage, 0f, 1f);
			Value = min + percentage * (max - min);
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			Graphics g = e.Graphics;
			g.SmoothingMode = SmoothingMode.AntiAlias;

			int w = Width;
			int h = Height;
			float percent = (max - min) > 0 ? (val - min) / (max - min) : 0;
			int progressX = (int)(percent * w);

			// Draw track background
			int trackY = (h - TrackHeight) / 2;
			using (GraphicsPath trackPath = GetRoundedBarPath(new Rectangle(0, trackY, w, TrackHeight), TrackHeight))
			using (SolidBrush trackBrush = new SolidBrush(TrackColor))
			{
				g.FillPath(trackBrush, trackPath);
			}

			// Draw progress highlight
			if (progressX > TrackHeight)
			{
				using (GraphicsPath progressPath = GetRoundedBarPath(new Rectangle(0, trackY, progressX, TrackHeight), TrackHeight))
				using (SolidBrush progressBrush = new SolidBrush(ProgressColor))
				{
					g.FillPath(progressBrush, progressPath);
				}
			}

			// Draw thumb handle
			int currentThumbRadius = isHovered || isDragging ? ThumbRadius + 2 : ThumbRadius;
			int thumbX = progressX - currentThumbRadius;
			int thumbY = (h - currentThumbRadius * 2) / 2;
			thumbX = Math.Clamp(thumbX, 0, w - currentThumbRadius * 2);

			using (SolidBrush thumbBrush = new SolidBrush(ThumbColor))
			{
				g.FillEllipse(thumbBrush, new Rectangle(thumbX, thumbY, currentThumbRadius * 2, currentThumbRadius * 2));
			}

			// Optional subtle glow around thumb on hover/drag
			if (isHovered || isDragging)
			{
				using (Pen glowPen = new Pen(Color.FromArgb(50, ProgressColor), 3f))
				{
					g.DrawEllipse(glowPen, new Rectangle(thumbX - 1, thumbY - 1, currentThumbRadius * 2 + 2, currentThumbRadius * 2 + 2));
				}
			}
		}

		private GraphicsPath GetRoundedBarPath(Rectangle rect, int height)
		{
			GraphicsPath path = new GraphicsPath();
			int radius = height / 2;
			if (radius <= 0) radius = 1;
			int diameter = radius * 2;

			if (rect.Width < diameter)
			{
				path.AddEllipse(rect);
				return path;
			}

			path.AddArc(rect.X, rect.Y, diameter, diameter, 90, 180);
			path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 180);
			path.CloseFigure();
			return path;
		}
	}

	public class CircularPictureBox : Control
	{
		private Image? image;
		public Image? Image
		{
			get => image;
			set { image = value; Invalidate(); }
		}

		public Color BorderColor { get; set; } = Color.FromArgb(0, 210, 255);
		public float BorderSize { get; set; } = 2.5f;

		public float RotationAngle { get; set; } = 0f;

		public CircularPictureBox()
		{
			SetStyle(ControlStyles.AllPaintingInWmPaint |
					 ControlStyles.UserPaint |
					 ControlStyles.OptimizedDoubleBuffer |
					 ControlStyles.ResizeRedraw |
					 ControlStyles.SupportsTransparentBackColor, true);

			BackColor = Color.Transparent;
			Size = new Size(100, 100);
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			Graphics g = e.Graphics;
			g.SmoothingMode = SmoothingMode.AntiAlias;
			g.InterpolationMode = InterpolationMode.HighQualityBicubic;

			Rectangle rect = ClientRectangle;
			// Leave space for border
			int shrink = (int)Math.Ceiling(BorderSize);
			Rectangle imageRect = new Rectangle(shrink, shrink, rect.Width - shrink * 2, rect.Height - shrink * 2);

			if (imageRect.Width <= 0 || imageRect.Height <= 0) return;

			using (GraphicsPath path = new GraphicsPath())
			{
				path.AddEllipse(imageRect);

				if (image != null)
				{
					g.SetClip(path);

					// Rotate about center if angle is non-zero
					if (RotationAngle != 0)
					{
						float cx = rect.Width / 2f;
						float cy = rect.Height / 2f;
						g.TranslateTransform(cx, cy);
						g.RotateTransform(RotationAngle);
						g.TranslateTransform(-cx, -cy);
					}

					// Maintain aspect ratio while drawing center-cropped
					float imgRatio = (float)image.Width / image.Height;
					float rectRatio = (float)imageRect.Width / imageRect.Height;

					Rectangle destRect;
					if (imgRatio > rectRatio)
					{
						// Image is wider
						int drawWidth = (int)(imageRect.Height * imgRatio);
						int drawX = imageRect.X - (drawWidth - imageRect.Width) / 2;
						destRect = new Rectangle(drawX, imageRect.Y, drawWidth, imageRect.Height);
					}
					else
					{
						// Image is taller
						int drawHeight = (int)(imageRect.Width / imgRatio);
						int drawY = imageRect.Y - (drawHeight - imageRect.Height) / 2;
						destRect = new Rectangle(imageRect.X, drawY, imageRect.Width, drawHeight);
					}

					g.DrawImage(image, destRect);
					g.ResetTransform();
					g.ResetClip();
				}
				else
				{
					// Draw placeholder background
					using (SolidBrush placeholderBrush = new SolidBrush(Color.FromArgb(25, 25, 30)))
					{
						g.FillPath(placeholderBrush, path);
					}

					// Draw placeholder icon (a simple music note or disc)
					using (Pen notePen = new Pen(Color.FromArgb(80, BorderColor), 3))
					{
						int cx = rect.Width / 2;
						int cy = rect.Height / 2;
						g.DrawEllipse(notePen, cx - 10, cy + 2, 8, 6);
						g.DrawLine(notePen, cx - 2, cy + 5, cx - 2, cy - 12);
						g.DrawLine(notePen, cx - 2, cy - 12, cx + 10, cy - 8);
					}
				}

				// Draw neon border
				if (BorderSize > 0)
				{
					using (Pen borderPen = new Pen(BorderColor, BorderSize))
					{
						// Draw concentric circle
						g.DrawEllipse(borderPen, imageRect);
					}

					// Optional outer glowing overlay
					using (Pen glowPen = new Pen(Color.FromArgb(40, BorderColor), BorderSize + 2f))
					{
						Rectangle glowRect = imageRect;
						glowRect.Inflate(1, 1);
						g.DrawEllipse(glowPen, glowRect);
					}
				}
			}
		}
	}

	public class AudioVisualizer : Control
	{
		private readonly System.Windows.Forms.Timer fpsTimer;
		private float[] currentFrequencies = new float[32];
		private float[] visualFrequencies = new float[32];
		private float[] peakHolds = new float[32];
		private float[] peakSpeeds = new float[32];

		public Color ActiveColor { get; set; } = Color.FromArgb(0, 210, 255);
		public Color HighlightColor { get; set; } = Color.FromArgb(127, 0, 255);
		public int BarCount
		{
			get => currentFrequencies.Length;
			set
			{
				if (value > 0 && value != currentFrequencies.Length)
				{
					currentFrequencies = new float[value];
					visualFrequencies = new float[value];
					peakHolds = new float[value];
					peakSpeeds = new float[value];
					Invalidate();
				}
			}
		}

		public AudioVisualizer()
		{
			SetStyle(ControlStyles.AllPaintingInWmPaint |
					 ControlStyles.UserPaint |
					 ControlStyles.OptimizedDoubleBuffer |
					 ControlStyles.ResizeRedraw |
					 ControlStyles.SupportsTransparentBackColor, true);

			BackColor = Color.FromArgb(12, 12, 14);
			Size = new Size(300, 100);

			fpsTimer = new System.Windows.Forms.Timer { Interval = 16 }; // ~60fps
			fpsTimer.Tick += FpsTimer_Tick;
			fpsTimer.Start();
		}

		private void FpsTimer_Tick(object? sender, EventArgs e)
		{
			// Update visual frequencies with smooth decay
			bool needsInvalidate = false;
			for (int i = 0; i < visualFrequencies.Length; i++)
			{
				float target = currentFrequencies[i];

				// Decay target frequency values slowly
				currentFrequencies[i] *= 0.92f; // Fade down the input value

				// Move visual towards target
				float diff = target - visualFrequencies[i];
				if (diff > 0)
				{
					// Instant rise
					visualFrequencies[i] = target;
				}
				else
				{
					// Smooth fall
					visualFrequencies[i] += diff * 0.15f;
				}

				// Peak hold logic
				if (visualFrequencies[i] >= peakHolds[i])
				{
					peakHolds[i] = visualFrequencies[i];
					peakSpeeds[i] = 0.002f; // Reset drop speed
				}
				else
				{
					peakHolds[i] -= peakSpeeds[i];
					peakSpeeds[i] *= 1.05f; // Accelerate drop
					if (peakHolds[i] < 0) peakHolds[i] = 0;
				}

				if (visualFrequencies[i] > 0.001f || peakHolds[i] > 0.001f)
				{
					needsInvalidate = true;
				}
			}

			if (needsInvalidate)
			{
				Invalidate();
			}
		}

		public void UpdateData(float[] magnitudes)
		{
			if (magnitudes == null || magnitudes.Length == 0) return;

			// Map incoming FFT data onto our visual bar count
			int count = Math.Min(magnitudes.Length, currentFrequencies.Length);
			for (int i = 0; i < count; i++)
			{
				// Soft scaling & log compression to make low-intensity signals visual
				float val = magnitudes[i] * 1.8f;
				val = Math.Clamp(val, 0f, 1f);
				if (val > currentFrequencies[i])
				{
					currentFrequencies[i] = val;
				}
			}
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			Graphics g = e.Graphics;
			g.SmoothingMode = SmoothingMode.AntiAlias;

			int w = Width;
			int h = Height;

			if (w <= 0 || h <= 0) return;

			int numBars = visualFrequencies.Length;
			float spacing = 3.0f;
			float totalSpacing = spacing * (numBars - 1);
			float barWidth = (w - totalSpacing) / numBars;
			if (barWidth < 1.0f) barWidth = 1.0f;

			using (LinearGradientBrush gradientBrush = new LinearGradientBrush(
				new Point(0, h),
				new Point(0, 0),
				ActiveColor,
				HighlightColor))
			{
				for (int i = 0; i < numBars; i++)
				{
					float val = visualFrequencies[i];
					float barHeight = val * h;
					if (barHeight < 2) barHeight = 2; // Always draw a tiny indicator line

					float x = i * (barWidth + spacing);
					float y = h - barHeight;

					// Draw spectrum bar
					g.FillRectangle(gradientBrush, x, y, barWidth, barHeight);

					// Draw peak dot
					float peakY = h - (peakHolds[i] * h) - 2;
					peakY = Math.Clamp(peakY, 0, h - 2);
					using (SolidBrush peakBrush = new SolidBrush(HighlightColor))
					{
						g.FillRectangle(peakBrush, x, peakY, barWidth, 2);
					}
				}
			}
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				fpsTimer.Stop();
				fpsTimer.Dispose();
			}
			base.Dispose(disposing);
		}
	}

	public class ModernListView : ListView
	{
		public Color HeaderBackColor { get; set; } = Color.FromArgb(20, 20, 24);
		public Color HeaderForeColor { get; set; } = Color.FromArgb(180, 180, 185);
		public Color SelectionColor { get; set; } = Color.FromArgb(30, 0, 210, 255);
		public Color HoverRowColor { get; set; } = Color.FromArgb(15, 255, 255, 255);

		private int hoveredIndex = -1;

		public ModernListView()
		{
			DoubleBuffered = true;
			OwnerDraw = true;
			View = View.Details;
			FullRowSelect = true;
			BorderStyle = BorderStyle.None;
			BackColor = Color.FromArgb(18, 18, 20);
			ForeColor = Color.White;
			HeaderStyle = ColumnHeaderStyle.Nonclickable; // flat column headers

			MouseMove += ModernListView_MouseMove;
			MouseLeave += ModernListView_MouseLeave;
		}

		private void ModernListView_MouseMove(object? sender, MouseEventArgs e)
		{
			ListViewItem? item = GetItemAt(e.X, e.Y);
			int index = item != null ? item.Index : -1;
			if (index != hoveredIndex)
			{
				hoveredIndex = index;
				Invalidate();
			}
		}

		private void ModernListView_MouseLeave(object? sender, EventArgs e)
		{
			if (hoveredIndex != -1)
			{
				hoveredIndex = -1;
				Invalidate();
			}
		}

		protected override void OnDrawColumnHeader(DrawListViewColumnHeaderEventArgs e)
		{
			Graphics g = e.Graphics;
			Rectangle rect = e.Bounds;

			using (SolidBrush headerBg = new SolidBrush(HeaderBackColor))
			{
				g.FillRectangle(headerBg, rect);
			}

			// Draw a subtle separator at the bottom of headers
			using (Pen sepPen = new Pen(Color.FromArgb(40, 255, 255, 255)))
			{
				g.DrawLine(sepPen, rect.Left, rect.Bottom - 1, rect.Right, rect.Bottom - 1);
			}

			TextFormatFlags flags = TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis;
			Rectangle textRect = new Rectangle(rect.X + 8, rect.Y, rect.Width - 16, rect.Height);
			TextRenderer.DrawText(g, e.Header?.Text ?? "", Font, textRect, HeaderForeColor, flags);
		}

		protected override void OnDrawItem(DrawListViewItemEventArgs e)
		{
			// Fully custom-drawn via SubItems
		}

		protected override void OnDrawSubItem(DrawListViewSubItemEventArgs e)
		{
			Graphics g = e.Graphics;
			Rectangle rect = e.Bounds;
			ListViewItem item = e.Item;

			g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

			// Determine Background Color
			Color bg = BackColor;
			if (item.Selected)
			{
				bg = SelectionColor;
			}
			else if (e.ItemIndex == hoveredIndex)
			{
				bg = HoverRowColor;
			}

			using (SolidBrush bgBrush = new SolidBrush(bg))
			{
				g.FillRectangle(bgBrush, rect);
			}

			// Text formatting
			TextFormatFlags flags = TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis;
			Rectangle textRect = new Rectangle(rect.X + 10, rect.Y, rect.Width - 20, rect.Height);

			Color fg = ForeColor;
			if (item.Selected)
			{
				fg = Color.FromArgb(0, 210, 255); // Highlight text on selection
			}
			else if (e.ItemIndex == hoveredIndex)
			{
				fg = Color.White;
			}
			else
			{
				fg = Color.FromArgb(170, 170, 175);
			}

			TextRenderer.DrawText(g, e.SubItem?.Text ?? "", Font, textRect, fg, flags);
		}
	}
}