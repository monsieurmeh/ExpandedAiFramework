using System.Reflection;
using System.Reflection.Emit;
using System;
using System.Linq;
using System.Collections.Generic;

namespace ExpandedAiFramework
{
    public class LogSettings : BaseNestedSettings
    {
        private static Type _dynamicLogSettingsType;
        private Dictionary<LogCategory, bool> _dynamicFields;

        public LogSettings(string path) : base(path) 
        {
            // If this is the base LogSettings being constructed, create and return the dynamic version instead
            if (this.GetType() == typeof(LogSettings))
            {
                // We can't change the constructor return, but we can make this instance behave like the dynamic one
                InitializeDynamicFields();
            }
        }

        // Protected constructor for dynamic subclass
        protected LogSettings(string path, bool isDynamic) : base(path) 
        {
            if (isDynamic)
            {
                InitializeDynamicFields();
            }
        }

        private void InitializeDynamicFields()
        {
            _dynamicFields = new Dictionary<LogCategory, bool>();
            
            // Initialize all log categories (except None, General, and COUNT) to false
            var logCategories = Enum.GetValues(typeof(LogCategory))
                .Cast<LogCategory>()
                .Where(category => category != LogCategory.None && 
                                 category != LogCategory.General && 
                                 category != LogCategory.COUNT);

            foreach (LogCategory category in logCategories)
            {
                _dynamicFields[category] = false;
            }
        }

        // Factory method that creates the proper dynamic type
        public static LogSettings CreateDynamic(string path)
        {
            if (_dynamicLogSettingsType == null)
            {
                CreateDynamicLogSettingsType();
            }
            
            return (LogSettings)Activator.CreateInstance(_dynamicLogSettingsType, path);
        }

        private static void CreateDynamicLogSettingsType()
        {
            // Create a dynamic assembly and module
            AssemblyName assemblyName = new AssemblyName("DynamicLogSettings");
            AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule("DynamicLogSettingsModule");

            // Create the type that inherits from LogSettings
            TypeBuilder typeBuilder = moduleBuilder.DefineType("DynamicLogSettings", 
                TypeAttributes.Public | TypeAttributes.Class, typeof(LogSettings));

            // Add constructor that calls base constructor with isDynamic = true
            ConstructorBuilder constructorBuilder = typeBuilder.DefineConstructor(
                MethodAttributes.Public, CallingConventions.Standard, new Type[] { typeof(string) });
            
            ILGenerator constructorIL = constructorBuilder.GetILGenerator();
            constructorIL.Emit(OpCodes.Ldarg_0); // Load 'this'
            constructorIL.Emit(OpCodes.Ldarg_1); // Load 'path' parameter
            constructorIL.Emit(OpCodes.Ldc_I4_1); // Load 'true' for isDynamic
            constructorIL.Emit(OpCodes.Call, typeof(LogSettings).GetConstructor(
                BindingFlags.NonPublic | BindingFlags.Instance, null, new Type[] { typeof(string), typeof(bool) }, null));
            constructorIL.Emit(OpCodes.Ret);

            // First, recreate the Enable field to ensure it appears first in reflection order
            FieldBuilder enableField = typeBuilder.DefineField("Enable", typeof(bool), FieldAttributes.Public);
            
            // Add all the attributes from the base Enable field
            var sectionAttr = typeof(SectionAttribute).GetConstructor(new Type[] { typeof(string) });
            var sectionBuilder = new CustomAttributeBuilder(sectionAttr, new object[] { "Advanced Logging" });
            enableField.SetCustomAttribute(sectionBuilder);

            var nameAttr = typeof(NameAttribute).GetConstructor(new Type[] { typeof(string) });
            var nameBuilder = new CustomAttributeBuilder(nameAttr, new object[] { "Enable Advanced Logging" });
            enableField.SetCustomAttribute(nameBuilder);

            var descAttr = typeof(DescriptionAttribute).GetConstructor(new Type[] { typeof(string) });
            var descBuilder = new CustomAttributeBuilder(descAttr, new object[] { "Enable advenced logging features. This should only be used for troubleshooting and debugging." });
            enableField.SetCustomAttribute(descBuilder);

            // Get all LogCategory enum values except None and General and COUNT
            var logCategories = Enum.GetValues(typeof(LogCategory))
                .Cast<LogCategory>()
                .Where(category => category != LogCategory.None && 
                                 category != LogCategory.General && 
                                 category != LogCategory.COUNT)
                .OrderBy(category => category.ToString()) // Sort for consistent ordering
                .ToArray();

            var fieldBuilders = new Dictionary<LogCategory, FieldBuilder>();

            // Create a field for each log category - these will appear after Enable in reflection order
            foreach (LogCategory category in logCategories)
            {
                string fieldName = $"Enable{category}Logging";
                FieldBuilder field = typeBuilder.DefineField(fieldName, typeof(bool), FieldAttributes.Public);
                fieldBuilders[category] = field;
                
                // Add Name attribute - no Section attribute needed, they inherit from Enable field's section
                var fieldNameBuilder = new CustomAttributeBuilder(nameAttr, new object[] { category.ToString() });
                field.SetCustomAttribute(fieldNameBuilder);
            }

            // Create the type first so we can get FieldInfos
            Type createdType = typeBuilder.CreateType();
            _dynamicLogSettingsType = createdType;
        }

        public virtual LogCategoryFlags GetFlags()
        {
            LogCategoryFlags flags = LogCategoryFlags.General;
            
            if (_dynamicFields != null)
            {
                // Use dynamic fields if available
                foreach (var kvp in _dynamicFields)
                {
                    if (kvp.Value)
                    {
                        LogCategoryFlags categoryFlag = (LogCategoryFlags)(1 << (int)kvp.Key);
                        flags |= categoryFlag;
                    }
                }
            }
            else if (this.GetType() != typeof(LogSettings))
            {
                // Use reflection to get fields from the dynamic type
                var fields = this.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);
                
                foreach (var field in fields)
                {
                    if (field.Name.StartsWith("Enable") && field.Name.EndsWith("Logging") && field.FieldType == typeof(bool))
                    {
                        string categoryName = field.Name.Substring(6, field.Name.Length - 13); // Remove "Enable" and "Logging"
                        if (Enum.TryParse<LogCategory>(categoryName, out LogCategory category))
                        {
                            bool isEnabled = (bool)field.GetValue(this);
                            if (isEnabled)
                            {
                                LogCategoryFlags categoryFlag = (LogCategoryFlags)(1 << (int)category);
                                flags |= categoryFlag;
                            }
                        }
                    }
                }
            }
            
            return flags;
        }

        protected override void OnChange(FieldInfo field, object oldValue, object newValue)
        {
            base.OnChange(field, oldValue, newValue);
            
            // Update dynamic fields dictionary if this is a logging field
            if (_dynamicFields != null && field.Name.StartsWith("Enable") && field.Name.EndsWith("Logging"))
            {
                string categoryName = field.Name.Substring(6, field.Name.Length - 13); // Remove "Enable" and "Logging"
                if (Enum.TryParse<LogCategory>(categoryName, out LogCategory category))
                {
                    _dynamicFields[category] = (bool)newValue;
                }
            }
            
            EAFManager.Instance.LogCategoryFlags = GetFlags();
        }
    }
}