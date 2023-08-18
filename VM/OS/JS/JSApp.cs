﻿//using System.Collections.Generic;
//using System;
//using VM.GUI;
//using Microsoft.ClearScript.JavaScript;
//using System.IO;
//using System.Runtime.InteropServices;
//using System.Windows.Controls;
//using System.Windows.Media.Imaging;
//using System.Windows.Media;
//using System.Windows;
//using System.Linq;
//using VM;

//namespace VM.JS
//{
//    internal class JSApp
//    {
//        private Computer Computer;
//        public JSApp(Computer computer)
//        {
//            this.Computer = computer;
//            EventActions.TryAdd("draw_pixels", DrawPixelsEvent);
//            EventActions.TryAdd("draw_image", DrawImageEvent);
//            EventActions.TryAdd("set_content", SetContent);
//            EventActions.TryAdd("get_content", GetContent);
//        }

        
//        #region C# Methods
//        public T FindElementInUserControl<T>(UserControl userControl, string elementName) where T : FrameworkElement
//        {
//            var elementType = typeof(T);
//            var contentProperty = userControl.GetType().GetProperty("Content");

//            if (contentProperty != null)
//            {
//                var content = contentProperty.GetValue(userControl);

//                if (content != null)
//                {
//                    foreach (var property in content.GetType().GetProperties())
//                    {
//                        if (elementType.IsAssignableFrom(property.PropertyType) && property.Name == elementName && property.GetValue(content) is T Instance)
//                        {
//                            return Instance;
//                        }
//                    }
//                }
//            }

//            return default;
//        }
//        public static void SetProperty(object target, string propertyName, object? value)
//        {
//            var targetType = target.GetType();
//            var propertyInfo = targetType.GetProperty(propertyName);

//            propertyInfo?.SetValue(target, value);
//        }
//        public static object GetProperty(object target, string propertyName)
//        {
//            var targetType = target.GetType();
//            var propertyInfo = targetType.GetProperty(propertyName);

//            return propertyInfo != null ? propertyInfo.GetValue(target) : null;
//        }
       
//        private object? GetContent(string id, string controlName, object? value)
//        {
//            object? output = null;

//            Computer.Window?.Dispatcher.Invoke(() =>
//            {
//                var userControl = GetUserContent(id, Computer);
//                var control = FindControl(userControl, controlName);

//                if (control is null)
//                    return;

//                if (control.GetType() == typeof(TextBox) || control.GetType() == typeof(TextBlock))
//                {
//                    output = GetProperty(control, "Text");
//                }
//                else
//                {
//                    output = GetProperty(control, "Content");
//                }
//            });

//            return output;
//        }
//        public static UserControl? GetUserContent(string javascript_controL_class_instance_id, Computer computer)
//        {
//            var window = computer.Window;
//            var resizableWins = window.USER_WINDOW_INSTANCES.Where(W => W.Key == javascript_controL_class_instance_id);
//            UserControl userContent = null;

//            if (resizableWins.Any())
//            {
//                var win = resizableWins.First().Value;

//                if (win != null)
//                {
//                    var contentGrid = win.Content as Grid;

//                    if (contentGrid != null)
//                    {
//                        var frame = contentGrid.Children.OfType<Frame>().FirstOrDefault();

//                        if (frame != null)
//                        {
//                            userContent = frame.Content as UserControl;
//                        }
//                    }
//                }
//            }

//            return userContent;
//        }
//        public static FrameworkElement? FindControl(UserControl userControl, string controlName)
//        {

//            FrameworkElement element = null;
//            var contentProperty = userControl?.GetType()?.GetProperty("Content");

//            if (contentProperty != null)
//            {
//                var content = contentProperty.GetValue(userControl);

//                if (content != null)
//                {
//                    if (content is FrameworkElement contentElement && contentElement.Name == controlName)
//                    {
//                        return contentElement;
//                    }

//                    return SearchVisualTree(content, controlName);
//                }
//            }
//            return element;
//        }
//        public static FrameworkElement? SearchVisualTree(object element, string controlName)
//        {
//            if (element is FrameworkElement frameworkElement && frameworkElement.Name == controlName)
//            {
//                return frameworkElement;
//            }

//            if (element is DependencyObject dependencyObject)
//            {
//                int childCount = VisualTreeHelper.GetChildrenCount(dependencyObject);
//                for (int i = 0; i < childCount; i++)
//                {
//                    var childElement = VisualTreeHelper.GetChild(dependencyObject, i);
//                    var result = SearchVisualTree(childElement, controlName);
//                    if (result != null)
//                    {
//                        return result;
//                    }
//                }
//            }

//            return null;
//        }
//        private object? SetContent(string id, string control, object? value)
//        {
//            object? output = null;

//            var wnd = Computer.Window;

//            wnd?.Dispatcher.Invoke(() =>
//            {
//                var userControl = GetUserContent(id, Computer);

//                if (userControl == null)
//                    return;

//                var _control = FindControl(userControl, control);

//                if (_control == null)
//                    return;

//                if (_control.GetType() == typeof(TextBox) || _control.GetType() == typeof(TextBlock))
//                {
//                    SetProperty(_control, "Text", value);
//                }
//                else
//                {
//                    SetProperty(_control, "Content", value);
//                }
//            });
//            return output;
//        }
//        public BitmapImage BitmapImageFromBase64(string base64String)
//        {
//            try
//            {
//                byte[] imageBytes = Convert.FromBase64String(base64String);

//                using (MemoryStream memoryStream = new MemoryStream(imageBytes))
//                {
//                    BitmapImage bitmapImage = new BitmapImage();
//                    bitmapImage.BeginInit();
//                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
//                    bitmapImage.StreamSource = memoryStream;
//                    bitmapImage.EndInit();
//                    return bitmapImage;
//                }
//            }
//            catch (Exception ex)
//            {
//                // Handle any exceptions that may occur during conversion
//                Console.WriteLine("Exception during base64 to BitmapImage conversion: " + ex.Message);
//                return null;
//            }
//        }
//        public object? DrawImageEvent(string id, string target_control, object? value)
//        {
//            if (value is null)
//                return null;

//            Computer.Window?.Dispatcher.Invoke(() =>
//            {
//                var control = GetUserContent(id,Computer);

//                var image = FindControl(control, target_control);

//                if (image is Image img)
//                {
//                    if (value is string Base64Image && BitmapImageFromBase64(Base64Image) is BitmapImage bitmap)
//                    {
//                        img.Source = bitmap;
//                    }
//                }
//            });
//            return null;
//        }
//        public object? DrawPixelsEvent(string id, string target_control, object? value)
//        {
//            if (value is null)
//                return null;


//            List<byte> colorData = new();

//            JSInterop.forEach<int>(value.ToEnumerable(), (item) => colorData.Add((byte)item));

//            Computer.Window?.Dispatcher.Invoke(() =>
//            {
//                var control = GetUserContent(id, Computer);
//                if (control?.Content is Grid grid)
//                {
//                    if (grid != null)
//                    {
//                        var image = FindControl(control, target_control) as Image;

//                        if (image != null)
//                        {
//                            Draw(colorData, image);
//                        }
//                    }
//                }
//            });

//            return null;
//        }
//        public static void Draw(List<byte> colorData, Image image)
//        {
//            var bytesPerPixel = 4;
//            var pixelCount = colorData.Count / bytesPerPixel;

//            var width = (int)Math.Sqrt(pixelCount);
//            var height = pixelCount / width;

//            var bitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);

//            bitmap.Lock();

//            for (int y = 0; y < height; y++)
//            {
//                for (int x = 0; x < width; x++)
//                {
//                    int pixelIndex = (y * width + x) * bytesPerPixel;
//                    byte a = colorData[pixelIndex];
//                    byte r = colorData[pixelIndex + 1];
//                    byte g = colorData[pixelIndex + 2];
//                    byte b = colorData[pixelIndex + 3];

//                    byte[] pixelData = new byte[] { b, g, r, a };
//                    Marshal.Copy(pixelData, 0, bitmap.BackBuffer + pixelIndex, bytesPerPixel);
//                }
//            }

//            bitmap.AddDirtyRect(new Int32Rect(0, 0, width, height));
//            bitmap.Unlock();

//            image.Source = bitmap;
//        }
//        #endregion
//        public static Dictionary<string, Func<string, string, object?, object?>> EventActions = new();
//        public object? pushEvent(string id, string targetControl, string eventType, object? data)
//        {
//            if (EventActions.TryGetValue(eventType, out var handler))
//            {
//                return handler.Invoke(id, targetControl, data);
//            }
//            return null;
//        }
//        public void eventHandler(string identifier, string targetControl, string methodName, int type)
//        {
//            var window = Computer.Window;

//            if (window.USER_WINDOW_INSTANCES.TryGetValue(identifier, out var app))
//                app.JavaScriptEngine?.CreateEventHandler(identifier, targetControl, methodName, type);
//        }

//    }
//}