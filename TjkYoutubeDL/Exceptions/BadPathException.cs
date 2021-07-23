using System;
using System.Collections.Generic;
using System.Text;

namespace TjkYoutubeDL.Exceptions
{
    public class BadPathException : Exception
    {
        public BadPathException(string message) : base("file not found: " + message)
        {

        }

    }
}
