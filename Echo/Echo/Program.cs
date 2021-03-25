// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;

namespace Echo
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                throw new Exception("Require at least one argument.");
            }

            Console.WriteLine(args[0]);

            if (args.Length > 1)
            {
                Console.Error.WriteLine(args[1]);
            }
        }
    }
}
