﻿//
// Copyright (C) 1993-1996 Id Software, Inc.
// Copyright (C) 2019-2020 Nobuaki Tanaka
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//

namespace DoomEngine
{
	using System;

	public static class Program
    {
        public static void Main(string[] args)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.BackgroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine(ApplicationInfo.Title);
            Console.ResetColor();

#if DEBUG
            Program.Main_Debug(args);
#else
            Main_Release(args);
#endif
        }

        private static void Main_Release(string[] args)
        {
            try
            {
                string quitMessage = null;

                using (var app = new DoomApplication(new CommandLineArgs(args)))
                {
                    app.Run();
                    quitMessage = app.QuitMessage;
                }

                if (quitMessage != null)
                {
                    Console.WriteLine(quitMessage);
                    Console.Write("Press any key to exit.");
                    Console.ReadKey();
                }
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(e);
                Console.ResetColor();
                Console.Write("Press any key to exit.");
                Console.ReadKey();
            }
        }

        private static void Main_Debug(string[] args)
        {
            using (var app = new DoomApplication(new CommandLineArgs(args)))
            {
                app.Run();
            }
        }
    }
}
