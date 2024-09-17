using System;

namespace Lemur.JS.Embedded {
    public class embedable
    {
        public static Computer GetComputer() => Computer.Current;
    }
}