
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
        public static Action<UserControl, Computer, object[]?> EventInitializer = new((control, pc, args) => {

            var fieldName = args[0] as string;
            var jsMethodBinding = args[1] as string;

            if (string.IsNullOrEmpty(fieldName) || string.IsNullOrEmpty(jsMethodBinding))
            {
                Notifications.Now($"Invalid args for Event initializer (xaml?.c#?.js)");
                return;
            }

            var action = pc.OS.JavaScriptEngine.FetchMethodBinding(jsMethodBinding);

            if (action == null)
            {
                Notifications.Now($"JavaScript method binding was not found for {jsMethodBinding}");
                return;
            }

            Type controlType = control.GetType();

            EventInfo[] events = controlType.GetEvents();

            foreach (EventInfo eventInfo in events)
            {
                if (eventInfo.Name.Contains(fieldName))
                {
                    Delegate handler = Delegate.CreateDelegate(eventInfo.EventHandlerType, action.Target, action.Method);

                    eventInfo.AddEventHandler(control, handler);
                }
            }
        });


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
                throw new InvalidOperationException("The UserControl does not have an InitializeComponent method.");
            }
        }

        public static UserControl ParseUserControl(string xaml)
        {
            try
            {
                object parsedObject = XamlReader.Parse(xaml);

                if (parsedObject is UserControl userControl)
                {
                    return userControl;
                }
                else
                {
                    Notifications.Now("The provided XAML does not represent a UserControl.");
                    return null;
                }
            }
            catch (XamlParseException ex)
            {
                Notifications.Now($"XAML parsing error: {ex.Message}");
                return null;
            }
        }

    }
}