using Microsoft.ClearScript;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using System.Drawing;
using System.Collections.Concurrent;

namespace Lemur.JS
{
    public class interop
    {
        internal Action<string, object?>? OnModuleExported;
        internal Action<string>? OnModuleImported;

        public static double random(double  max)
        {
            return Random.Shared.NextDouble() * max;
        }
        /// <summary>
        /// A non-throwing foreach over a collection of objects, running action on each object.
        /// a try catch prints exceptions but ignores them.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <param name="action"></param>
        public static void ForEachCast<T>(IEnumerable<object> source, Action<T> action)
        {
            try
            {
                foreach (var item in source)
                {
                    T instance = TryCast<T>(item, out var success);

                    if (success)
                        action(instance);
                }
            }
            catch (Exception e)
            {
                if (e is not ObjectDisposedException ode)
                {
                    Notifications.Exception(e);
                }
            }
        }
        public static T TryCast<T>(object item, out bool success)
        {
            success = false;
            if (item is T instance)
            {
                success = true;
                return instance;
            }
            return default;
        }
        public async void sleep(int ms)
        {
            await Task.Delay(ms);
        }
        public void require(string path)
        {
            Computer.Current.JavaScript.ImportModule(path);
        }
        public void export(string id, object? obj)
        {
            OnModuleExported?.Invoke(id, obj);
        }

    }
}

