using System.Collections.Generic;

namespace SysBot.Pokemon
{
    /// <summary>
    /// Pokémon Scarlet/Violet RAM offsets
    /// </summary>
    public class PokeDataOffsetsSV
    {
        public const string ScarletID = "0100A3D008C5C000";
        public const string VioletID  = "01008F6008C5E000";
        public IReadOnlyList<long> BoxStartPokemonPointer { get; } = new long[] { 0x44A98C8, 0x130, 0x9B0, 0x00 };
        public IReadOnlyList<long> LinkTradePartnerPokemonPointer { get; } = new long[] { 0x44C7730, 0x38, 0x148 };
        public IReadOnlyList<long> LinkTradePartnerNIDPointer { get; } = new long[] { 0x44CCB40, 0x8 };
        public IReadOnlyList<long> MyStatusPointer { get; } = new long[] { 0x44CCA68, 0xE0, 0x40 };
        public IReadOnlyList<long> Trader1MyStatusPointer { get; } = new long[] { 0x44CCBB0, 0x28, 0xB0, 0x0 }; // The trade partner status uses a compact struct that looks like MyStatus.
        public IReadOnlyList<long> Trader2MyStatusPointer { get; } = new long[] { 0x44CCBB0, 0x28, 0xE0, 0x0 };
        public IReadOnlyList<long> ConfigPointer { get; } = new long[] { 0x44CCA58, 0x70 };
        public IReadOnlyList<long> CurrentBoxPointer { get; } = new long[] { 0x44CCA68, 0x108, 0x570 };//[[main+44CCA68]+108]+570
        public IReadOnlyList<long> PortalBoxStatusPointer { get; } = new long[] { 0x44C2DF8, 0x240, 0x2B0, 0x28 };  // 9-A in portal, 4-6 in box.
        public IReadOnlyList<long> IsConnectedPointer { get; } = new long[] { 0x44A2AC8, 0x30 };
        public IReadOnlyList<long> OverworldPointer { get; } = new long[] { 0x44CCAC8, 0x140, 0xE8, 0x28 };

        public const int BoxFormatSlotSize = 0x158;
        public const string LibAppletWeID = "010000000000100a"; // One of the process IDs for the news.
    }
}
