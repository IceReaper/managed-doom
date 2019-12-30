﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;

namespace ManagedDoom
{
    public sealed class Texture
    {
        private string name;
        private bool masked;
        private int width;
        private int height;
        private TexturePatch[] patches;
        private Patch composite;

        public Texture(
            string name,
            bool masked,
            int width,
            int height,
            TexturePatch[] patches)
        {
            this.name = name;
            this.masked = masked;
            this.width = width;
            this.height = height;
            this.patches = patches;
            composite = GenerateComposite(name, width, height, patches);
        }

        public static Texture FromData(byte[] data, int offset, Patch[] patchLookup)
        {
            var name = DoomInterop.ToString(data, offset, 8);
            var masked = BitConverter.ToInt32(data, offset + 8);
            var width = BitConverter.ToInt16(data, offset + 12);
            var height = BitConverter.ToInt16(data, offset + 14);
            var patchCount = BitConverter.ToInt16(data, offset + 20);
            var patches = new TexturePatch[patchCount];
            for (var i = 0; i < patchCount; i++)
            {
                var patchOffset = offset + 22 + TexturePatch.DataSize * i;
                patches[i] = TexturePatch.FromData(data, patchOffset, patchLookup);
            }

            return new Texture(
                name,
                masked != 0,
                width,
                height,
                patches);
        }

        private static Patch GenerateComposite(string name, int width, int height, TexturePatch[] patches)
        {
            var patchCount = new int[width];
            var columns = new Column[width][];
            var compositeColumnCount = 0;

            foreach (var patch in patches)
            {
                var left = patch.OriginX;
                var right = left + patch.Width;

                var start = Math.Max(left, 0);
                var end = Math.Min(right, width);

                for (var x = start; x < end; x++)
                {
                    patchCount[x]++;
                    if (patchCount[x] == 2)
                    {
                        compositeColumnCount++;
                    }
                    columns[x] = patch.Columns[x - patch.OriginX];
                }
            }

            var padding = Math.Max(128 - height, 0);
            var data = new byte[height * compositeColumnCount + padding];
            var i = 0;
            for (var x = 0; x < width; x++)
            {
                if (patchCount[x] == 0)
                {
                    throw new Exception();
                }

                if (patchCount[x] >= 2)
                {
                    var column = new Column(0, data, height * i, height);

                    foreach (var patch in patches)
                    {
                        var px = x - patch.OriginX;
                        if (px < 0 || px >= patch.Width)
                        {
                            continue;
                        }
                        var patchColumn = patch.Columns[px];
                        DrawColumnInCache(
                            patchColumn,
                            column.Data,
                            column.Offset,
                            patch.OriginY,
                            height);
                    }

                    columns[x] = new[] { column };

                    i++;
                }
            }

            return new Patch(name, width, height, 0, 0, columns);
        }

        private static void DrawColumnInCache(
            Column[] source,
            byte[] destination,
            int destinationOffset,
            int destinationY,
            int destinationHeight)
        {
            foreach (var column in source)
            {
                var sourceIndex = column.Offset;
                var destinationIndex = destinationOffset + destinationY + column.TopDelta;
                var length = column.Length;

                var topExceedance = -(destinationY + column.TopDelta);
                if (topExceedance > 0)
                {
                    sourceIndex += topExceedance;
                    destinationIndex += topExceedance;
                    length -= topExceedance;
                }

                var bottomExceedance = destinationY + column.TopDelta + column.Length - destinationHeight;
                if (bottomExceedance > 0)
                {
                    length -= bottomExceedance;
                }

                if (length > 0)
                {
                    Array.Copy(
                        column.Data,
                        sourceIndex,
                        destination,
                        destinationIndex,
                        length);
                }
            }
        }

        /*
        public Bitmap ToBitmap(byte[] palette)
        {
            return composite.ToBitmap(palette);
        }
        */

        public override string ToString()
        {
            return name;
        }

        public string Name => name;
        public bool Masked => masked;
        public int Width => width;
        public int Height => height;
        public IReadOnlyList<TexturePatch> Patches => patches;
        public Patch Composite => composite;
    }
}
