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

namespace DoomEngine.Doom.Graphics
{
	using System;
	using System.Collections.Generic;
	using System.IO;

	public sealed class Patch
	{
		private string name;
		private int width;
		private int height;
		private int leftOffset;
		private int topOffset;
		private Column[][] columns;

		public Patch(string name, int width, int height, int leftOffset, int topOffset, Column[][] columns)
		{
			this.name = name;
			this.width = width;
			this.height = height;
			this.leftOffset = leftOffset;
			this.topOffset = topOffset;
			this.columns = columns;
		}

		public static Patch FromData(string name, byte[] data)
		{
			var width = BitConverter.ToInt16(data, 0);
			var height = BitConverter.ToInt16(data, 2);
			var leftOffset = BitConverter.ToInt16(data, 4);
			var topOffset = BitConverter.ToInt16(data, 6);

			Patch.PadData(ref data, width);

			var columns = new Column[width][];

			for (var x = 0; x < width; x++)
			{
				var cs = new List<Column>();
				var p = BitConverter.ToInt32(data, 8 + 4 * x);

				while (true)
				{
					var topDelta = data[p];

					if (topDelta == Column.Last)
					{
						break;
					}

					var length = data[p + 1];
					var offset = p + 3;
					cs.Add(new Column(topDelta, data, offset, length));
					p += length + 4;
				}

				columns[x] = cs.ToArray();
			}

			return new Patch(name, width, height, leftOffset, topOffset, columns);
		}

		public static Patch FromWad(string name)
		{
			var reader = new BinaryReader(DoomApplication.Instance.FileSystem.Read(name));

			return Patch.FromData(name, reader.ReadBytes((int) reader.BaseStream.Length));
		}

		private static void PadData(ref byte[] data, int width)
		{
			var need = 0;

			for (var x = 0; x < width; x++)
			{
				var p = BitConverter.ToInt32(data, 8 + 4 * x);

				while (true)
				{
					var topDelta = data[p];

					if (topDelta == Column.Last)
					{
						break;
					}

					var length = data[p + 1];
					var offset = p + 3;
					need = Math.Max(offset + 128, need);
					p += length + 4;
				}
			}

			if (data.Length < need)
			{
				Array.Resize(ref data, need);
			}
		}

		public override string ToString()
		{
			return this.name;
		}

		public string Name => this.name;
		public int Width => this.width;
		public int Height => this.height;
		public int LeftOffset => this.leftOffset;
		public int TopOffset => this.topOffset;
		public Column[][] Columns => this.columns;
	}
}
