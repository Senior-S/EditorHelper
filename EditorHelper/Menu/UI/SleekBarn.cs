using EditorHelper.CustomAssets;
using UnityEngine;
using SDG.Unturned;

namespace Breakdown.Module.Editor.UI
{
    public class SleekBarn : SleekWrapper
    {
        public delegate void ClickedMenuItem(SleekBarn item);
        public event ClickedMenuItem onClickedItem;

        private ISleekButton button;
        private ISleekImage icon;
        private ISleekLabel nameLabel;
        private ISleekLabel infoLabel;

        public SleekBarn(SceneAsset sceneAsset)
        {
            base.SizeOffset_X = 400f;
            base.SizeOffset_Y = 100f;

            button = Glazier.Get().CreateButton();
            button.SizeOffset_X = 0f;
            button.SizeOffset_Y = 0f;
            button.SizeScale_X = 1f;
            button.SizeScale_Y = 1f;
            button.OnClicked += OnClickedButton;
            AddChild(button);

            icon = Glazier.Get().CreateImage();
            icon.PositionOffset_X = 10f;
            icon.PositionOffset_Y = 10f;
            icon.SizeOffset_X = 380f;
            icon.SizeOffset_Y = 80f;
            button.AddChild(icon);

            nameLabel = Glazier.Get().CreateLabel();
            nameLabel.PositionOffset_Y = 10f;
            nameLabel.SizeScale_X = 1f;
            nameLabel.SizeOffset_Y = 50f;
            nameLabel.Text = sceneAsset.BarnName;
            nameLabel.TextAlignment = TextAnchor.MiddleCenter;
            nameLabel.FontSize = ESleekFontSize.Medium;
            nameLabel.TextContrastContext = ETextContrastContext.ColorfulBackdrop;
            button.AddChild(nameLabel);

            infoLabel = Glazier.Get().CreateLabel();
            infoLabel.PositionOffset_X = 100;
            infoLabel.PositionOffset_Y = 50;
            infoLabel.SizeScale_X = 1f;
            infoLabel.SizeOffset_Y = 30;
            infoLabel.Text = "";
            infoLabel.TextColor = ESleekTint.FONT;
            button.AddChild(infoLabel);
        }

        public void SetIconTexture(Texture2D iconTexture)
        {
            if (iconTexture != null)
                icon.Texture = iconTexture;
        }

        private void OnClickedButton(ISleekElement button)
        {
            onClickedItem?.Invoke(this);
        }
    }
}
