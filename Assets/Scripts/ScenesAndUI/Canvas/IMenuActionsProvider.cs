using UnityEngine;
using UnityEngine.UI;

public interface IMenuActionsProvider
{
    // Botones �por color� (modo cl�sico)
    Button BlueButton { get; }
    Button OrangeButton { get; }
    Button GreenButton { get; }
    Button YellowButton { get; }
    Button BackButton { get; }

    // Nuevo: modo lista
    bool UseListNavigation { get; }
    Button[] NavButtons { get; }                  // orden a recorrer (elige t� en el Inspector)
    Color NormalColor { get; }
    Color SelectedColor { get; }

    void SetHighlight(Button selected);           // pinta uno como seleccionado (rojizo) y el resto en blanco
    void ClearHighlight();                        // resetea todos al color normal
}
