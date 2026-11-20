using System.Collections.Generic;
using UnityEngine;

namespace Frontend.UI
{
    /// <summary>Position of a child in the logical grid, before start-corner mirroring and alignment.</summary>
    internal struct GridCell
    {
        /// <summary>First slot index inside its line.</summary>
        public int Slot;
        /// <summary>Line index.</summary>
        public int Line;
        /// <summary>Slots taken along the start axis.</summary>
        public int Span;
    }

    /// <summary>
    /// Packs children into lines along the start axis (rows if horizontal, columns if vertical).
    /// A child that doesn't fit in the remaining slots of a line moves to the next one.
    /// </summary>
    internal sealed class GridLines
    {
        private readonly List<GridCell> m_Cells = new List<GridCell>();
        private readonly List<int> m_LineUnits = new List<int>();

        public int CellCount => m_Cells.Count;

        public int LineCount => m_LineUnits.Count;

        /// <summary>Slots used by the widest line.</summary>
        public int BlockUnits { get; private set; }

        public GridCell GetCell(int index) => m_Cells[index];

        /// <summary>Slots used by <paramref name="line"/>.</summary>
        public int GetLineUnits(int line) => m_LineUnits[line];

        /// <summary>O(n), allocation free. Spans are clamped to <paramref name="slotsPerLine"/>.</summary>
        public void Pack(GridChildMetrics metrics, int slotsPerLine)
        {
            m_Cells.Clear();
            m_LineUnits.Clear();
            BlockUnits = 0;

            int count = metrics.Count;
            if (count == 0)
                return;

            int slot = 0;
            for (int i = 0; i < count; i++)
            {
                int span = Mathf.Min(metrics.GetSpan(i), slotsPerLine);

                if (slot + span > slotsPerLine)
                    CloseLine(ref slot);

                m_Cells.Add(new GridCell { Slot = slot, Line = m_LineUnits.Count, Span = span });
                slot += span;
            }

            CloseLine(ref slot);
        }

        private void CloseLine(ref int usedSlots)
        {
            m_LineUnits.Add(usedSlots);
            if (usedSlots > BlockUnits)
                BlockUnits = usedSlots;
            usedSlots = 0;
        }
    }
}
