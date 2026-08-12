using System;
using System.Collections.Generic;
using System.Text;

namespace FileCreator.Services;

public sealed class DataGridViewCheckBoxHeaderCell : DataGridViewColumnHeaderCell
{
    public bool Checked { get; private set; }

    public event EventHandler? CheckedChanged;

    public DataGridViewCheckBoxHeaderCell()
    {
        ValueType = typeof(bool);
    }

    protected override void Paint(
        Graphics graphics,
        Rectangle clipBounds,
        Rectangle cellBounds,
        int rowIndex,
        DataGridViewElementStates dataGridViewCellState,
        object? value,
        object? formattedValue,
        string? errorText,
        DataGridViewCellStyle cellStyle,
        DataGridViewAdvancedBorderStyle advancedBorderStyle,
        DataGridViewPaintParts paintParts)
    {
        base.Paint(
            graphics,
            clipBounds,
            cellBounds,
            rowIndex,
            dataGridViewCellState,
            value,
            formattedValue,
            errorText,
            cellStyle,
            advancedBorderStyle,
            paintParts);

        var checkBoxSize = 15;

        var x = cellBounds.X +
                (cellBounds.Width - checkBoxSize) / 2;

        var y = cellBounds.Y +
                (cellBounds.Height - checkBoxSize) / 2;

        var checkBoxState = Checked
            ? ButtonState.Checked
            : ButtonState.Normal;

        ControlPaint.DrawCheckBox(
            graphics,
            x,
            y,
            checkBoxSize,
            checkBoxSize,
            checkBoxState);
    }

    protected override void OnMouseClick(DataGridViewCellMouseEventArgs e)
    {
        base.OnMouseClick(e);

        Checked = !Checked;

        CheckedChanged?.Invoke(this, EventArgs.Empty);

        DataGridView?.InvalidateCell(this);
    }

    public void SetChecked(bool value)
    {
        if (Checked == value)
            return;

        Checked = value;

        CheckedChanged?.Invoke(this, EventArgs.Empty);

        DataGridView?.InvalidateCell(this);
    }
}