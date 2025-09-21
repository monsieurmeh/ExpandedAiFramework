using System.Security.Cryptography;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace ExpandedAiFramework.UI
{

    [System.Serializable]
    public struct IconOptions
    {
        public ImageOptions ImageOptions;
        public LayoutElementOptions LayoutElementOptions;
    }

    [System.Serializable]
    public struct ImageOptions
    {
        public string ResourcePath;
        public string AssetBundlePath;
        public string AtlasName;         // Optional: specific atlas to search in
        public string SpriteName;
        public Color Color;
    }

    public static class ImageFactory
    {
        // Cache for loaded AssetBundles and sprites
        private static Dictionary<string, AssetBundle> loadedAssetBundles = new Dictionary<string, AssetBundle>();
        private static Dictionary<string, Sprite> cachedSprites = new Dictionary<string, Sprite>();
        
        public static Image CreateIcon(Transform parent, IconOptions options)
        {
            var imageObj = new GameObject("Icon");
            imageObj.transform.SetParent(parent, false);
            Image image = CreateImage(imageObj.transform, options.ImageOptions);
            LayoutFactory.CreateLayoutElement(imageObj.transform, options.LayoutElementOptions);
            return image;
        }

        public static Image CreateImage(Transform parent, ImageOptions options)
        {
            var image = parent.gameObject.AddComponent<Image>();
            SetImage(image, options);
            return image;
        }

        public static void SetImage(Image image, ImageOptions options)
        {
            Sprite sprite = null;
            
            // Try AssetBundle first
            if (!string.IsNullOrEmpty(options.AssetBundlePath) && !string.IsNullOrEmpty(options.SpriteName))
            {
                // If specific atlas is specified, use it
                if (!string.IsNullOrEmpty(options.AtlasName))
                {
                    sprite = LoadSpriteFromAtlas(options.AssetBundlePath, options.AtlasName, options.SpriteName);
                }
                else
                {
                    // Try general sprite loading (includes atlas search)
                    sprite = LoadSpriteFromAssetBundle(options.AssetBundlePath, options.SpriteName);
                }
            }
            // Fallback to Resources
            else if (!string.IsNullOrEmpty(options.ResourcePath))
            {
                sprite = Resources.Load<Sprite>(options.ResourcePath);
            }
            
            if (sprite != null)
            {
                image.sprite = sprite;
            }
            
            image.color = options.Color;
        }

        /// <summary>
        /// Loads a sprite from an AssetBundle with caching
        /// </summary>
        public static Sprite LoadSpriteFromAssetBundle(string assetBundlePath, string spriteName)
        {
            var cacheKey = $"{assetBundlePath}:{spriteName}";
            
            // Check sprite cache first
            if (cachedSprites.TryGetValue(cacheKey, out Sprite cachedSprite) && cachedSprite != null)
            {
                return cachedSprite;
            }
            
            // Load or get cached AssetBundle
            AssetBundle assetBundle = GetOrLoadAssetBundle(assetBundlePath);
            if (assetBundle == null)
            {
                MelonLoader.MelonLogger.Warning($"Failed to load AssetBundle: {assetBundlePath}");
                return null;
            }
            
            // Load sprite from AssetBundle
            Sprite sprite = assetBundle.LoadAsset<Sprite>($"{spriteName}.png");
            if (sprite == null)
            {
                // Try loading from sprite atlas
                var atlases = assetBundle.LoadAllAssets<UnityEngine.U2D.SpriteAtlas>();
                foreach (var atlas in atlases)
                {
                    sprite = atlas.GetSprite(spriteName);
                    if (sprite != null) break;
                }
            }
            
            if (sprite != null)
            {
                cachedSprites[cacheKey] = sprite;
                MelonLoader.MelonLogger.Msg($"Loaded sprite '{spriteName}' from AssetBundle '{assetBundlePath}'");
            }
            else
            {
                MelonLoader.MelonLogger.Warning($"Sprite '{spriteName}' not found in AssetBundle '{assetBundlePath}'");
            }
            
            return sprite;
        }

        /// <summary>
        /// Gets or loads an AssetBundle from the file system
        /// </summary>
        private static AssetBundle GetOrLoadAssetBundle(string assetBundlePath)
        {
            // Check cache first
            if (loadedAssetBundles.TryGetValue(assetBundlePath, out AssetBundle cachedBundle) && cachedBundle != null)
            {
                return cachedBundle;
            }
            
            // Resolve full path
            string fullPath = ResolveAssetBundlePath(assetBundlePath);
            if (!File.Exists(fullPath))
            {
                MelonLoader.MelonLogger.Error($"AssetBundle file not found: {fullPath}");
                return null;
            }
            
            try
            {
                AssetBundle assetBundle = AssetBundle.LoadFromFile(fullPath);
                if (assetBundle != null)
                {
                    loadedAssetBundles[assetBundlePath] = assetBundle;
                    MelonLoader.MelonLogger.Msg($"Loaded AssetBundle: {fullPath}");
                    return assetBundle;
                }
                else
                {
                    MelonLoader.MelonLogger.Error($"Failed to load AssetBundle from: {fullPath}");
                }
            }
            catch (System.Exception ex)
            {
                MelonLoader.MelonLogger.Error($"Exception loading AssetBundle '{fullPath}': {ex.Message}");
            }
            
            return null;
        }

        /// <summary>
        /// Resolves AssetBundle path - supports relative paths from mod folder
        /// </summary>
        private static string ResolveAssetBundlePath(string assetBundlePath)
        {
            // If already absolute path, use as-is
            if (Path.IsPathRooted(assetBundlePath))
            {
                return assetBundlePath;
            }
            
            // Relative to MelonLoader Mods folder
            string modsFolder = Path.Combine(MelonLoader.MelonUtils.GameDirectory, "Mods");
            return Path.Combine(modsFolder, assetBundlePath);
        }

        /// <summary>
        /// Preloads an AssetBundle for faster sprite access
        /// </summary>
        public static bool PreloadAssetBundle(string assetBundlePath)
        {
            return GetOrLoadAssetBundle(assetBundlePath) != null;
        }

        /// <summary>
        /// Lists all sprites available in an AssetBundle (including atlas sprites)
        /// </summary>
        public static string[] GetAvailableSprites(string assetBundlePath)
        {
            AssetBundle assetBundle = GetOrLoadAssetBundle(assetBundlePath);
            if (assetBundle == null) return new string[0];
            
            var spriteNames = new List<string>();
            
            // Get direct sprites
            var sprites = assetBundle.LoadAllAssets<Sprite>();
            foreach (var sprite in sprites)
            {
                spriteNames.Add(sprite.name);
            }
            
            // Get sprites from atlases
            var atlases = assetBundle.LoadAllAssets<UnityEngine.U2D.SpriteAtlas>();
            foreach (var atlas in atlases)
            {
                var atlasSprites = GetSpriteNamesFromAtlas(atlas);
                spriteNames.AddRange(atlasSprites);
            }
            
            return spriteNames.ToArray();
        }

        /// <summary>
        /// Gets all sprite names from a SpriteAtlas
        /// </summary>
        private static List<string> GetSpriteNamesFromAtlas(UnityEngine.U2D.SpriteAtlas atlas)
        {
            var spriteNames = new List<string>();
            
            try
            {
                // Try to get sprite count (this may not work in all Unity versions)
                int spriteCount = atlas.spriteCount;
                
                // If we can get the count, try to enumerate by common naming patterns
                if (spriteCount > 0)
                {
                    // Try common sprite naming patterns
                    var commonPatterns = new List<string>();
                    
                    // Add atlas name as base pattern
                    commonPatterns.Add(atlas.name);
                    
                    // Try numbered patterns
                    for (int i = 0; i < spriteCount * 2; i++) // Try more than the count in case of gaps
                    {
                        commonPatterns.Add($"{atlas.name}_{i}");
                        commonPatterns.Add($"{atlas.name}_{i:00}");
                        commonPatterns.Add($"{atlas.name}_{i:000}");
                        commonPatterns.Add($"sprite_{i}");
                        commonPatterns.Add($"icon_{i}");
                    }
                    
                    // Test each pattern
                    foreach (var pattern in commonPatterns)
                    {
                        var sprite = atlas.GetSprite(pattern);
                        if (sprite != null && !spriteNames.Contains(pattern))
                        {
                            spriteNames.Add(pattern);
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                MelonLoader.MelonLogger.Warning($"Could not enumerate sprites from atlas '{atlas.name}': {ex.Message}");
            }
            
            return spriteNames;
        }

        /// <summary>
        /// Gets all available sprite atlases in an AssetBundle
        /// </summary>
        public static UnityEngine.U2D.SpriteAtlas[] GetSpriteAtlases(string assetBundlePath)
        {
            AssetBundle assetBundle = GetOrLoadAssetBundle(assetBundlePath);
            if (assetBundle == null) return new UnityEngine.U2D.SpriteAtlas[0];
            
            return assetBundle.LoadAllAssets<UnityEngine.U2D.SpriteAtlas>();
        }

        /// <summary>
        /// Loads a sprite from a specific atlas within an AssetBundle
        /// </summary>
        public static Sprite LoadSpriteFromAtlas(string assetBundlePath, string atlasName, string spriteName)
        {
            var cacheKey = $"{assetBundlePath}:{atlasName}:{spriteName}";
            
            // Check sprite cache first
            if (cachedSprites.TryGetValue(cacheKey, out Sprite cachedSprite) && cachedSprite != null)
            {
                return cachedSprite;
            }
            
            AssetBundle assetBundle = GetOrLoadAssetBundle(assetBundlePath);
            if (assetBundle == null) return null;
            
            // Load specific atlas
            UnityEngine.U2D.SpriteAtlas atlas = assetBundle.LoadAsset<UnityEngine.U2D.SpriteAtlas>(atlasName);
            if (atlas == null)
            {
                MelonLoader.MelonLogger.Warning($"Atlas '{atlasName}' not found in AssetBundle '{assetBundlePath}'");
                return null;
            }
            
            // Get sprite from atlas
            Sprite sprite = atlas.GetSprite(spriteName);
            if (sprite != null)
            {
                cachedSprites[cacheKey] = sprite;
                MelonLoader.MelonLogger.Msg($"Loaded sprite '{spriteName}' from atlas '{atlasName}' in AssetBundle '{assetBundlePath}'");
            }
            else
            {
                MelonLoader.MelonLogger.Warning($"Sprite '{spriteName}' not found in atlas '{atlasName}'");
            }
            
            return sprite;
        }

        /// <summary>
        /// Tries multiple methods to find a sprite in atlases
        /// </summary>
        public static Sprite FindSpriteInAtlases(string assetBundlePath, string spriteName)
        {
            AssetBundle assetBundle = GetOrLoadAssetBundle(assetBundlePath);
            if (assetBundle == null) return null;
            
            var atlases = assetBundle.LoadAllAssets<UnityEngine.U2D.SpriteAtlas>();
            
            foreach (var atlas in atlases)
            {
                var sprite = atlas.GetSprite(spriteName);
                if (sprite != null)
                {
                    var cacheKey = $"{assetBundlePath}:{atlas.name}:{spriteName}";
                    cachedSprites[cacheKey] = sprite;
                    return sprite;
                }
            }
            
            return null;
        }

        /// <summary>
        /// Unloads an AssetBundle and clears its cached sprites
        /// </summary>
        public static void UnloadAssetBundle(string assetBundlePath, bool unloadAllLoadedObjects = false)
        {
            if (loadedAssetBundles.TryGetValue(assetBundlePath, out AssetBundle assetBundle))
            {
                // Clear cached sprites from this bundle
                var keysToRemove = new List<string>();
                foreach (var key in cachedSprites.Keys)
                {
                    if (key.StartsWith(assetBundlePath + ":"))
                    {
                        keysToRemove.Add(key);
                    }
                }
                foreach (var key in keysToRemove)
                {
                    cachedSprites.Remove(key);
                }
                
                // Unload the bundle
                assetBundle.Unload(unloadAllLoadedObjects);
                loadedAssetBundles.Remove(assetBundlePath);
                
                MelonLoader.MelonLogger.Msg($"Unloaded AssetBundle: {assetBundlePath}");
            }
        }

        /// <summary>
        /// Clears all cached AssetBundles and sprites
        /// </summary>
        public static void ClearCache()
        {
            foreach (var bundle in loadedAssetBundles.Values)
            {
                if (bundle != null)
                {
                    bundle.Unload(false);
                }
            }
            
            loadedAssetBundles.Clear();
            cachedSprites.Clear();
            
            MelonLoader.MelonLogger.Msg("Cleared ImageFactory cache");
        }
    }
}