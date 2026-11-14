using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace MolimoByLulucaGameDev
{
    public class CursorHide : MonoBehaviour
    {
        public bool IsVisible;

        void Start()
        {
            // Oculta o cursor quando o jogo inicia
            IsVisible = false;
            Cursor.visible = IsVisible;
        }

        void Update()
        {
            if (Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                IsVisible = !IsVisible;
            }

            Cursor.visible = IsVisible;
        }
    }
}