using System.Collections.Generic;
using Image = System.Windows.Controls.Image;

namespace VM.JS
{
    public class Graphics
    {
        private int ctxIndex;
        public Dictionary<int, GraphicsContext> gfxContext = new();
        public bool writePixel(int gfx_ctx, int x, int y, int color)
        {
            if (!gfxContext.TryGetValue(gfx_ctx, out var ctx))
            {
                Notifications.Now($"Couldn't find graphics context for id : {gfx_ctx}");
                return false;
            }
            ctx.WritePixel(x, y, color);
            return true;
        }
        public void clearColor(int gfx_ctx, int color)
        {
            if (!gfxContext.TryGetValue(gfx_ctx, out var ctx))
            {
                Notifications.Now($"Couldn't find graphics context for id : {gfx_ctx}");
                return;
            }
            ctx.clearColor(color);
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
                var control = JSInterop.GetUserContent(context.InstanceID, Computer.Current);
                var image = JSInterop.FindControl(control, context.TargetControl) as Image;
                context.Draw(image);
            });

            return true;
        }
        public int createCtx(string id, string target, int width, int height)
        {
            int bpp = 4;
            
            var ctx = new GraphicsContext(id, target, bpp);
            ctx.Resize(width, height);

            int i = 0;

            while (gfxContext.ContainsKey(ctxIndex))
                ctxIndex++;

            gfxContext[ctxIndex] = ctx;

            return ctxIndex;
        }
    }
}

