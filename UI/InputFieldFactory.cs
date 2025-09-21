using System;
using UnityEngine;
using UnityEngine.UI;

namespace ExpandedAiFramework.UI
{
    [System.Serializable]
    public struct InputFieldOptions
    {
        public int characterLimit;
        public InputField.ContentType contentType;
        public TextFieldOptions textFieldOptions;
        public ImageOptions backgroundImageOptions;

        public static InputFieldOptions Default()
        {
            return new InputFieldOptions
            {
                characterLimit = 0,
                contentType = InputField.ContentType.Standard,
                textFieldOptions = TextFieldOptions.Default(TextOptions.Default("", 14)),
                backgroundImageOptions = new ImageOptions { Color = Color.white }
            };
        }
    }

    public static class InputFieldFactory
    {            
        public static InputField CreateInputField(Transform parent, InputFieldOptions options, Action<string> onValueChanged = null)
        {
            var inputObj = new GameObject("InputField");
            inputObj.transform.SetParent(parent, false);
            LayoutFactory.CreateLayoutElement(inputObj.transform, options.textFieldOptions.layoutElement);
            Image backgroundImage = ImageFactory.CreateImage(inputObj.transform, options.backgroundImageOptions);
            backgroundImage.type = Image.Type.Sliced; 
            var inputField = inputObj.AddComponent<InputField>();
            inputField.contentType = options.contentType;
            inputField.characterLimit = options.characterLimit;
            inputField.targetGraphic = backgroundImage;
            inputField.textComponent = TextFactory.CreateTextField(inputObj.transform, options.textFieldOptions);;

            if (onValueChanged != null)
            {
                inputField.onValueChanged.AddListener(onValueChanged);
            }
            
            return inputField;
        }
    }
}