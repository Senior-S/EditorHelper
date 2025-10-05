using EditorHelper2.Assets;
using EditorHelper2.UI.Builders;
using UnityEngine;
using SDG.Unturned;

namespace EditorHelper2.UI.Elements
{
    public class SleekBarn : SleekWrapper
    {
        public delegate void ClickedMenuItem(SleekBarn item);
        public event ClickedMenuItem OnClickedItem;

        private readonly ISleekButton _button;
        private readonly ISleekImage _icon;

        public SleekBarn(BarnAsset sceneAsset)
        {
            base.SizeOffset_X = 400f;
            base.SizeOffset_Y = 100f;

            UIBuilder builder = new(0f, 0f);
            builder.ResetProperties()
                        .SetSizeHorizontal(0f)
                        .SetSizeVertical(0f)
                        .SetScaleHorizontal(1f)
                        .SetScaleVertical(1f);
            _button = builder.BuildButton();
            _button.OnClicked += OnClickedButton;
            AddChild(_button);
            
            builder.ResetProperties()
                .SetOffsetHorizontal(10f)
                .SetOffsetVertical(10f)
                .SetSizeHorizontal(380f)
                .SetSizeVertical(80f);
            _icon = builder.BuildImage();
            _button.AddChild(_icon);

            builder.ResetProperties()
                .SetOffsetVertical(10f)
                .SetScaleHorizontal(1f)
                .SetSizeVertical(50f)
                .SetText(sceneAsset.BarnName);
            ISleekLabel nameLabel = builder.BuildLabel(fontSize: ESleekFontSize.Medium);
            nameLabel.TextContrastContext = ETextContrastContext.ColorfulBackdrop;
            _button.AddChild(nameLabel);

            builder.ResetProperties()
                .SetOffsetHorizontal(100)
                .SetOffsetVertical(50)
                .SetScaleHorizontal(1f)
                .SetSizeVertical(30)
                .SetText("");
            ISleekLabel infoLabel = builder.BuildLabel(fontSize: ESleekFontSize.Medium);
            _button.AddChild(infoLabel);
        }

        public void SetIconTexture(Texture2D iconTexture)
        {
            if (iconTexture != null)
                _icon.Texture = iconTexture;
        }

        private void OnClickedButton(ISleekElement btn)
        {
            OnClickedItem?.Invoke(this);
        }
    }
}
