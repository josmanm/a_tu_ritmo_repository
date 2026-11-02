using UnityEngine;

/// <summary>
/// Recursos visuales compartidos por todos los menus del juego.
/// Toma como referencia el estilo del menu principal (botones tipo pildora brillante,
/// panel crema redondeado e iconos blancos). La instancia vive en Resources/MenuTheme
/// para que cualquier escena pueda usarla en tiempo de ejecucion a traves de MenuStyle.
/// </summary>
[CreateAssetMenu(fileName = "MenuTheme", menuName = "A Tu Ritmo/Menu Theme")]
public class MenuTheme : ScriptableObject
{
    [Header("Botones")]
    [Tooltip("Pildora blanca brillante. Se tine con el color de cada boton (assets menu buttons_0).")]
    public Sprite buttonSprite;

    [Header("Paneles")]
    [Tooltip("Panel crema redondeado del menu de perfiles (assets de menu de perfiles_0).")]
    public Sprite panelSprite;

    [Header("Iconos")]
    public Sprite playIcon;
    public Sprite settingsIcon;
    public Sprite exitIcon;
}
