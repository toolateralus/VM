using System;
using System.Runtime.Serialization;

namespace Lemur.JS.Embedded
{
    [Serializable]
    internal class ComputerNotFoundException : Exception
    {
        public ComputerNotFoundException()
        {
        }

        public ComputerNotFoundException(string? message) : base(message)
        {
        }

        public ComputerNotFoundException(string? message, Exception? innerException) : base(message, innerException)
        {
        }

        protected ComputerNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}