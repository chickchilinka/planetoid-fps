using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Utils.Extensions
{
    public static class UIExtentions
    {
        public static T RaycastUI<T>(Vector3 position) where T : class
        {
            var pointerData = new PointerEventData(EventSystem.current);
            pointerData.position = position;
            var results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointerData, results);

            return results.Select(res => res.gameObject.GetComponent<T>())
                .FirstOrDefault(container => container != null);
        }

        public static T RaycastWorld<T>(Vector3 position) where T : class
        {
            var worldPoint = Camera.main.ScreenToWorldPoint(position);

            var hit = Physics2D.Raycast(worldPoint, Vector2.zero);

            if (hit.collider != null)
                return hit.collider.GetComponent<T>();

            return null;
        }

        public static Rect GetSafeArea()
        {
#if UNITY_EDITOR
            Rect rect;

            if (Screen.width == 1125 && Screen.height == 2436)
            {
                rect = new Rect(0f, 102f / 2436f, 1f, 2202f / 2436f);
                return new Rect(Screen.width * rect.x, Screen.height * rect.y, Screen.width * rect.width,
                    Screen.height * rect.height);
            }

            if (Screen.width == 2436 && Screen.height == 1125)
            {
                rect = new Rect(132f / 2436f, 63f / 1125f, 2172f / 2436f, 1062f / 1125f);
                return new Rect(Screen.width * rect.x, Screen.height * rect.y, Screen.width * rect.width,
                    Screen.height * rect.height);
            }
#endif
            return Screen.safeArea;
        }

        public static Color GetHueColor(Color color, float newHue)
        {
            float hue, saturation, value;
            Color.RGBToHSV(color, out hue, out saturation, out value);

            hue = newHue;
            var newColor = Color.HSVToRGB(hue, saturation, value);

            return new Color(newColor.r, newColor.g, newColor.b, color.a);
        }

        public static Texture2D DecompressTexture(Texture2D texture)
        {
            var atlasColors = texture.GetPixels32();
            var decompressedTexture = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false);
            decompressedTexture.SetPixels32(atlasColors);
            return decompressedTexture;
        }
        public static Sprite GetSpriteFromDecompressedAtlas(Sprite sourceSprite, Texture2D decompressedAtlasTexture)
        {
            var colors = decompressedAtlasTexture.GetPixels((int)sourceSprite.textureRect.x, (int)sourceSprite.textureRect.y,
                (int)sourceSprite.textureRect.width, (int)sourceSprite.textureRect.height);

            var newTexture = new Texture2D((int)sourceSprite.textureRect.width, (int)sourceSprite.textureRect.height, TextureFormat.RGBA32, false);
            
            newTexture.filterMode = FilterMode.Point;
            newTexture.wrapMode = TextureWrapMode.Clamp;

            newTexture.SetPixels(colors);
            newTexture.Apply();

            return Sprite.Create(newTexture, new Rect(0, 0, newTexture.width, newTexture.height),
                Vector2.one / 2f);
        }
    }
}