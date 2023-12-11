using Lemur.FS;
using Lemur.JavaScript.Api;
using Microsoft.ClearScript.JavaScript;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Lemur.JS.Embedded
{
    public class conv
    {
        public string utf8FromBase64(string base64)
        {
            return Encoding.UTF8.GetString((byte[])toBytes(base64));
        }
        public object toBytes(string base64) => Convert.FromBase64String(base64);
        public string toBase64(object ints)
        {
            List<byte> bytes = new List<byte>();

            interop.ForEachCast<int>(ints.ToEnumerable(), (data) => bytes.Add((byte)data));

            return Convert.ToBase64String(bytes.ToArray());
        }
        /// <summary>
        /// Opens a file, reads its bytes contents, converts it to a base64 string and
        /// returns it. great for loading images into java script and keeping data transfer lightweight
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
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
    }
}

