using System;
using System.Collections.Generic;
using WowPacketParser.Enums;
using WowPacketParser.Enums.Version;
using WowPacketParser.Misc;
using WowPacketParser.Parsing;
using WowPacketParser.Store;
using WowPacketParser.Store.Objects;

namespace WowPacketParserModule.V6_0_2_19033.Parsers
{
    public static class SpellHandler
    {
        [Parser(Opcode.SMSG_LEARNED_SPELL)]
        public static void HandleLearnSpell(Packet packet)
        {
            var count = packet.ReadUInt32("Spell Count");

            for (var i = 0; i < count; ++i)
                packet.ReadEntry<Int32>(StoreNameType.Spell, "Spell ID", i);
            packet.ReadBit("Unk Bits");
        }

        [Parser(Opcode.SMSG_INITIAL_SPELLS)]
        public static void HandleInitialSpells(Packet packet)
        {
            packet.ReadBit("Unk Bit");
            var count = packet.ReadUInt32("Spell Count");

            var spells = new List<uint>((int)count);
            for (var i = 0; i < count; i++)
            {
                var spellId = packet.ReadEntry<UInt32>(StoreNameType.Spell, "Spell ID", i);
                spells.Add(spellId);
            }

            var startSpell = new StartSpell { Spells = spells };

            WoWObject character;
            if (Storage.Objects.TryGetValue(WowPacketParser.Parsing.Parsers.SessionHandler.LoginGuid, out character))
            {
                var player = character as Player;
                if (player != null && player.FirstLogin)
                    Storage.StartSpells.Add(new Tuple<Race, Class>(player.Race, player.Class), startSpell, packet.TimeSpan);
            }
        }

        [Parser(Opcode.SMSG_SPELL_CATEGORY_COOLDOWN)]
        public static void HandleSpellCategoryCooldown(Packet packet)
        {
            var count = packet.ReadUInt32("Spell Count");

            for (var i = 0; i < count; ++i)
            {
                packet.ReadInt32("Cooldown", i);
                packet.ReadInt32("Category Cooldown", i);
            }
        }

        [Parser(Opcode.SMSG_SET_FLAT_SPELL_MODIFIER)]
        public static void HandleSetSpellModifierFlat(Packet packet)
        {
            var modCount = packet.ReadUInt32("Modifier type count");
            var modTypeCount = new uint[modCount];

            for (var j = 0; j < modCount; ++j)
            {
                packet.ReadEnum<SpellModOp>("Spell Mod", TypeCode.Byte, j);

                modTypeCount[j] = packet.ReadUInt32("Count", j);
            }

            for (var j = 0; j < modCount; ++j)
            {
                for (var i = 0; i < modTypeCount[j]; ++i)
                {
                    packet.ReadSingle("Amount", j, i);
                    packet.ReadByte("Spell Mask bitpos", j, i);
                }
            }
        }

        [Parser(Opcode.SMSG_SET_PCT_SPELL_MODIFIER)]
        public static void HandleSetSpellModifierPct(Packet packet)
        {
            var modCount = packet.ReadUInt32("Modifier type count");
            var modTypeCount = new uint[modCount];

            for (var j = 0; j < modCount; ++j)
            {
                packet.ReadEnum<SpellModOp>("Spell Mod", TypeCode.Byte, j);

                modTypeCount[j] = packet.ReadUInt32("Count", j);
            }

            for (var j = 0; j < modCount; ++j)
            {
                for (var i = 0; i < modTypeCount[j]; ++i)
                {
                    packet.ReadSingle("Amount", j, i);
                    packet.ReadByte("Spell Mask bitpos", j, i);
                }
            }
        }

        [HasSniffData]
        [Parser(Opcode.SMSG_AURA_UPDATE)]
        public static void HandleAuraUpdate(Packet packet)
        {
            packet.ReadBit("bit16");
            packet.ReadPackedGuid128("Guid");
            var count = packet.ReadUInt32("AuraCount");

            var auras = new List<Aura>();
            for (var i = 0; i < count; ++i)
            {
                var aura = new Aura();

                packet.ReadByte("Slot", i);

                packet.ResetBitReader();
                var hasAura = packet.ReadBit("HasAura");
                if (hasAura)
                {
                    var id = packet.ReadEntry<Int32>(StoreNameType.Spell, "Spell ID", i);
                    aura.AuraFlags = packet.ReadEnum<AuraFlagMoP>("Flags", TypeCode.Byte, i);
                    packet.ReadInt32("Effect Mask", i);
                    packet.ResetBitReader();
                    aura.Level = packet.ReadUInt16("Caster Level", i);
                    aura.Charges = packet.ReadByte("Charges", i);

                    var int72 = packet.ReadUInt32("Int56 Count");
                    var effectCount = packet.ReadUInt32("Effect Count");

                    for (var j = 0; j < int72; ++j)
                        packet.ReadSingle("Float15", i, j);

                    for (var j = 0; j < effectCount; ++j)
                        packet.ReadSingle("Effect Value", i, j);

                    var hasCasterGUID = packet.ReadBit("hasCasterGUID");
                    var hasDuration = packet.ReadBit("hasDuration");
                    var hasMaxDuration = packet.ReadBit("hasMaxDuration");

                    if (hasCasterGUID)
                        packet.ReadPackedGuid128("Caster Guid");

                    if (hasDuration)
                        aura.Duration = packet.ReadInt32("Duration", i);
                    else
                        aura.Duration = 0;

                    if (hasMaxDuration)
                        aura.MaxDuration = packet.ReadInt32("Max Duration", i);
                    else
                        aura.MaxDuration = 0;

                    auras.Add(aura);
                    packet.AddSniffData(StoreNameType.Spell, (int)aura.SpellId, "AURA_UPDATE");
                }
            }
            // To-Do: Fix me
            /*
            if (Storage.Objects.ContainsKey(GUID))
            {
                var unit = Storage.Objects[GUID].Item1 as Unit;
                if (unit != null)
                {
                    // If this is the first packet that sends auras
                    // (hopefully at spawn time) add it to the "Auras" field,
                    // if not create another row of auras in AddedAuras
                    // (similar to ChangedUpdateFields)

                    if (unit.Auras == null)
                        unit.Auras = auras;
                    else
                        unit.AddedAuras.Add(auras);
                }
            }*/
        }

        [Parser(Opcode.SMSG_TALENTS_INFO)]
        public static void ReadTalentInfo(Packet packet)
        {
            packet.ReadByte("Active Spec Group");

            var specCount = packet.ReadInt32("Spec Group count");
            for (var i = 0; i < specCount; ++i)
            {
                packet.ReadUInt32("Spec Id", i);
                var spentTalents = packet.ReadInt32("Spec Talent Count", i);

                for (var j = 0; j < 6; ++j)
                    packet.ReadUInt16("Glyph", i, j);

                for (var j = 0; j < spentTalents; ++j)
                    packet.ReadUInt16("Talent Id", i, j);
            }
        }

        [Parser(Opcode.SMSG_SPELL_START)]
        [Parser(Opcode.SMSG_SPELL_GO)]
        public static void HandleSpellStart(Packet packet)
        {
            packet.ReadPackedGuid128("Caster Guid");
            packet.ReadPackedGuid128("CasterUnit Guid");

            packet.ReadByte("CastID");

            packet.ReadUInt32("SpellID");
            packet.ReadUInt32("CastFlags");
            packet.ReadUInt32("CastTime");

            var int52 = packet.ReadUInt32("HitTargets");
            var int68 = packet.ReadUInt32("MissTargets");
            var int84 = packet.ReadUInt32("MissStatus");

            // SpellTargetData
            packet.ResetBitReader();

            packet.ReadEnum<TargetFlag>("Flags", 21);
            var bit72 = packet.ReadBit("HasSrcLocation");
            var bit112 = packet.ReadBit("HasDstLocation");
            var bit124 = packet.ReadBit("HasOrientation");
            var bits128 = packet.ReadBits(7);

            packet.ReadPackedGuid128("Unit Guid");
            packet.ReadPackedGuid128("Item Guid");

            if (bit72)
            {
                packet.ReadPackedGuid128("SrcLocation Guid");
                packet.ReadVector3("SrcLocation");
            }

            if (bit112)
            {
                packet.ReadPackedGuid128("DstLocation Guid");
                packet.ReadVector3("DstLocation");
            }

            if (bit124)
                packet.ReadSingle("Orientation");

            packet.ReadWoWString("Name", bits128);

            var int360 = packet.ReadUInt32("SpellPowerData");

            // MissileTrajectoryResult
            packet.ReadUInt32("TravelTime");
            packet.ReadSingle("Pitch");

            // SpellAmmo
            packet.ReadUInt32("DisplayID");
            packet.ReadByte("InventoryType");

            packet.ReadByte("DestLocSpellCastIndex");

            var int428 = packet.ReadUInt32("TargetPoints");

            // CreatureImmunities
            packet.ReadUInt32("School");
            packet.ReadUInt32("Value");

            // SpellHealPrediction
            packet.ReadUInt32("Points");
            packet.ReadByte("Type");
            packet.ReadPackedGuid128("BeaconGUID");

            // HitTargets
            for (var i = 0; i < int52; ++i)
                packet.ReadPackedGuid128("HitTarget Guid", i);

            // MissTargets
            for (var i = 0; i < int68; ++i)
                packet.ReadPackedGuid128("MissTarget Guid", i);

            // MissStatus
            for (var i = 0; i < int84; ++i)
            {
                if (packet.ReadBits("Reason", 4, i) == 11)
                    packet.ReadBits("ReflectStatus", 4, i);
            }

            // SpellPowerData
            for (var i = 0; i < int360; ++i)
            {
                packet.ReadInt32("Cost", i);
                packet.ReadEnum<PowerType>("Type", TypeCode.Byte, i);
            }

            // TargetPoints
            for (var i = 0; i < int428; ++i)
            {
                packet.ReadPackedGuid128("Transport Guid");
                packet.ReadVector3("Location");
            }

            packet.ResetBitReader();

            packet.ReadBits("CastFlagsEx", 18);

            var bit396 = packet.ReadBit("HasRuneData");
            var bit424 = packet.ReadBit("HasProjectileVisual");

            // RuneData
            if (bit396)
            {
                packet.ReadByte("Start");
                packet.ReadByte("Count");

                packet.ResetBitReader();
                var bits1 = packet.ReadBits("CooldownCount", 3);

                for (var i = 0; i < bits1; ++i)
                    packet.ReadByte("Cooldowns", i);
            }

            // ProjectileVisual
            if (bit424)
                for (var i = 0; i < 2; ++i)
                    packet.ReadInt32("Id", i);

            if (packet.Opcode == Opcodes.GetOpcode(Opcode.SMSG_SPELL_START))
                return;

            ReadSpellCastLogData(ref packet);
        }

        private static void ReadSpellCastLogData(ref Packet packet)
        {

            packet.ResetBitReader();
            var bit52 = packet.ReadBit("SpellCastLogData");

            // SpellCastLogData
            if (bit52)
            {
                packet.ReadInt32("Health");
                packet.ReadInt32("AttackPower");
                packet.ReadInt32("SpellPower");

                var int3 = packet.ReadInt32("SpellLogPowerData");

                // SpellLogPowerData
                for (var i = 0; int3 < 2; ++i)
                {
                    packet.ReadInt32("PowerType", i);
                    packet.ReadInt32("Amount", i);
                }

                var bit32 = packet.ReadBit("bit32");

                if (bit32)
                    packet.ReadSingle("Float7");
            }
        }
    }
}