using Lemur.Windowing;
using System.Windows.Controls;
using System.Windows.Markup;

namespace Lemur.JavaScript.Api {
    public static class XamlHelper {
        public static UserControl? ParseUserControl(string xaml) {
            var task = Computer.Current.Window.Dispatcher.InvokeAsync(() => {
                try {
                   return XamlReader.Parse(xaml) as UserControl;
                }
                catch (XamlParseException ex) {
                    Notifications.Now($"XAML parsing error: {ex.Message}");
                }
                return null;
            });
            task.Wait();
            return task.Result;
        }
    }
}