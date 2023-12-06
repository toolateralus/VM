using System.Collections.Generic;
using Image = System.Windows.Controls.Image;

namespace Lemur.JS
{
    public class graphics 
    {
        private int ctxIndex;

        public Dictionary<int, GfxContext> gfxContext = new();
        public bool writePixel(int gfx_ctx, int x, int y, int color)
        {
            if (!gfxContext.TryGetValue(gfx_ctx, out var ctx))
            {

                Notifications.Now($"Couldn't find graphics context for id : {gfx_ctx}");
                return false;
            }
            ctx.WritePixelPacked(x, y, color);
            return true;
        }
        // mainly to allow flexibility and allow for testing of code.. not neccesarily the best method XD
        public bool writePixelRGBA(int gfx_ctx, int x, int y, byte r, byte g, byte b, byte a)
        {
            if (!gfxContext.TryGetValue(gfx_ctx, out var ctx))
            {
                Notifications.Now($"Couldn't find graphics context for id : {gfx_ctx}");
                return false;
            }

            ctx.WritePixel(x, y, r, g, b, a);
            return true;
        }
        public bool drawFilledShape(int gfx_ctx, int x, int y, int w, int h,  int colorIndex, int primitveIndex)
        {
            if (!gfxContext.TryGetValue(gfx_ctx, out var ctx))
            {
                Notifications.Now($"Couldn't find graphics context for id : {gfx_ctx}");
                return false;
            }
            ctx.DrawFilledShape(x, y, h, w, colorIndex, (GfxContext.PrimitiveShape)primitveIndex);
            return true;
        }
        public bool writePixelIndexed(int gfx_ctx, int x, int y, int index)
        {
            if (!gfxContext.TryGetValue(gfx_ctx, out var ctx))
            {
                Notifications.Now($"Couldn't find graphics context for id : {gfx_ctx}");
                return false;
            }
            ctx.WritePixelIndexed(x, y, index);
            return true;
        }
        public void clearColor(int gfx_ctx, int color)
        {
            if (!gfxContext.TryGetValue(gfx_ctx, out var ctx))
            {
                Notifications.Now($"Couldn't find graphics context for id : {gfx_ctx}");
                return;
            }
            ctx.ClearColor(color);
        }
        public void clearColorIndexed(int gfx_ctx, int index)
        {
            if (!gfxContext.TryGetValue(gfx_ctx, out var ctx))
            {
                Notifications.Now($"Couldn't find graphics context for id : {gfx_ctx}");
                return;
            }
            ctx.ClearColorIndex(index);
        }
        public bool flushCtx(int gfx_ctx, bool exception = false)
        {
            if (!gfxContext.TryGetValue(gfx_ctx, out var context))
            {
                if (exception) 
                    Notifications.Now($"Couldn't find graphics context for id : {gfx_ctx}");

                return false;
            }

            Computer.Current?.Window?.Dispatcher?.Invoke(() => { 
              
                if (context.image.TryGetTarget(out var image))
                    context.Draw(image);
            });

            return true;
        }
        public int createCtx(string id, string target, int width, int height)
        {
            int bpp = 4;
            

            var ctx = new GfxContext(id, target, bpp);

            ctx.Resize(width, height);
            
            int i = 0;

            while (gfxContext.ContainsKey(ctxIndex))
                ctxIndex++;

            gfxContext[ctxIndex] = ctx;

            return ctxIndex;
        }
    }
}

