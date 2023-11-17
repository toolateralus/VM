
using Microsoft.Windows.Themes;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows.Controls;
using System.Windows.Markup;
using VM.JS;
using VM;

namespace VM.UserInterface
{
    public class XamlJsInterop
    {
        public static UserControl? ParseUserControl(string xaml)
        {
            UserControl? product = null;
            Action<UserControl> output = (e) => { product = e; };

            if (xaml == "Not found!")
                return null;

            App.Current.Dispatcher.Invoke(delegate { 
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