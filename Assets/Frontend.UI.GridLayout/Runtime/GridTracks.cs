using System.Collections.Generic;
using UnityEngine;

namespace Frontend.UI
{
    /// <summary>
    /// Sizes and offsets of the grid tracks: slots run along the start axis, lines across it.
    /// Values are in pixels, relative to the grid block (before padding, alignment and mirroring).
    /// </summary>
    internal sealed class GridTracks
    {
        private readonly List<float> m_SlotSizes = new List<float>();
        private readonly List<float> m_SlotOffsets = new List<float>();
        private readonly List<float> m_LineSizes = new List<float>();
        private readonly List<float> m_LineOffsets = new List<float>();

        /// <summary>Size of the block along the start axis (slots + spacing).</summary>
        public float MainExtent { get; private set; }

        /// <summary>Size of the block across the start axis (lines + spacing).</summary>
        public float CrossExtent { get; private set; }

        public float GetSlotOffset(int slot) => m_SlotOffsets[slot];

        public float GetLineSize(int line) => m_LineSizes[line];

        public float GetLineOffset(int line) => m_LineOffsets[line];

        public float GetSlotSizeOrZero(int slot) => slot >= 0 && slot < m_SlotSizes.Count ? m_SlotSizes[slot] : 0f;

        public float GetLineSizeOrZero(int line) => line >= 0 && line < m_LineSizes.Count ? m_LineSizes[line] : 0f;

        /// <summary>Distance from the start of <paramref name="firstSlot"/> to the end of the last spanned slot.</summary>
        public float GetSpanExtent(int firstSlot, int span)
        {
            int last = firstSlot + span - 1;
            return m_SlotOffsets[last] + m_SlotSizes[last] - m_SlotOffsets[firstSlot];
        }

        /// <summary>Every slot and line gets the same size (Preserve / Stretch).</summary>
        public void SetUniform(GridLines lines, Vector2 cellSize, int mainAxis, Vector2 spacing)
        {
            Fill(m_SlotSizes, lines.BlockUnits, cellSize[mainAxis]);
            Fill(m_LineSizes, lines.LineCount, cellSize[1 - mainAxis]);
            BuildAllOffsets(mainAxis, spacing);
        }

        /// <summary>
        /// FitContent: each line is as big as its largest cell, each slot as big as its largest single-slot cell.
        /// Spanning cells that still don't fit spread the missing space evenly over their slots.
        /// </summary>
        public void SetFromContent(GridLines lines, GridChildMetrics metrics, int mainAxis, Vector2 spacing)
        {
            int crossAxis = 1 - mainAxis;
            Fill(m_SlotSizes, lines.BlockUnits, 0f);
            Fill(m_LineSizes, lines.LineCount, 0f);

            for (int i = 0; i < lines.CellCount; i++)
            {
                GridCell cell = lines.GetCell(i);
                Vector2 preferred = metrics.GetPreferredSize(i);

                if (preferred[crossAxis] > m_LineSizes[cell.Line])
                    m_LineSizes[cell.Line] = preferred[crossAxis];

                if (cell.Span == 1 && preferred[mainAxis] > m_SlotSizes[cell.Slot])
                    m_SlotSizes[cell.Slot] = preferred[mainAxis];
            }

            if (metrics.HasSpans)
                GrowSlotsForSpans(lines, metrics, mainAxis, spacing[mainAxis]);

            BuildAllOffsets(mainAxis, spacing);
        }

        private void GrowSlotsForSpans(GridLines lines, GridChildMetrics metrics, int mainAxis, float gap)
        {
            for (int i = 0; i < lines.CellCount; i++)
            {
                GridCell cell = lines.GetCell(i);
                if (cell.Span < 2)
                    continue;

                float missing = metrics.GetPreferredSize(i)[mainAxis] - SumSizes(m_SlotSizes, cell.Slot, cell.Span, gap);
                if (missing <= 0f)
                    continue;

                float perSlot = missing / cell.Span;
                for (int s = cell.Slot; s < cell.Slot + cell.Span; s++)
                    m_SlotSizes[s] += perSlot;
            }
        }

        private void BuildAllOffsets(int mainAxis, Vector2 spacing)
        {
            MainExtent = BuildOffsets(m_SlotSizes, m_SlotOffsets, spacing[mainAxis]);
            CrossExtent = BuildOffsets(m_LineSizes, m_LineOffsets, spacing[1 - mainAxis]);
        }

        private static void Fill(List<float> sizes, int count, float value)
        {
            sizes.Clear();
            for (int i = 0; i < count; i++)
                sizes.Add(value);
        }

        private static float SumSizes(List<float> sizes, int start, int count, float gap)
        {
            float total = gap * (count - 1);
            for (int i = start; i < start + count; i++)
                total += sizes[i];
            return total;
        }

        /// <summary>Fills prefix offsets and returns the total extent (sizes + gaps).</summary>
        private static float BuildOffsets(List<float> sizes, List<float> offsets, float gap)
        {
            offsets.Clear();
            float position = 0f;
            for (int i = 0; i < sizes.Count; i++)
            {
                offsets.Add(position);
                position += sizes[i] + gap;
            }

            return sizes.Count > 0 ? position - gap : 0f;
        }
    }
}
