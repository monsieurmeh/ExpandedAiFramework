using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using static ExpandedAiFramework.DebugMenu.Extensions;
using ExpandedAiFramework.UI;

namespace ExpandedAiFramework.DebugMenu
{
    /// <summary>
    /// UI options for text form fields
    /// </summary>
    [System.Serializable]
    public struct TextFormFieldOptions
    {
        public PanelOptions ContainerOptions;
        public TextFieldOptions LabelOptions;
        public InputFieldOptions InputOptions;
        public bool IsReadOnly;
        
        public static TextFormFieldOptions Default(string fieldName)
        {
            var containerOptions = PanelOptions.Default(PanelLayoutType.Horizontal);
            containerOptions.Name = $"TextField_{fieldName}";
            containerOptions.LayoutElementOptions = LayoutElementOptions.PreferredSize(-1, 35, 1, 0);
            containerOptions.LayoutGroupOptions = LayoutGroupOptions.Horizontal(15, new RectOffset(10, 10, 10, 10)); 
            containerOptions.LayoutGroupOptions.childControlWidth = true; 
            containerOptions.LayoutGroupOptions.childControlHeight = true; 
            containerOptions.LayoutGroupOptions.childForceExpandWidth = false; 
            containerOptions.LayoutGroupOptions.childForceExpandHeight = false; 
            containerOptions.HasBackground = true;
            containerOptions.ImageOptions = new ImageOptions { Color = new Color(0.08f, 0.1f, 0.12f, 0.3f) };
            
            var labelTextOptions = TextOptions.Default($"{fieldName}:", 12);
            labelTextOptions.color = new Color(0.85f, 0.9f, 0.95f, 1f);
            labelTextOptions.alignment = TextAnchor.MiddleLeft;
            labelTextOptions.fontStyle = FontStyle.Bold;
            var labelFieldOptions = TextFieldOptions.Label(labelTextOptions, 160);
            labelFieldOptions.layoutElement = LayoutElementOptions.PreferredSize(160, 30, 0, 0);
            
            var inputOptions = InputFieldOptions.Default();
            var textOptions = TextOptions.Default("", 11);
            textOptions.color = new Color(0.95f, 0.95f, 0.98f, 1f);
            inputOptions.textFieldOptions = TextFieldOptions.Default(textOptions);
            inputOptions.textFieldOptions.layoutElement = LayoutElementOptions.Flexible(1, 0);
            inputOptions.backgroundImageOptions = new ImageOptions { Color = new Color(0.18f, 0.2f, 0.25f, 1f) };
            
            return new TextFormFieldOptions
            {
                ContainerOptions = containerOptions,
                LabelOptions = labelFieldOptions,
                InputOptions = inputOptions,
                IsReadOnly = false
            };
        }
        
        public static TextFormFieldOptions Compact(string fieldName, float height = 20f)
        {
            var options = Default(fieldName);
            options.ContainerOptions.LayoutElementOptions = LayoutElementOptions.PreferredSize(-1, height, 1, 0);
            options.ContainerOptions.LayoutGroupOptions = LayoutGroupOptions.Horizontal(10, new RectOffset(5, 5, 5, 5));
            options.LabelOptions.layoutElement = LayoutElementOptions.PreferredSize(120, height - 10, 0, 0);
            return options;
        }
        
        public static TextFormFieldOptions Wide(string fieldName, float height = 20f)
        {
            var options = Default(fieldName);
            options.ContainerOptions.LayoutElementOptions = LayoutElementOptions.PreferredSize(-1, height, 1, 0);
            options.LabelOptions.layoutElement = LayoutElementOptions.PreferredSize(200, 50, 0, 0);
            return options;
        }
    }
    
    /// <summary>
    /// UI options for dropdown form fields
    /// </summary>
    [System.Serializable]
    public struct DropdownFormFieldOptions
    {
        public PanelOptions ContainerOptions;
        public TextFieldOptions LabelOptions;
        public DropdownOptions DropdownOptions;
        public bool IsReadOnly;
        
        public static DropdownFormFieldOptions Default(string fieldName)
        {
            var containerOptions = PanelOptions.Default(PanelLayoutType.Horizontal);
            containerOptions.Name = $"DropdownField_{fieldName}";
            containerOptions.LayoutElementOptions = LayoutElementOptions.Fixed(0, 45);
            containerOptions.LayoutGroupOptions = LayoutGroupOptions.Horizontal(10, new RectOffset(5, 5, 5, 5));
            containerOptions.LayoutGroupOptions.childControlWidth = false;
            containerOptions.LayoutGroupOptions.childControlHeight = true;
            containerOptions.LayoutGroupOptions.childForceExpandWidth = false;
            containerOptions.LayoutGroupOptions.childForceExpandHeight = false;
            containerOptions.HasBackground = false;
            
            var labelTextOptions = TextOptions.Default($"{fieldName}:", 11);
            labelTextOptions.color = new Color(0.7f, 0.7f, 0.7f, 1f);
            labelTextOptions.alignment = TextAnchor.MiddleLeft;
            labelTextOptions.fontStyle = FontStyle.Bold;
            var labelFieldOptions = TextFieldOptions.Label(labelTextOptions, 150);
            labelFieldOptions.layoutElement = LayoutElementOptions.Fixed(150, 40);
            
            var dropdownOptions = DropdownOptions.Default();
            dropdownOptions.layoutElement = LayoutElementOptions.MinSize(400, 40, 1, 0);
            dropdownOptions.backgroundImageOptions = new ImageOptions { Color = new Color(0.3f, 0.3f, 0.3f, 1f) };
            
            return new DropdownFormFieldOptions
            {
                ContainerOptions = containerOptions,
                LabelOptions = labelFieldOptions,
                DropdownOptions = dropdownOptions,
                IsReadOnly = false
            };
        }
        
        public static DropdownFormFieldOptions Compact(string fieldName)
        {
            var options = Default(fieldName);
            options.ContainerOptions.LayoutElementOptions = LayoutElementOptions.Fixed(0, 45);
            options.LabelOptions.layoutElement = LayoutElementOptions.Fixed(120, 40);
            options.DropdownOptions.layoutElement = LayoutElementOptions.MinSize(300, 40, 1, 0);
            return options;
        }
        
        public static DropdownFormFieldOptions Wide(string fieldName)
        {
            var options = Default(fieldName);
            options.LabelOptions.layoutElement = LayoutElementOptions.Fixed(200, 50);
            options.DropdownOptions.layoutElement = LayoutElementOptions.MinSize(500, 50, 1, 0);
            return options;
        }
    }
    
    /// <summary>
    /// UI options for toggle form fields
    /// </summary>
    [System.Serializable]
    public struct ToggleFormFieldOptions
    {
        public PanelOptions ContainerOptions;
        public TextFieldOptions LabelOptions;
        public ToggleOptions ToggleOptions;
        public bool IsReadOnly;
        
        public static ToggleFormFieldOptions Default(string fieldName)
        {
            var containerOptions = PanelOptions.Default(PanelLayoutType.Horizontal);
            containerOptions.Name = $"ToggleField_{fieldName}";
            containerOptions.LayoutElementOptions = LayoutElementOptions.Fixed(0, 45);
            containerOptions.LayoutGroupOptions = LayoutGroupOptions.Horizontal(10, new RectOffset(5, 5, 5, 5));
            containerOptions.LayoutGroupOptions.childControlWidth = false;
            containerOptions.LayoutGroupOptions.childControlHeight = true;
            containerOptions.LayoutGroupOptions.childForceExpandWidth = false;
            containerOptions.LayoutGroupOptions.childForceExpandHeight = false;
            containerOptions.HasBackground = false;
            
            var labelTextOptions = TextOptions.Default($"{fieldName}:", 11);
            labelTextOptions.color = new Color(0.7f, 0.7f, 0.7f, 1f);
            labelTextOptions.alignment = TextAnchor.MiddleLeft;
            labelTextOptions.fontStyle = FontStyle.Bold;
            var labelFieldOptions = TextFieldOptions.Label(labelTextOptions, 150);
            labelFieldOptions.layoutElement = LayoutElementOptions.Fixed(150, 40);
            
            var toggleOptions = ToggleOptions.CheckboxOnly(false, 50f);
            toggleOptions.backgroundOptions = new ImageOptions { Color = new Color(0.3f, 0.3f, 0.3f, 1f) };
            toggleOptions.checkmarkOptions = new ImageOptions { Color = new Color(0.2f, 0.8f, 0.2f, 1f) };
            
            return new ToggleFormFieldOptions
            {
                ContainerOptions = containerOptions,
                LabelOptions = labelFieldOptions,
                ToggleOptions = toggleOptions,
                IsReadOnly = false
            };
        }
        
        public static ToggleFormFieldOptions Compact(string fieldName)
        {
            var options = Default(fieldName);
            options.ContainerOptions.LayoutElementOptions = LayoutElementOptions.Fixed(0, 45);
            options.LabelOptions.layoutElement = LayoutElementOptions.Fixed(120, 40);
            options.ToggleOptions = ToggleOptions.CheckboxOnly(false, 40f);
            return options;
        }
    }
    
    /// <summary>
    /// UI options for Vector3 form fields
    /// </summary>
    [System.Serializable]
    public struct Vector3FormFieldOptions
    {
        public PanelOptions ContainerOptions;
        public TextFieldOptions LabelOptions;
        public InputFieldOptions ComponentInputOptions;
        public bool IsReadOnly;
        
        public static Vector3FormFieldOptions Default(string fieldName)
        {
            var containerOptions = PanelOptions.Default(PanelLayoutType.Horizontal);
            containerOptions.Name = $"Vector3Field_{fieldName}";
            containerOptions.LayoutElementOptions = LayoutElementOptions.PreferredSize(-1, 55, 1, 0);
            containerOptions.LayoutGroupOptions = LayoutGroupOptions.Horizontal(15, new RectOffset(10, 10, 10, 10));
            containerOptions.LayoutGroupOptions.childControlWidth = true;
            containerOptions.LayoutGroupOptions.childControlHeight = true;
            containerOptions.LayoutGroupOptions.childForceExpandWidth = false;
            containerOptions.LayoutGroupOptions.childForceExpandHeight = false;
            containerOptions.HasBackground = true;
            containerOptions.ImageOptions = new ImageOptions { Color = new Color(0.08f, 0.1f, 0.12f, 0.3f) };
            
            var labelTextOptions = TextOptions.Default($"{fieldName}:", 12);
            labelTextOptions.color = new Color(0.85f, 0.9f, 0.95f, 1f);
            labelTextOptions.alignment = TextAnchor.MiddleLeft;
            labelTextOptions.fontStyle = FontStyle.Bold;
            var labelFieldOptions = TextFieldOptions.Label(labelTextOptions, 160);
            labelFieldOptions.layoutElement = LayoutElementOptions.PreferredSize(160, 40, 0, 0);
            
            var inputOptions = InputFieldOptions.Default();
            inputOptions.contentType = InputField.ContentType.DecimalNumber;
            var textOptions = TextOptions.Default("", 10);
            textOptions.alignment = TextAnchor.MiddleCenter;
            textOptions.color = new Color(0.95f, 0.95f, 0.98f, 1f);
            inputOptions.textFieldOptions = TextFieldOptions.Default(textOptions);
            inputOptions.textFieldOptions.layoutElement = LayoutElementOptions.PreferredSize(-1, 32, 1, 0);
            inputOptions.backgroundImageOptions = new ImageOptions { Color = new Color(0.18f, 0.2f, 0.25f, 1f) };
            
            return new Vector3FormFieldOptions
            {
                ContainerOptions = containerOptions,
                LabelOptions = labelFieldOptions,
                ComponentInputOptions = inputOptions,
                IsReadOnly = false
            };
        }
        
        public static Vector3FormFieldOptions Compact(string fieldName)
        {
            var options = Default(fieldName);
            options.ContainerOptions.LayoutElementOptions = LayoutElementOptions.PreferredSize(-1, 50, 1, 0);
            options.ContainerOptions.LayoutGroupOptions = LayoutGroupOptions.Horizontal(10, new RectOffset(5, 5, 5, 5));
            options.LabelOptions.layoutElement = LayoutElementOptions.PreferredSize(120, 40, 0, 0);
            return options;
        }
        
        public static Vector3FormFieldOptions Wide(string fieldName)
        {
            var options = Default(fieldName);
            options.ContainerOptions.LayoutElementOptions = LayoutElementOptions.PreferredSize(-1, 80, 1, 0);
            options.LabelOptions.layoutElement = LayoutElementOptions.PreferredSize(200, 60, 0, 0);
            return options;
        }
    }
    public static class FormFieldFactory
    {
        // New method with UI options
        public static TextFormField CreateTextField(string fieldName, string currentValue, Transform parent, System.Action<string, object> onValueChanged, TextFormFieldOptions? uiOptions = null, bool isReadOnly = false)
        {
            var options = uiOptions ?? TextFormFieldOptions.Default(fieldName);
            options.IsReadOnly = isReadOnly;
            
            var fieldContainer = PanelFactory.CreatePanel(parent, options.ContainerOptions);
            
            // Update label text with field name
            var labelOptions = options.LabelOptions;
            labelOptions.textOptions.text = $"{fieldName}:";
            var label = TextFactory.CreateTextField(fieldContainer.transform, labelOptions);
            label.name = $"Label_{fieldName}";
            
            // Update input options with current value
            var inputOptions = options.InputOptions;
            inputOptions.textFieldOptions.textOptions.text = currentValue ?? "";
            
            var inputField = InputFieldFactory.CreateInputField(fieldContainer.transform, inputOptions, 
                onValueChanged != null ? (value) => onValueChanged(fieldName, value) : null);
            inputField.name = $"Input_{fieldName}";
            inputField.text = currentValue ?? "";
            
            var textField = new TextFormField(fieldName, inputField);
            textField.SetReadOnlyState(isReadOnly);
            return textField;
        }
        
        // Backwards compatibility method
        public static TextFormField CreateTextField(string fieldName, string currentValue, Transform parent, System.Action<string, object> onValueChanged, bool isReadOnly)
        {
            return CreateTextField(fieldName, currentValue, parent, onValueChanged, null, isReadOnly);
        }

        // New method with UI options
        public static DropdownFormField CreateDropdownField<T>(string fieldName, T currentValue, Transform parent, System.Action<string, object> onValueChanged, DropdownFormFieldOptions? uiOptions = null, bool isReadOnly = false) where T : Enum
        {
            var options = uiOptions ?? DropdownFormFieldOptions.Default(fieldName);
            options.IsReadOnly = isReadOnly;
            
            var fieldContainer = PanelFactory.CreatePanel(parent, options.ContainerOptions);
            
            // Update label text with field name
            var labelOptions = options.LabelOptions;
            labelOptions.textOptions.text = $"{fieldName}:";
            var label = TextFactory.CreateTextField(fieldContainer.transform, labelOptions);
            label.name = $"Label_{fieldName}";
            
            // Get enum values and create string options
            var enumValues = Enum.GetValues(typeof(T)).Cast<T>().ToArray();
            var stringOptions = enumValues.Select(v => v.ToString()).ToList();
            var currentIndex = Array.IndexOf(enumValues, currentValue);
            
            var dropdown = DropdownFactory.CreateDropdown(fieldContainer.transform, stringOptions, currentIndex,
                onValueChanged != null ? (index) =>
                {
                    if (index >= 0 && index < enumValues.Length)
                    {
                        onValueChanged(fieldName, enumValues[index]);
                    }
                } : null,
                options.DropdownOptions);
            dropdown.name = $"Dropdown_{fieldName}";
            
            var formField = new DropdownFormField(fieldName, dropdown, enumValues);
            formField.SetReadOnlyState(isReadOnly);
            return formField;
        }
        
        // Backwards compatibility method
        public static DropdownFormField CreateDropdownField<T>(string fieldName, T currentValue, Transform parent, System.Action<string, object> onValueChanged, bool isReadOnly) where T : Enum
        {
            return CreateDropdownField(fieldName, currentValue, parent, onValueChanged, null, isReadOnly);
        }

        // New method with UI options
        public static ToggleFormField CreateToggleField(string fieldName, bool currentValue, Transform parent, System.Action<string, object> onValueChanged, ToggleFormFieldOptions? uiOptions = null, bool isReadOnly = false)
        {
            var options = uiOptions ?? ToggleFormFieldOptions.Default(fieldName);
            options.IsReadOnly = isReadOnly;
            
            var fieldContainer = PanelFactory.CreatePanel(parent, options.ContainerOptions);
            
            // Update label text with field name
            var labelOptions = options.LabelOptions;
            labelOptions.textOptions.text = $"{fieldName}:";
            var label = TextFactory.CreateTextField(fieldContainer.transform, labelOptions);
            label.name = $"Label_{fieldName}";
            
            // Update toggle options with current value
            var toggleOptions = options.ToggleOptions;
            toggleOptions.isOn = currentValue;
            
            var toggle = ToggleFactory.CreateToggle(fieldContainer.transform, toggleOptions,
                onValueChanged != null ? (value) => onValueChanged(fieldName, value) : null);
            toggle.name = $"Toggle_{fieldName}";
            
            var formField = new ToggleFormField(fieldName, toggle);
            formField.SetReadOnlyState(isReadOnly);
            return formField;
        }
        
        // Backwards compatibility method
        public static ToggleFormField CreateToggleField(string fieldName, bool currentValue, Transform parent, System.Action<string, object> onValueChanged, bool isReadOnly)
        {
            return CreateToggleField(fieldName, currentValue, parent, onValueChanged, null, isReadOnly);
        }

        // New method with UI options
        public static Vector3FormField CreateVector3Field(string fieldName, Vector3 currentValue, Transform parent, System.Action<string, object> onValueChanged, Vector3FormFieldOptions? uiOptions = null, bool isReadOnly = false)
        {
            var options = uiOptions ?? Vector3FormFieldOptions.Default(fieldName);
            options.IsReadOnly = isReadOnly;
            
            var fieldContainer = PanelFactory.CreatePanel(parent, options.ContainerOptions);
            
            // Update label text with field name
            var labelOptions = options.LabelOptions;
            labelOptions.textOptions.text = $"{fieldName}:";
            var label = TextFactory.CreateTextField(fieldContainer.transform, labelOptions);
            label.name = $"Label_{fieldName}";
            
            var xField = CreateFloatInput("X", currentValue.x, fieldContainer.transform, options.ComponentInputOptions);
            var yField = CreateFloatInput("Y", currentValue.y, fieldContainer.transform, options.ComponentInputOptions);
            var zField = CreateFloatInput("Z", currentValue.z, fieldContainer.transform, options.ComponentInputOptions);
            
            var formField = new Vector3FormField(fieldName, xField, yField, zField);
            
            if (onValueChanged != null)
            {
                System.Action notifyChange = () => onValueChanged(fieldName, formField.GetValue());
                
                xField.onValueChanged.AddListener(new Action<string>((_) => notifyChange()));
                yField.onValueChanged.AddListener(new Action<string>((_) => notifyChange()));
                zField.onValueChanged.AddListener(new Action<string>((_) => notifyChange()));
            }
            
            formField.SetReadOnlyState(isReadOnly);
            return formField;
        }
        
        // Backwards compatibility method
        public static Vector3FormField CreateVector3Field(string fieldName, Vector3 currentValue, Transform parent, System.Action<string, object> onValueChanged, bool isReadOnly)
        {
            return CreateVector3Field(fieldName, currentValue, parent, onValueChanged, null, isReadOnly);
        }


        private static InputField CreateFloatInput(string label, float value, Transform parent, InputFieldOptions? baseOptions = null)
        {
            var containerOptions = PanelOptions.Default(PanelLayoutType.Vertical);
            containerOptions.Name = $"FloatInput_{label}";
            containerOptions.LayoutElementOptions = LayoutElementOptions.Flexible(1, 0);
            containerOptions.LayoutGroupOptions = LayoutGroupOptions.Vertical(2, new RectOffset(5, 5, 0, 0));
            containerOptions.LayoutGroupOptions.childControlWidth = true;
            containerOptions.LayoutGroupOptions.childControlHeight = true;
            containerOptions.HasBackground = false;
            
            var inputContainer = PanelFactory.CreatePanel(parent, containerOptions);
            
            var labelTextOptions = TextOptions.Default(label, 10);
            labelTextOptions.color = new Color(0.85f, 0.9f, 0.95f, 1f);
            labelTextOptions.alignment = TextAnchor.MiddleCenter;
            labelTextOptions.fontStyle = FontStyle.Bold;
            
            var labelFieldOptions = TextFieldOptions.Default(labelTextOptions);
            labelFieldOptions.layoutElement = LayoutElementOptions.PreferredSize(-1, 18, 1, 0);
            
            var labelText = TextFactory.CreateTextField(inputContainer.transform, labelFieldOptions);
            labelText.name = $"Label_{label}";
            
            // Use provided options or default
            var inputOptions = baseOptions ?? InputFieldOptions.Default();
            inputOptions.contentType = InputField.ContentType.DecimalNumber;
            
            // Update text with current value
            inputOptions.textFieldOptions.textOptions.text = value.ToString("F2");
            
            var inputField = InputFieldFactory.CreateInputField(inputContainer.transform, inputOptions);
            inputField.name = $"Input_{label}";
            inputField.text = value.ToString("F2");
            
            return inputField;
        }
    }

    // Form field implementations
    public class TextFormField : IFormField
    {
        public string FieldName { get; }
        public bool IsReadOnly { get; set; }
        private InputField mInputField;

        public TextFormField(string fieldName, InputField inputField)
        {
            FieldName = fieldName;
            mInputField = inputField;
            IsReadOnly = false;
        }

        public object GetValue()
        {
            return mInputField.text;
        }

        public void SetValue(object value)
        {
            mInputField.text = value?.ToString() ?? "";
        }

        public void SetReadOnlyState(bool readOnly)
        {
            IsReadOnly = readOnly;
            mInputField.readOnly = readOnly;
            
            var inputImage = mInputField.GetComponent<Image>();
            if (inputImage != null)
            {
                if (readOnly)
                {
                    inputImage.color = new Color(0.15f, 0.15f, 0.15f, 1f);
                }
                else
                {
                    inputImage.color = new Color(0.3f, 0.3f, 0.3f, 1f);
                }
            }
        }

        public InputField GetInputField()
        {
            return mInputField;
        }
    }

    public class DropdownFormField : IFormField
    {
        public string FieldName { get; }
        public bool IsReadOnly { get; set; }
        private Dropdown mDropdown;
        private System.Array mEnumValues;

        public DropdownFormField(string fieldName, Dropdown dropdown, System.Array enumValues)
        {
            FieldName = fieldName;
            mDropdown = dropdown;
            mEnumValues = enumValues;
            IsReadOnly = false;
        }

        public object GetValue()
        {
            var index = mDropdown.value;
            return (index >= 0 && index < mEnumValues.Length) ? mEnumValues.GetValue(index) : null;
        }

        public void SetValue(object value)
        {
            if (value != null)
            {
                for (int i = 0; i < mEnumValues.Length; i++)
                {
                    if (mEnumValues.GetValue(i).Equals(value))
                    {
                        mDropdown.value = i;
                        break;
                    }
                }
            }
        }

        public void SetReadOnlyState(bool readOnly)
        {
            IsReadOnly = readOnly;
            mDropdown.interactable = !readOnly;
            
            var dropdownImage = mDropdown.GetComponent<Image>();
            if (dropdownImage != null)
            {
                if (readOnly)
                {
                    dropdownImage.color = new Color(0.15f, 0.15f, 0.15f, 1f);
                }
                else
                {
                    dropdownImage.color = new Color(0.25f, 0.25f, 0.25f, 1f);
                }
            }
        }
    }

    public class ToggleFormField : IFormField
    {
        public string FieldName { get; }
        public bool IsReadOnly { get; set; }
        private Toggle mToggle;

        public ToggleFormField(string fieldName, Toggle toggle)
        {
            FieldName = fieldName;
            mToggle = toggle;
            IsReadOnly = false;
        }

        public object GetValue()
        {
            return mToggle.isOn;
        }

        public void SetValue(object value)
        {
            if (value is bool boolValue)
            {
                mToggle.isOn = boolValue;
            }
        }

        public void SetReadOnlyState(bool readOnly)
        {
            IsReadOnly = readOnly;
            mToggle.interactable = !readOnly;
            
            var toggleImage = mToggle.GetComponent<Image>();
            if (toggleImage != null)
            {
                if (readOnly)
                {
                    toggleImage.color = new Color(0.15f, 0.15f, 0.15f, 1f);
                }
                else
                {
                    toggleImage.color = new Color(0.3f, 0.3f, 0.3f, 1f);
                }
            }
        }
    }

    public class Vector3FormField : IFormField
    {
        public string FieldName { get; }
        public bool IsReadOnly { get; set; }
        private InputField mXField;
        private InputField mYField;
        private InputField mZField;

        public Vector3FormField(string fieldName, InputField xField, InputField yField, InputField zField)
        {
            FieldName = fieldName;
            mXField = xField;
            mYField = yField;
            mZField = zField;
            IsReadOnly = false;
        }

        public object GetValue()
        {
            float.TryParse(mXField.text, out float x);
            float.TryParse(mYField.text, out float y);
            float.TryParse(mZField.text, out float z);
            return new Vector3(x, y, z);
        }

        public void SetValue(object value)
        {
            if (value is Vector3 vector3)
            {
                mXField.text = vector3.x.ToString("F2");
                mYField.text = vector3.y.ToString("F2");
                mZField.text = vector3.z.ToString("F2");
            }
        }

        public void SetReadOnlyState(bool readOnly)
        {
            IsReadOnly = readOnly;
            
            mXField.readOnly = readOnly;
            mYField.readOnly = readOnly;
            mZField.readOnly = readOnly;
            
            var readOnlyColor = new Color(0.15f, 0.15f, 0.15f, 1f);
            var normalColor = new Color(0.3f, 0.3f, 0.3f, 1f);
            
            var xImage = mXField.GetComponent<Image>();
            if (xImage != null)
                xImage.color = readOnly ? readOnlyColor : normalColor;
                
            var yImage = mYField.GetComponent<Image>();
            if (yImage != null)
                yImage.color = readOnly ? readOnlyColor : normalColor;
                
            var zImage = mZField.GetComponent<Image>();
            if (zImage != null)
                zImage.color = readOnly ? readOnlyColor : normalColor;
        }
    }
    
    /*
    USAGE EXAMPLES:
    
    // Basic usage (backwards compatible - uses default styling)
    var textField = FormFieldFactory.CreateTextField("Name", "John Doe", parent, OnValueChanged);
    
    // Custom compact text field
    var compactOptions = TextFormFieldOptions.Compact("Name", 40f);
    var compactField = FormFieldFactory.CreateTextField("Name", "John Doe", parent, OnValueChanged, compactOptions);
    
    // Custom wide text field
    var wideOptions = TextFormFieldOptions.Wide("Description", 90f);
    var wideField = FormFieldFactory.CreateTextField("Description", "", parent, OnValueChanged, wideOptions);
    
    // Fully custom text field
    var customOptions = TextFormFieldOptions.Default("CustomField");
    customOptions.ContainerOptions.LayoutElementOptions = LayoutElementOptions.PreferredSize(-1, 100, 1, 0);
    customOptions.ContainerOptions.ImageOptions = new ImageOptions { Color = new Color(0.2f, 0.3f, 0.4f, 0.5f) };
    customOptions.LabelOptions.textOptions.fontSize = 14;
    customOptions.LabelOptions.textOptions.color = Color.yellow;
    customOptions.InputOptions.backgroundImageOptions = new ImageOptions { Color = new Color(0.1f, 0.1f, 0.2f, 1f) };
    var customField = FormFieldFactory.CreateTextField("CustomField", "", parent, OnValueChanged, customOptions);
    
    // Compact dropdown
    var compactDropdown = DropdownFormFieldOptions.Compact("Mode");
    var dropdownField = FormFieldFactory.CreateDropdownField("Mode", MyEnum.Value1, parent, OnValueChanged, compactDropdown);
    
    // Compact toggle
    var compactToggle = ToggleFormFieldOptions.Compact("Enabled");
    var toggleField = FormFieldFactory.CreateToggleField("Enabled", true, parent, OnValueChanged, compactToggle);
    
    // Compact Vector3
    var compactVector = Vector3FormFieldOptions.Compact("Position");
    var vectorField = FormFieldFactory.CreateVector3Field("Position", Vector3.zero, parent, OnValueChanged, compactVector);
    */
}
