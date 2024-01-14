using Lemur.Windowing;
using System;
using System.Windows.Controls;
using System.Windows.Markup;

namespace Lemur.JavaScript.Api
{
    public static class XamlHelper
    {
        public static UserControl? ParseUserControl(string xaml)
        {
            UserControl? product = null;
            Action<UserControl> output = (e) => { product = e; };

            if (xaml == "Not found!")
                return null;

            System.Windows.Application.Current.Dispatcher.Invoke(delegate
            {
                try
                {
                    object parsedObject = XamlReader.Parse(xaml);

                    if (parsedObject is UserControl userControl)
                    {
                        output.Invoke(userControl);
                    }
                    else
                    {
                        Notifications.Now("The provided XAML does not represent a UserControl, or has errors.");
                    }
                }
                catch (XamlParseException ex)
                {
                    Notifications.Now($"XAML parsing error: {ex.Message}");
                }
            });

            return product;
        }


    }
}