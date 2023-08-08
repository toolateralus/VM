
using Microsoft.Windows.Themes;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows.Controls;
using System.Windows.Markup;
using VM.OS.JS;

namespace VM.OS.UserInterface
{
    public class XamlJsInterop
    {
        /// <summary>
        /// Arg0 = string, event field name
        /// Arg1 = somehow make a binding to a js method? and call it?
        /// </summary>
       
        public static void InitializeControl(Computer computer, UserControl control, List<Action<UserControl, Computer, object[]?>> initializations, List<object[]?> args)
        {
            for (int i = 0; i < initializations.Count; i++)
            {
                Action<UserControl, Computer, object[]?> init = initializations[i];
                init?.Invoke(control, computer, args[i]);
            }
        }
        public static void CallInitializeComponent(UserControl userControl)
        {
            Type userControlType = userControl.GetType();
            MethodInfo initializeComponentMethod = userControlType.GetMethod("InitializeComponent", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (initializeComponentMethod != null)
            {
                initializeComponentMethod.Invoke(userControl, null);
            }
            else
            {
                return;
            }
        }

        public static UserControl ParseUserControl(string xaml)
        {
            UserControl product = null;
            Action<UserControl> output = (e) => { product = e; };

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
                        Notifications.Now("The provided XAML does not represent a UserControl.");
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