using System.Security.Cryptography;
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
        public Color Color;
    }

    public static class ImageFactory
    {         
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
            if (options.ResourcePath != null)
            {
                image.sprite = Resources.Load<Sprite>(options.ResourcePath);
            }
            image.color = options.Color;
        }

    }
}