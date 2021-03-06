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

namespace DoomEngine.Doom.World
{
	using Common;
	using DoomEngine.Game;
	using DoomEngine.Game.Components;
	using DoomEngine.Game.Components.Items;
	using DoomEngine.Game.Components.Weapons;
	using Game;
	using Math;
	using System.Collections.Generic;
	using System.Linq;

	public sealed class StatusBar
	{
		private World world;

		// Used for appopriately pained face.
		private int oldHealth;

		// Used for evil grin.
		private List<Entity> oldWeaponsOwned = new List<Entity>();

		// Count until face changes.
		private int faceCount;

		// Current face index.
		private int faceIndex;

		// A random number per tick.
		private int randomNumber;

		private int priority;

		private int lastAttackDown;
		private int lastPainOffset;

		private DoomRandom random;

		public StatusBar(World world)
		{
			this.world = world;

			this.oldHealth = -1;
			this.oldWeaponsOwned.Clear();

			this.oldWeaponsOwned.AddRange(
				world.Options.Player.Entity.GetComponent<InventoryComponent>().Items.Where(item => item.GetComponent<WeaponComponent>() != null)
			);

			this.faceCount = 0;
			this.faceIndex = 0;
			this.randomNumber = 0;
			this.priority = 0;
			this.lastAttackDown = -1;
			this.lastPainOffset = 0;

			this.random = new DoomRandom();
		}

		public void Reset()
		{
			this.oldHealth = -1;

			this.oldWeaponsOwned.AddRange(
				this.world.Options.Player.Entity.GetComponent<InventoryComponent>().Items.Where(item => item.GetComponent<WeaponComponent>() != null)
			);

			this.faceCount = 0;
			this.faceIndex = 0;
			this.randomNumber = 0;
			this.priority = 0;
			this.lastAttackDown = -1;
			this.lastPainOffset = 0;
		}

		public void Update()
		{
			this.randomNumber = this.random.Next();
			this.UpdateFace();
		}

		private void UpdateFace()
		{
			var player = this.world.Options.Player;
			var healthComponent = player.Entity.GetComponent<Health>();

			if (this.priority < 10)
			{
				// Dead.
				if (healthComponent.Current == 0)
				{
					this.priority = 9;
					this.faceIndex = Face.DeadIndex;
					this.faceCount = 1;
				}
			}

			if (this.priority < 9)
			{
				if (player.BonusCount != 0)
				{
					// Picking up bonus.
					var doEvilGrin = false;

					foreach (var entity in player.Entity.GetComponent<InventoryComponent>()
						.Items.Where(entity => entity.GetComponent<WeaponComponent>() != null && !this.oldWeaponsOwned.Contains(entity)))
					{
						doEvilGrin = true;
						this.oldWeaponsOwned.Add(entity);
					}

					if (doEvilGrin)
					{
						// Evil grin if just picked up weapon.
						this.priority = 8;
						this.faceCount = Face.EvilGrinDuration;
						this.faceIndex = this.CalcPainOffset() + Face.EvilGrinOffset;
					}
				}
			}

			if (this.priority < 8)
			{
				if (player.DamageCount != 0 && player.Attacker != null && player.Attacker != player.Mobj)
				{
					// Being attacked.
					this.priority = 7;

					if (healthComponent.Current - this.oldHealth > Face.MuchPain)
					{
						this.faceCount = Face.TurnDuration;
						this.faceIndex = this.CalcPainOffset() + Face.OuchOffset;
					}
					else
					{
						var attackerAngle = Geometry.PointToAngle(player.Mobj.X, player.Mobj.Y, player.Attacker.X, player.Attacker.Y);

						Angle diff;
						bool right;

						if (attackerAngle > player.Mobj.Angle)
						{
							// Whether right or left.
							diff = attackerAngle - player.Mobj.Angle;
							right = diff > Angle.Ang180;
						}
						else
						{
							// Whether left or right.
							diff = player.Mobj.Angle - attackerAngle;
							right = diff <= Angle.Ang180;
						}

						this.faceCount = Face.TurnDuration;
						this.faceIndex = this.CalcPainOffset();

						if (diff < Angle.Ang45)
						{
							// Head-on.
							this.faceIndex += Face.RampageOffset;
						}
						else if (right)
						{
							// Turn face right.
							this.faceIndex += Face.TurnOffset;
						}
						else
						{
							// Turn face left.
							this.faceIndex += Face.TurnOffset + 1;
						}
					}
				}
			}

			if (this.priority < 7)
			{
				// Getting hurt because of your own damn stupidity.
				if (player.DamageCount != 0)
				{
					if (healthComponent.Current - this.oldHealth > Face.MuchPain)
					{
						this.priority = 7;
						this.faceCount = Face.TurnDuration;
						this.faceIndex = this.CalcPainOffset() + Face.OuchOffset;
					}
					else
					{
						this.priority = 6;
						this.faceCount = Face.TurnDuration;
						this.faceIndex = this.CalcPainOffset() + Face.RampageOffset;
					}
				}
			}

			if (this.priority < 6)
			{
				// Rapid firing.
				if (player.AttackDown)
				{
					if (this.lastAttackDown == -1)
					{
						this.lastAttackDown = Face.RampageDelay;
					}
					else if (--this.lastAttackDown == 0)
					{
						this.priority = 5;
						this.faceIndex = this.CalcPainOffset() + Face.RampageOffset;
						this.faceCount = 1;
						this.lastAttackDown = 1;
					}
				}
				else
				{
					this.lastAttackDown = -1;
				}
			}

			if (this.priority < 5)
			{
				// Invulnerability.
				if ((player.Cheats & CheatFlags.GodMode) != 0 || player.Powers[(int) PowerType.Invulnerability] != 0)
				{
					this.priority = 4;

					this.faceIndex = Face.GodIndex;
					this.faceCount = 1;
				}
			}

			// Look left or look right if the facecount has timed out.
			if (this.faceCount == 0)
			{
				this.faceIndex = this.CalcPainOffset() + (this.randomNumber % 3);
				this.faceCount = Face.StraightFaceDuration;
				this.priority = 0;
			}

			this.faceCount--;
		}

		private int CalcPainOffset()
		{
			var player = this.world.Options.Player;
			var healthComponent = player.Entity.GetComponent<Health>();

			var health = healthComponent.Current > 100 ? 100 : healthComponent.Current;

			if (health != this.oldHealth)
			{
				this.lastPainOffset = Face.Stride * (((100 - health) * Face.PainFaceCount) / 101);
				this.oldHealth = health;
			}

			return this.lastPainOffset;
		}

		public int FaceIndex => this.faceIndex;

		public static class Face
		{
			public static readonly int PainFaceCount = 5;
			public static readonly int StraightFaceCount = 3;
			public static readonly int TurnFaceCount = 2;
			public static readonly int SpecialFaceCount = 3;

			public static readonly int Stride = Face.StraightFaceCount + Face.TurnFaceCount + Face.SpecialFaceCount;
			public static readonly int ExtraFaceCount = 2;
			public static readonly int FaceCount = Face.Stride * Face.PainFaceCount + Face.ExtraFaceCount;

			public static readonly int TurnOffset = Face.StraightFaceCount;
			public static readonly int OuchOffset = Face.TurnOffset + Face.TurnFaceCount;
			public static readonly int EvilGrinOffset = Face.OuchOffset + 1;
			public static readonly int RampageOffset = Face.EvilGrinOffset + 1;
			public static readonly int GodIndex = Face.PainFaceCount * Face.Stride;
			public static readonly int DeadIndex = Face.GodIndex + 1;

			public static readonly int EvilGrinDuration = (2 * GameConst.TicRate);
			public static readonly int StraightFaceDuration = (GameConst.TicRate / 2);
			public static readonly int TurnDuration = (1 * GameConst.TicRate);
			public static readonly int OuchDuration = (1 * GameConst.TicRate);
			public static readonly int RampageDelay = (2 * GameConst.TicRate);

			public static readonly int MuchPain = 20;
		}
	}
}
