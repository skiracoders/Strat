using System;
using System.Collections.Generic;
using System.Threading;
using System.Runtime.InteropServices;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.Windowing;
using Silk.NET.OpenGL;
using System.Drawing;
using System.Drawing.Imaging;

namespace Skira
{
    public class InputSystem
    {

    }
    public class OutputSystem
    {
        private IWindow window;
        private GL gl;
        private IWindowPlatform windowPlatform;
        private IMonitor monitor;
        private uint texture, framebuffer;
        private byte[] pixels;
        private Size textureSize;
        private System.Drawing.Rectangle textureRectangle, bitmapRectangle;
        private Bitmap bitmap;
        public OutputSystem(string name, int width, int height)
        {
            windowPlatform = Window.GetWindowPlatform(false);
            monitor = windowPlatform.GetMainMonitor();
            WindowOptions windowOptions = WindowOptions.Default;
            windowOptions.WindowBorder = WindowBorder.Resizable;
            windowOptions.IsVisible = false;
            windowOptions.Size = monitor.VideoMode.Resolution.Value * 3 / 4;
            windowOptions.Title = name;
            windowOptions.VSync = true;
            window = Window.Create(windowOptions);
            window.Load += OnLoad;
            window.Render += OnRender;
            window.Resize += OnResize;
            pixels = new byte[width * height * 3];
            textureSize = new Size(width, height);
            bitmapRectangle = new System.Drawing.Rectangle(Point.Empty, textureSize);
            bitmap = new Bitmap(textureSize.Width, textureSize.Height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            textureRectangle = System.Drawing.Rectangle.Empty;
            AdjustView();
        }
        private unsafe void OnLoad()
        {
            window.Position = monitor.VideoMode.Resolution.Value / 2 - window.Size / 2;
            gl = GL.GetApi(window);
            texture = gl.GenTexture();
            framebuffer = gl.GenFramebuffer();
            gl.BindTexture(TextureTarget.Texture2D, texture);
            fixed (void* pixelsPointer = &pixels[0])
            {
                gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgb, (uint)textureSize.Width,
                    (uint)textureSize.Height, 0, Silk.NET.OpenGL.PixelFormat.Bgr, PixelType.UnsignedByte, pixelsPointer);
            }
            gl.BindFramebuffer(FramebufferTarget.ReadFramebuffer, framebuffer);
            gl.FramebufferTexture2D(FramebufferTarget.ReadFramebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, texture, 0);
            window.IsVisible = true;
        }
        private void FillPixels()
        {
            var bitmapData = bitmap.LockBits(bitmapRectangle, ImageLockMode.ReadOnly, bitmap.PixelFormat);
            Marshal.Copy(bitmapData.Scan0, pixels, 0, pixels.Length);
            bitmap.UnlockBits(bitmapData);
        }
        private unsafe void UpdateTexture()
        {
            fixed (void* pixelsPointer = &pixels[0])
            {
                gl.TexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, (uint)textureSize.Width, (uint)textureSize.Height, Silk.NET.OpenGL.PixelFormat.Bgr,
                    PixelType.UnsignedByte, pixelsPointer);
            }
        }
        private void OnRender(double seconds)
        {
            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                graphics.Clear(Color.White);
            }
            FillPixels();
            UpdateTexture();
            gl.ClearColor(Color.Black);
            gl.BindFramebuffer(FramebufferTarget.ReadFramebuffer, framebuffer);
            gl.BlitFramebuffer(0, 0, textureSize.Width, textureSize.Height, textureRectangle.X, textureRectangle.Y, textureRectangle.Right,
                textureRectangle.Bottom, ClearBufferMask.ColorBufferBit, BlitFramebufferFilter.Nearest);
            gl.BindFramebuffer(FramebufferTarget.ReadFramebuffer, 0u);
        }
        public void Run()
        {
            window.Run();
        }
        private void OnResize(Vector2D<int> newSize)
        {
            AdjustView();
        }
        public void AdjustView()
        {
            double windowHeight = window.Size.Y;
            double windowWidth = window.Size.X;
            double width, height, difference, half;
            double proportion = (double)textureSize.Width / (double)textureSize.Height;
            double inverse = 1d / proportion;
            if (windowHeight > inverse * windowWidth)
            {
                width = windowWidth;
                height = width * inverse;
                difference = windowHeight - height;
                half = difference / 2d;
                textureRectangle.X = 0;
                textureRectangle.Y = (int)half;
            }
            else
            {
                height = windowHeight;
                width = height * proportion;
                difference = windowWidth - width;
                half = difference / 2d;
                textureRectangle.X = (int)half;
                textureRectangle.Y = 0;
            }
            textureRectangle.Width = (int)width;
            textureRectangle.Height = (int)height;
        }
    }
    public class Strat
    {
        private OutputSystem outputSystem;
        public Strat()
        {
            outputSystem = new OutputSystem("Strat", 64, 32);
        }
        public void Run()
        {
            outputSystem.Run();
        }
    }
    public class Singleton : IDisposable
    {
        private static Singleton instance = null;
        private static readonly object padlock = new object();
        public static Singleton Instance
        {
            get
            {
                lock (padlock)
                {
                    if (instance == null)
                    {
                        instance = new Singleton();
                    }
                    return instance;
                }
            }
        }
        private Singleton()
        {
        }
        public void Dispose()
        {

        }
        private Strat strat;
        public void Initialize()
        {
            strat = new Strat();
        }
        public void Run()
        {
            strat.Run();
        }
    }
    class Program
    {
        static void Main(string[] args)
        {
            using (Singleton singleton = Singleton.Instance)
            {
                singleton.Initialize();
                singleton.Run();
            }
        }
    }
}
