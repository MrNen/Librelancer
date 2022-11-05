﻿// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Collections.Generic;
using System.Text;
using LibreLancer.Ini;
    
namespace LibreLancer.Data.Save
{
    public class PlayerEquipment
    {
        public HashValue Item;
        public string Hardpoint;
        public float Unknown = 1; //Either health or count, not sure
        public PlayerEquipment() { }
        public PlayerEquipment(Entry e)
        {
            var s = e[0].ToString();
            if (!uint.TryParse(s, out uint hash)) Item = new HashValue(s);
            else Item = hash;
            if (e.Count < 2) return;
            //Extra
            Hardpoint = e[1].ToString();
            if (e.Count > 2) Unknown = e[2].ToSingle();
        }

        public string ToString(string ename)
        {
            return $"{ename} = {(uint)Item}, {Hardpoint}, {Unknown}";
        }

        public override string ToString() => ToString("equip");
    }

    public class PlayerCargo
    {
        //hash, count, percentage_health, UNK, mission_cargo
        public HashValue Item;
        public float PercentageHealth = 1;
        public int Count;
        public bool IsMissionCargo;
        //Some unknowns here
        public PlayerCargo() { }
        public PlayerCargo(Entry e)
        {
            var s = e[0].ToString();
            if (!uint.TryParse(s, out uint hash)) Item = new HashValue(s);
            else Item = hash;
            Count = e[1].ToInt32();
            if (e.Count > 2)
                PercentageHealth = e[2].ToSingle();
            if (e.Count > 4)
                IsMissionCargo = e[4].ToBoolean();
        }

        public string ToString(string ename)
        {
            string hStr = "";
            if (PercentageHealth < 1) hStr = PercentageHealth.ToString(CultureInfo.InvariantCulture);
            return $"{ename} = {(uint)Item}, {Count}, {hStr}, , {(IsMissionCargo ? 1 : 0)}";
        }

        public override string ToString() => ToString("cargo");
    }


    public class SavePlayer : ICustomEntryHandler, IWriteSection
    {
        
        
        [Entry("descrip_strid")] public int DescripStrid;

        //HandleEntry(description)
        public string Description;

        //HandleEntry (tstamp)
        public DateTime? TimeStamp;

        //HandleEntry (name)
        public string Name;
        [Entry("rank")] public string Rank;

        [Entry("money")] public long Money;

        [Entry("num_kills")] public int NumKills;
        [Entry("num_misn_successes")] public int NumMissionSuccesses;
        [Entry("num_misn_failures")] public int NumMissionFailures;

        //HandleEntry(house)
        public List<SaveRep> House = new List<SaveRep>();

        [Entry("voice")] public string Voice;
        [Entry("costume")] public string Costume;
        [Entry("com_costume")] public string ComCostume;
        [Entry("com_body")] public HashValue ComBody;
        [Entry("com_head")] public HashValue ComHead;
        [Entry("com_lefthand")] public HashValue ComLeftHand;
        [Entry("com_righthand")] public HashValue ComRightHand;
        [Entry("body")] public HashValue Body;
        [Entry("head")] public HashValue Head;
        [Entry("lefthand")] public HashValue LeftHand;
        [Entry("righthand")] public HashValue RightHand;

        [Entry("system")] public string System;
        [Entry("base")] public string Base;
        [Entry("pos")] public Vector3 Position;
        [Entry("rotate")] public Vector3 Rotate;

        [Entry("location")] public int Location;

        [Entry("ship_archetype")] public HashValue ShipArchetype;

        //HandleEntry(equip)
        public List<PlayerEquipment> Equip = new List<PlayerEquipment>();

        //HandleEntry(cargo)
        public List<PlayerCargo> Cargo = new List<PlayerCargo>();
        //HandleEntry(visit)


        private static readonly CustomEntry[] _custom = new CustomEntry[]
        {
            new("tstamp", (h, e) =>  ((SavePlayer)h).TimeStamp =  DateTime.FromFileTime(e[0].ToInt64() << 32 | e[1].ToInt64())),
            new("house", (h, e) => ((SavePlayer)h).House.Add(new SaveRep(e))),
            new("description", (h,e) => ((SavePlayer)h).HandleDescription(e)),
            new("log", CustomEntry.Ignore),
            new("visit", CustomEntry.Ignore),
            new("name", (h,e) =>((SavePlayer)h).HandleName(e)),
            new("equip", (h, e) => ((SavePlayer)h).Equip.Add(new PlayerEquipment(e))),
            new("cargo", (h, e) => ((SavePlayer)h).Cargo.Add(new PlayerCargo(e))),
        };
        IEnumerable<CustomEntry> ICustomEntryHandler.CustomEntries => _custom;
        
        [Entry("interface")] public int Interface;
        void HandleDescription(Entry e)
        {
            try
            {
                var bytes = SplitInGroups(e[0].ToString(), 2).Select(x => byte.Parse(x, NumberStyles.HexNumber)).ToArray();
                Description = Encoding.BigEndianUnicode.GetString(bytes);
            }
            catch (Exception)
            {
                Description = string.Join(',', e.Select(x => x.ToString()));
            }
        }
        void HandleName(Entry e)
        {
            try
            {
                var bytes = SplitInGroups(e[0].ToString(), 2).Select(x => byte.Parse(x, NumberStyles.HexNumber)).ToArray();
                Name = Encoding.BigEndianUnicode.GetString(bytes);
            }
            catch (Exception)
            {
                Name = string.Join(',', e.Select(x => x.ToString()));
            }
        }

        public static string EncodeName(string name)
        {
            var bytes = Encoding.BigEndianUnicode.GetBytes(name);
            var builder = new StringBuilder();
            foreach (var b in bytes)
                builder.Append(b.ToString("X2"));
            return builder.ToString();
        }

        static IEnumerable<string> SplitInGroups(string original, int size)
        {
            var p = 0;
            var l = original.Length;
            while (l - p > size)
            {
                yield return original.Substring(p, size);
                p += size;
            }
            var s = original.Substring(p);
            if (!string.IsNullOrWhiteSpace(s) && !string.IsNullOrEmpty(s)) yield return s;
        }

        public void WriteTo(StringBuilder builder)
        {
            builder.AppendLine("[Player]");
            if (DescripStrid != 0)
                builder.AppendEntry("descrip_strid", DescripStrid);
            if (!string.IsNullOrWhiteSpace(Description))
                builder.AppendEntry("description", EncodeName(Description));
            builder.AppendLine();
            //Timestamp
            var fileTime = TimeStamp?.ToFileTime();
            builder.Append("tstamp = ");
            builder.Append((fileTime >> 32).ToString());
            builder.Append(", ");
            builder.AppendLine((fileTime & 0xFFFFFFFF).ToString());
            //
            if (!string.IsNullOrWhiteSpace(Name))
                builder.AppendEntry("name", EncodeName(Name));
            builder.AppendEntry("rank", Rank);
            foreach (var h in House) {
                builder.AppendEntry("house", h.Reputation, h.Group);
            }

            builder.AppendEntry("money", Money);
            builder.AppendEntry("num_kills", NumKills);
            builder.AppendEntry("num_misn_successes", NumMissionSuccesses);
            builder.AppendEntry("num_misn_failures", NumMissionFailures);
            builder.AppendLine();
            builder.AppendEntry("voice", Voice);
            builder.AppendEntry("com_body", ComBody);
            builder.AppendEntry("com_head", ComHead);
            builder.AppendEntry("com_lefthand", ComLeftHand);
            builder.AppendEntry("com_righthand", ComRightHand);
            builder.AppendEntry("body", Body);
            builder.AppendEntry("head", Head);
            builder.AppendEntry("lefthand", LeftHand);
            builder.AppendEntry("righthand", RightHand);
            builder.AppendLine();
            builder.AppendEntry("system", System);
            builder.AppendEntry("base", Base);
            if (string.IsNullOrWhiteSpace(Base))
            {
                builder.AppendEntry("pos", Position);
                builder.AppendEntry("rot", Rotate);
            }
            builder.AppendEntry("location", (uint)Location, false);
            builder.AppendEntry("ship_archetype", ShipArchetype, false);
            builder.AppendLine();
            foreach (var e in Equip)
                builder.AppendLine(e.ToString());
            foreach (var c in Cargo)
                builder.AppendLine(c.ToString());
            builder.AppendLine();
            builder.AppendEntry("interface", Interface);
            builder.AppendLine();
        }
    }
}
