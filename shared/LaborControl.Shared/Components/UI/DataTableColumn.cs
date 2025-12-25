using Microsoft.AspNetCore.Components;

namespace VitrineFr.Client.Components.UI;

/// <summary>
/// Définition d'une colonne pour le composant DataTable
/// </summary>
public class DataTableColumn
{
    /// <summary>
    /// Titre de la colonne
    /// </summary>
    public string Title { get; set; } = "";

    /// <summary>
    /// Nom de la propriété à afficher (si pas de template)
    /// </summary>
    public string Property { get; set; } = "";

    /// <summary>
    /// Template personnalisé pour afficher la cellule
    /// </summary>
    public RenderFragment<object>? Template { get; set; }
}
