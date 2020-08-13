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
	using Common;
	using Info;
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Runtime.ExceptionServices;
	using Wad;

	public sealed class TextureLookup : IReadOnlyList<Texture>
    {
        private List<Texture> textures;
        private Dictionary<string, Texture> nameToTexture;
        private Dictionary<string, int> nameToNumber;

        private int[] switchList;

        public TextureLookup(Wad wad) : this(wad, false)
        {
        }

        public TextureLookup(Wad wad, bool useDummy)
        {
            if (!useDummy)
            {
                this.Init(wad);
            }
            else
            {
                this.InitDummy(wad);
            }

            this.InitSwitchList();
        }

        private void Init(Wad wad)
        {
            try
            {
                Console.Write("Load textures: ");

                this.textures = new List<Texture>();
                this.nameToTexture = new Dictionary<string, Texture>();
                this.nameToNumber = new Dictionary<string, int>();

                var patches = TextureLookup.LoadPatches(wad);

                for (var n = 1; n <= 2; n++)
                {
                    var lumpNumber = wad.GetLumpNumber("TEXTURE" + n);
                    if (lumpNumber == -1)
                    {
                        break;
                    }

                    var data = wad.ReadLump(lumpNumber);
                    var count = BitConverter.ToInt32(data, 0);
                    for (var i = 0; i < count; i++)
                    {
                        var offset = BitConverter.ToInt32(data, 4 + 4 * i);
                        var texture = Texture.FromData(data, offset, patches);
                        this.nameToNumber.Add(texture.Name, this.textures.Count);
                        this.textures.Add(texture);
                        this.nameToTexture.Add(texture.Name, texture);
                    }
                }

                Console.WriteLine("OK (" + this.nameToTexture.Count + " textures)");
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed");
                ExceptionDispatchInfo.Throw(e);
            }
        }

        private void InitDummy(Wad wad)
        {
            this.textures = new List<Texture>();
            this.nameToTexture = new Dictionary<string, Texture>();
            this.nameToNumber = new Dictionary<string, int>();

            for (var n = 1; n <= 2; n++)
            {
                var lumpNumber = wad.GetLumpNumber("TEXTURE" + n);
                if (lumpNumber == -1)
                {
                    break;
                }

                var data = wad.ReadLump(lumpNumber);
                var count = BitConverter.ToInt32(data, 0);
                for (var i = 0; i < count; i++)
                {
                    var offset = BitConverter.ToInt32(data, 4 + 4 * i);
                    var name = Texture.GetName(data, offset);
                    var height = Texture.GetHeight(data, offset);
                    var texture = Dummy.GetTexture(height);
                    this.nameToNumber.Add(name, this.textures.Count);
                    this.textures.Add(texture);
                    this.nameToTexture.Add(name, texture);
                }
            }
        }

        private void InitSwitchList()
        {
            var list = new List<int>();
            foreach (var tuple in DoomInfo.SwitchNames)
            {
                var texNum1 = this.GetNumber(tuple.Item1);
                var texNum2 = this.GetNumber(tuple.Item2);
                if (texNum1 != -1 && texNum2 != -1)
                {
                    list.Add(texNum1);
                    list.Add(texNum2);
                }
            }
            this.switchList = list.ToArray();
        }

        public int GetNumber(string name)
        {
            if (name[0] == '-')
            {
                return 0;
            }

            int number;
            if (this.nameToNumber.TryGetValue(name, out number))
            {
                return number;
            }
            else
            {
                return -1;
            }
        }

        private static Patch[] LoadPatches(Wad wad)
        {
            var patchNames = TextureLookup.LoadPatchNames(wad);
            var patches = new Patch[patchNames.Length];
            for (var i = 0; i < patches.Length; i++)
            {
                var name = patchNames[i];

                // This check is necessary to avoid crash in DOOM1.WAD.
                if (wad.GetLumpNumber(name) == -1)
                {
                    continue;
                }

                var data = wad.ReadLump(name);
                patches[i] = Patch.FromData(name, data);
            }
            return patches;
        }

        private static string[] LoadPatchNames(Wad wad)
        {
            var data = wad.ReadLump("PNAMES");
            var count = BitConverter.ToInt32(data, 0);
            var names = new string[count];
            for (var i = 0; i < names.Length; i++)
            {
                names[i] = DoomInterop.ToString(data, 4 + 8 * i, 8);
            }
            return names;
        }

        public IEnumerator<Texture> GetEnumerator()
        {
            return this.textures.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.textures.GetEnumerator();
        }

        public int Count => this.textures.Count;
        public Texture this[int num] => this.textures[num];
        public Texture this[string name] => this.nameToTexture[name];
        public int[] SwitchList => this.switchList;
    }
}