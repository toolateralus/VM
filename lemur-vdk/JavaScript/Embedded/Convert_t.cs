using Lemur.FS;
using Lemur.JavaScript.Api;
using Microsoft.ClearScript.JavaScript;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Lemur.JS.Embedded
{
    public class Convert_t : embedable
    {
        [ApiDoc("convert a base64 string to a utf8 string.")]
        public string utf8FromBase64(string base64) => Encoding.UTF8.GetString((byte[])toBytes(base64));
        [ApiDoc("convert a base64 string to a byte array.")]
        public object toBytes(string base64) => Convert.FromBase64String(base64);

        [ApiDoc("convert a byte array to a base 64 string")]
        public string toBase64(object inBytes)
        {
            List<byte> bytes = new List<byte>();

            Interop_t.ForEachCast<int>(bytes.ToEnumerable(), (data) => bytes.Add((byte)data));

            return Convert.ToBase64String(bytes.ToArray());
        }
        /// <summary>
        /// Opens a file, reads its bytes contents, converts it to a base64 string and
        /// returns it. great for loading images into java script and keeping data transfer lightweight
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        [ApiDoc("load a file from a path, and return it's contents as a base64 string")]
        public string? base64FromFile(string path)
        {
            byte[] imageData = null;

            if (!File.Exists(path))
            {
                if (FileSystem.GetResourcePath(path) is string absPath && !string.IsNullOrEmpty(absPath))
                    imageData = File.ReadAllBytes(absPath);
            }
            else
            {
                imageData = File.ReadAllBytes(path);
            }

            if (imageData != null)
                return Convert.ToBase64String(imageData);

            return null!;
        }
        public string getType(object? obj)
        {
            return obj?.GetType().Name ?? "null";
        }
        public float toFloat(double num) {
            return (float)num;
        }
    }
}

