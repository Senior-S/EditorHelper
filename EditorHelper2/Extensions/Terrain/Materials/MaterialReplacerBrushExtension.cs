using System;
using DanielWillett.UITools.API.Extensions;
using EditorHelper2.common.API.Attributes;
using EditorHelper2.common.API.Interfaces;
using SDG.Unturned;

namespace EditorHelper2.Extensions.Terrain.Materials;

[UIExtension(typeof(EditorLevelObjectsUI))]
[EHExtension("Material Replacer Brush Extension", "JienSultan")]
public class MaterialReplacerBrushExtension : UIExtension, IExtension
{
    public MaterialReplacerBrushExtension()
    {

    }

    public void Initialize()
    {
        throw new NotImplementedException();
    }

    #region Event handlers

    #endregion Event handlers

    #region Extension Functions

    #endregion Extension Functions

    public void Dispose()
    {

    }
}