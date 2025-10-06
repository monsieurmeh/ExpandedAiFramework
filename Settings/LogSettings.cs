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
        public LogSettings(string path) : base(path) { }

        public static LogSettings CreateDynamic(string path)
        {
            if (_dynamicLogSettingsType == null)
            {
                CreateDynamicLogSettingsType();
            }
            
            var instance = (LogSettings)Activator.CreateInstance(_dynamicLogSettingsType, path);
            return instance;
        }

        private static void CreateDynamicLogSettingsType()
        {
            AssemblyName assemblyName = new AssemblyName("DynamicLogSettings");
            AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule("DynamicLogSettingsModule");
            TypeBuilder typeBuilder = moduleBuilder.DefineType("DynamicLogSettings", TypeAttributes.Public | TypeAttributes.Class, typeof(LogSettings));
            ConstructorBuilder constructorBuilder = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, new Type[] { typeof(string) });
            ILGenerator constructorIL = constructorBuilder.GetILGenerator();
            constructorIL.Emit(OpCodes.Ldarg_0); // Load 'this'
            constructorIL.Emit(OpCodes.Ldarg_1); // Load 'path' parameter
            constructorIL.Emit(OpCodes.Call, typeof(LogSettings).GetConstructor(new Type[] { typeof(string) }));
            constructorIL.Emit(OpCodes.Ret);
            FieldBuilder enableField = typeBuilder.DefineField("Enable", typeof(bool), FieldAttributes.Public);
            var sectionAttr = typeof(SectionAttribute).GetConstructor(new Type[] { typeof(string) });
            var sectionBuilder = new CustomAttributeBuilder(sectionAttr, new object[] { "Advanced Logging" });
            enableField.SetCustomAttribute(sectionBuilder);
            var nameAttr = typeof(NameAttribute).GetConstructor(new Type[] { typeof(string) });
            var nameBuilder = new CustomAttributeBuilder(nameAttr, new object[] { "Enable Advanced Logging" });
            enableField.SetCustomAttribute(nameBuilder);
            var descAttr = typeof(DescriptionAttribute).GetConstructor(new Type[] { typeof(string) });
            var descBuilder = new CustomAttributeBuilder(descAttr, new object[] { "Enable advanced logging features. This should only be used for troubleshooting and debugging." });
            enableField.SetCustomAttribute(descBuilder);
            var logCategories = Enum.GetValues(typeof(LogCategory))
                .Cast<LogCategory>()
                .Where(category => category != LogCategory.None && 
                                 category != LogCategory.General && 
                                 category != LogCategory.COUNT)
                .OrderBy(category => category.ToString())
                .ToArray();
            var fieldBuilders = new Dictionary<LogCategory, FieldBuilder>();
            foreach (LogCategory category in logCategories)
            {
                string fieldName = $"Enable{category}Logging";
                FieldBuilder field = typeBuilder.DefineField(fieldName, typeof(bool), FieldAttributes.Public);
                fieldBuilders[category] = field;
                var fieldNameBuilder = new CustomAttributeBuilder(nameAttr, new object[] { category.ToString() });
                field.SetCustomAttribute(fieldNameBuilder);
            }
            Type createdType = typeBuilder.CreateType();
            _dynamicLogSettingsType = createdType;
        }

        public virtual LogCategoryFlags GetFlags()
        {
            LogCategoryFlags flags = LogCategoryFlags.General;
            var fields = this.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);
            foreach (var field in fields)
            {
                if (!field.Name.StartsWith("Enable")) continue;
                if (!field.Name.EndsWith("Logging")) continue;
                if (field.FieldType != typeof(bool)) continue;
                string categoryName = field.Name.Substring(6, field.Name.Length - 13);
                if (!(bool)field.GetValue(this)) continue;
                if (!Enum.TryParse<LogCategory>(categoryName, out LogCategory category)) continue;
                LogCategoryFlags categoryFlag = (LogCategoryFlags)(1 << (int)category);
                flags |= categoryFlag;
            }
            
            return flags;
        }

        protected override void OnChange(FieldInfo field, object oldValue, object newValue)
        {
            base.OnChange(field, oldValue, newValue);
            if (!field.Name.StartsWith("Enable")) return;
            if (!field.Name.EndsWith("Logging")) return;
            if (field.FieldType != typeof(bool)) return;
            string categoryName = field.Name.Substring(6, field.Name.Length - 13);
            if (!Enum.TryParse<LogCategory>(categoryName, out LogCategory category)) return;
            var newFlags = GetFlags();
            EAFManager.Instance.LogCategoryFlags = newFlags;
        }
    }
}