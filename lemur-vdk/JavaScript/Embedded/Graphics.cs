using Lemur.Windowing;
using System;
using System.Collections.Generic;

namespace Lemur.JS.Embedded
{
    public class graphics : embedable
    {

        private double ctxIndex;

        public Dictionary<double, GraphicsContext> GraphicsContext = [];
        public static readonly List<byte[]> palette = new()
        {
            new byte[]{255, 0, 0, 255}, // Red 0
            new byte[]{255, 128, 0, 255}, // Orange 1
            new byte[]{255, 255, 0, 255}, // Yellow 2
            new byte[]{128, 255, 0, 255}, // Lime Green 3
            new byte[]{0, 255, 0, 255}, // Green 4
            new byte[]{0, 255, 128, 255}, // Spring Green 5
            new byte[]{0, 255, 255, 255}, // Cyan 6
            new byte[]{0, 128, 255, 255}, // Sky Blue 7 
            new byte[]{0, 0, 255, 255}, // Blue 8
            new byte[]{128, 0, 255, 255}, // Purple 9 
            new byte[]{255, 0, 255, 255}, // Magenta 10
            new byte[]{255, 0, 128, 255}, // Pink 11
            new byte[]{192, 192, 192, 255}, // Light Gray 12
            new byte[]{128, 128, 128, 255}, // Medium Gray 13
            new byte[]{64, 64, 64, 255}, // Dark Gray 14
            new byte[]{0, 0, 0, 255}, // Black 15
            new byte[]{255, 255, 255, 255}, // White 16
            new byte[]{255, 69, 0, 255}, // Red-Orange 17
            new byte[]{255, 215, 0, 255}, // Gold 18
            new byte[]{0, 128, 0, 255}, // Dark Green 19
            new byte[]{0, 128, 128, 255}, // Teal 20
            new byte[]{0, 0, 128, 255}, // Navy 21
            new byte[]{255, 20, 147, 255}, // Deep Pink 22
            new byte[]{0, 250, 154, 255} // Medium Spring Green 23
        };
        public graphics(Computer computer) : base(computer)
        {
        }

        public bool writePixel(double gfx_ctx, double x, double y, double color)
        {
            if (!GraphicsContext.TryGetValue(gfx_ctx, out var ctx))
            {

                Notifications.Now($"Couldn't find graphics context for id : {gfx_ctx}");
                return false;
            }

            ctx.WritePixelPacked(x, y, color);

            return true;
        }
        public bool writePixelRGBA(double gfx_ctx, double x, double y, byte r, byte g, byte b, byte a)
        {
            if (!GraphicsContext.TryGetValue(gfx_ctx, out var ctx))
            {
                Notifications.Now($"Couldn't find graphics context for id : {gfx_ctx}");
                return false;
            }

            ctx.WritePixel(x, y, r, g, b, a);

            return true;
        }
        public bool drawFilledShape(double gfx_ctx, double x, double y, double w, double h, double r, double colorIndex, double primitveIndex)
        {
            if (!GraphicsContext.TryGetValue(gfx_ctx, out var ctx))
            {
                Notifications.Now($"Couldn't find graphics context for id : {gfx_ctx}");
                return false;
            }
            ctx.DrawFilledShape(x, y, h, w, r, colorIndex, (GraphicsContext.PrimitiveShape)primitveIndex);
            return true;
        }
        public bool writePixelIndexed(double gfx_ctx, double x, double y, double index)
        {
            if (!GraphicsContext.TryGetValue(gfx_ctx, out var ctx))
            {
                Notifications.Now($"Couldn't find graphics context for id : {gfx_ctx}");
                return false;
            }
            ctx.WritePixelIndexed(x, y, index);
            return true;
        }
        public void clearColor(double gfx_ctx, double color)
        {
            if (!GraphicsContext.TryGetValue(gfx_ctx, out var ctx))
            {
                Notifications.Now($"Couldn't find graphics context for id : {gfx_ctx}");
                return;
            }
            ctx.ClearColor(color);
        }
        public void clearColorIndexed(double gfx_ctx, double index)
        {
            if (!GraphicsContext.TryGetValue(gfx_ctx, out var ctx))
            {
                Notifications.Now($"Couldn't find graphics context for id : {gfx_ctx}");
                return;
            }
            ctx.ClearColorIndex(index);
        }
        public bool flushCtx(double gfx_ctx)
        {
            if (!GraphicsContext.TryGetValue(gfx_ctx, out var context))
            {
                Notifications.Now($"Couldn't find graphics context for id : {gfx_ctx}");
                return false;
            }

            Computer.Current?.Window?.Dispatcher?.Invoke(() =>
            {

                if (context.image.TryGetTarget(out var image))
                    context.Draw(image);
            });

            return true;
        }
        public void saveToImage(double gfx_ctx, string path)
        {
            if (!GraphicsContext.TryGetValue(gfx_ctx, out var context))
            {
                Notifications.Now($"Couldn't find graphics context for id : {gfx_ctx}");
                return;
            }

            context.SaveToImage(path);
        }
        public void drawSkybox(double gfx_ctx)
        {
            if (!GraphicsContext.TryGetValue(gfx_ctx, out var context))
            {
                Notifications.Now($"Couldn't find graphics context for id : {gfx_ctx}");
                return;
            }
            
            if (context == null)
            {
                Notifications.Now($"Couldn't find graphics context for id : {gfx_ctx}");
                return;
            }


            context.DrawSkybox();
        }
        public void loadSkybox(double gfx_ctx, string path)
        {
            if (!GraphicsContext.TryGetValue(gfx_ctx, out var context))
            {
                Notifications.Now($"Couldn't find graphics context for id : {gfx_ctx}");

                return;
            }

            context.LoadSkybox(path);
        }
        public void loadFromImage(double gfx_ctx, string path)
        {
            if (!GraphicsContext.TryGetValue(gfx_ctx, out var context))
            {
                Notifications.Now($"Couldn't find graphics context for id : {gfx_ctx}");

                return;
            }

            context.LoadFromImage(path);
        }
        public double createCtx(string id, string target, double width, double height)
        {
            const int bpp = 4;

            var ctx = new GraphicsContext(GetComputer(), id, target, bpp);

            ctx.Resize(width, height);

            while (GraphicsContext.ContainsKey(ctxIndex))
                ctxIndex++;

            GraphicsContext[ctxIndex] = ctx;

            return ctxIndex;
        }
    }
}

