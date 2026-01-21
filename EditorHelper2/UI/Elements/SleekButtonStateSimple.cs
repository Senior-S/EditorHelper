using EditorHelper2.UI.Builders;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace EditorHelper2.UI.Elements
{
    public class SleekButtonStateSimple : SleekWrapper
    {
        public delegate void SwappedStateSimple(SleekButtonStateSimple button, int index);
        public SwappedStateSimple? onSwappedState;
        protected ISleekButton button;

        public GUIContent[]? states { get; private set; }

        public int state
        {
            get => _state;
            set
            {
                _state = value;
                synchronizeActiveContent();
            }
        }
        private int _state;

        public string tooltip
        {
            get => button.TooltipText;
            set => button.TooltipText = value;
        }

        public bool isInteractable
        {
            get => button.IsClickable;
            set => button.IsClickable = value;
        }

        public bool UseContentTooltip
        {
            get;
            set
            {
                field = value;

                if (UseContentTooltip)
                {
                    if (states != null && state >= 0 && state < states.Length && states[state] != null)
                    {
                        tooltip = states[state].tooltip;
                    }
                    else
                    {
                        tooltip = string.Empty;
                    }
                }
            }
        }

        private void synchronizeActiveContent()
        {
            if (states != null && state >= 0 && state < states.Length && states[state] != null)
            {
                button.Text = states[state].text;
                if (UseContentTooltip) tooltip = states[state].tooltip;
            }
            else
            {
                button.Text = string.Empty;
                if (UseContentTooltip) tooltip = string.Empty;
            }
        }

        protected virtual void onClickedState(ISleekElement button)
        {
            _state++;
            if (state >= states.Length)
                _state = 0;

            synchronizeActiveContent();
            onSwappedState?.Invoke(this, state);
        }

        protected virtual void onRightClickedState(ISleekElement button)
        {
            _state--;
            if (state < 0)
                _state = states.Length - 1;

            synchronizeActiveContent();
            onSwappedState?.Invoke(this, state);
        }

        public void setContent(params GUIContent[] newStates)
        {
            states = newStates;
            if (state >= states.Length)
                _state = 0;

            synchronizeActiveContent();
        }

        public SleekButtonStateSimple(params GUIContent[] newStates)
            : this(0, newStates)
        {
        }

        public SleekButtonStateSimple(int iconSize, params GUIContent[] newStates)
        {
            _state = 0;
            button = new UIBuilder(0f, 0f).BuildButton();
            button.SizeScale_X = 1f;
            button.SizeScale_Y = 1f;
            AddChild(button);
            if (newStates != null) setContent(newStates);

            button.OnClicked += onClickedState;
            button.OnRightClicked += onRightClickedState;
        }
    }
}
