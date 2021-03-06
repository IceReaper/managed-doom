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

	public static class ConfigUtilities
	{
		public static string GetDefaultIwadPath()
		{
			var names = new[] {"DOOM2.WAD", "PLUTONIA.WAD", "TNT.WAD", "DOOM.WAD", "DOOM1.WAD", "FREEDOOM2.WAD", "FREEDOOM1.WAD"};

			foreach (var name in names)
			{
				if (DoomApplication.Instance.FileSystem.Exists(name))
					return name;
			}

			throw new Exception("No IWAD was found!");
		}
	}
}
