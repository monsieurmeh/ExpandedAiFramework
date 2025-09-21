using UnityEngine;
using UnityEngine.UI;


namespace ExpandedAiFramework.UI
{   
    [System.Serializable]
    public struct TextFieldOptions
    {
        public TextOptions textOptions;
        public LayoutElementOptions layoutElement;

        public static TextFieldOptions Default(TextOptions textOptions)
        {
            return new TextFieldOptions
            {
                textOptions = textOptions,
                layoutElement = LayoutElementOptions.MinSize(0, textOptions.fontSize + 4, 1, 0)
            };
        }

        public static TextFieldOptions Title(TextOptions textOptions)   
        {
            return new TextFieldOptions
            {
                textOptions = textOptions,
                layoutElement = LayoutElementOptions.MinSize(0, textOptions.fontSize + 8, 1, 0)
            };
        }

        public static TextFieldOptions Label(TextOptions textOptions, float labelWidth = 100f)
        {
            return new TextFieldOptions
            {
                textOptions = textOptions,
                layoutElement = LayoutElementOptions.Fixed(labelWidth, textOptions.fontSize + 4)
            };
        }
    }



    [System.Serializable]
    public struct TextOptions
    {
        public string text;
        public int fontSize;
        public Color color;
        public TextAnchor alignment;
        public FontStyle fontStyle;

        public static TextOptions Default(string text, int fontSize = 14)
        {
            return new TextOptions
            {
                text = text,
                fontSize = fontSize,
                color = Color.white,
                alignment = TextAnchor.MiddleLeft,
                fontStyle = FontStyle.Normal,
            };
        }

        public static TextOptions Title(string text, int fontSize = 18)
        {
            return new TextOptions
            {
                text = text,
                fontSize = fontSize,
                color = Color.white,
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
            };
        }

        public static TextOptions Label(string text, int fontSize = 12)
        {
            return new TextOptions
            {
                text = text,
                fontSize = fontSize,
                color = new Color(0.9f, 0.9f, 0.9f, 1f),
                alignment = TextAnchor.MiddleLeft,
                fontStyle = FontStyle.Normal,
            };
        }
    }

    public static class TextFactory
    {            

        // Creates a dedicated gameobject with text field and layout element
        public static Text CreateTextField(Transform parent, TextFieldOptions options)
        {
            var textObj = new GameObject("TextField");
            textObj.transform.SetParent(parent, false);
            LayoutFactory.CreateLayoutElement(textObj.transform, options.layoutElement);
            return CreateText(textObj.transform, options.textOptions);
        }

        // Creates JUST the text component
        public static Text CreateText(Transform parent, TextOptions options)
        {
            var textComponent = parent.gameObject.AddComponent<Text>();
            SetText(textComponent, options);
            return textComponent;
        }

        public static void SetText(Text textComponent, TextOptions options)
        {
            textComponent.text = options.text;
            textComponent.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            textComponent.fontSize = options.fontSize;
            textComponent.color = options.color;
            textComponent.alignment = options.alignment;
            textComponent.fontStyle = options.fontStyle;
        }
    }
}