using UnityEngine;
using UnityEditor;

namespace LulucaDesign
{
    [InitializeOnLoad]
    public static class HierarchyCustomizer
    {
        private static ColorPalette palette;

        // Fator de escurecimento aplicado quando o GameObject está desativado.
        // Multiplica os canais RGB (mantendo o alpha intacto) para simular o aspeto "unchecked" da Unity.
        private const float DisabledDimFactor = 0.5f;

        static HierarchyCustomizer()
        {
            EditorApplication.hierarchyWindowItemByEntityIdOnGUI -= OnHierarchyGUI;
            EditorApplication.hierarchyWindowItemByEntityIdOnGUI += OnHierarchyGUI;
            LoadPalette();
        }

        private static void LoadPalette()
        {
            string[] guids = AssetDatabase.FindAssets("t:ColorPalette");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                palette = AssetDatabase.LoadAssetAtPath<ColorPalette>(path);
            }
        }

        private static void OnHierarchyGUI(EntityId entityId, Rect selectionRect)
        {
            GameObject obj = EditorUtility.EntityIdToObject(entityId) as GameObject;
            if (obj == null) return;

            if (palette == null)
            {
                LoadPalette();
                if (palette == null) return;
            }

            if (palette.TryGetDesign(obj.name, out ColorDesign design))
            {
                Rect bgRect = selectionRect;
                bgRect.width += 50; // Cobre o texto que transborda à direita

                bool isSelected = Selection.Contains(obj);
                bool isMouseOver = bgRect.Contains(Event.current.mousePosition);

                // GameObject ativo na hierarquia? (self + todos os pais)
                // Se estiver desativado, aplicamos o efeito "dim" apenas nos RGB.
                bool isDisabled = !obj.activeInHierarchy;

                // Cores base (com dim aplicado se necessário) — nunca alteramos o alpha.
                Color bgColor = isDisabled ? DimRgb(design.backgroundColor, DisabledDimFactor) : design.backgroundColor;
                Color txtColor = isDisabled ? DimRgb(design.textColor, DisabledDimFactor) : design.textColor;

                // 1. DESENHAR O FUNDO (Personalizado ou Selecionado)
                if (isSelected)
                {
                    // Desenha primeiro a cor personalizada do design
                    EditorGUI.DrawRect(bgRect, bgColor);

                    // Desenha uma camada azul semi-transparente por cima para simular a seleção do Unity mantendo a cor
                    Color selectionTint = UNITY_VERSION_IS_DARK
                        ? new Color(0.25f, 0.5f, 0.8f, 0.6f)
                        : new Color(0.24f, 0.49f, 0.9f, 0.6f);
                    EditorGUI.DrawRect(bgRect, selectionTint);
                }
                else if (isMouseOver)
                {
                    // Cor ao passar o rato (Hover) — clareia ligeiramente o RGB base.
                    Color hoverColor = new Color(
                        Mathf.Clamp01(bgColor.r + 0.1f),
                        Mathf.Clamp01(bgColor.g + 0.1f),
                        Mathf.Clamp01(bgColor.b + 0.1f),
                        bgColor.a
                    );
                    EditorGUI.DrawRect(bgRect, hoverColor);
                    EditorApplication.RepaintHierarchyWindow();
                }
                else
                {
                    // Desenha a cor de fundo normal
                    EditorGUI.DrawRect(bgRect, bgColor);
                }

                // 2. TRATAR DO ÍCONE
                if (design.icon != null && design.useCustomIconPlacement)
                {
                    Rect iconRect = new Rect(selectionRect.x, selectionRect.y, 16, 16);
                    iconRect.y += (selectionRect.height - 16) / 2;

                    Color prevGuiColor = GUI.color;
                    if (isDisabled)
                    {
                        // Multiplica o ícone pela mesma intensidade do "dim", sem tocar no alpha.
                        GUI.color = new Color(DisabledDimFactor, DisabledDimFactor, DisabledDimFactor, 1f);
                    }

                    GUI.DrawTexture(iconRect, design.icon);
                    GUI.color = prevGuiColor;

                    selectionRect.xMin += 18;
                }

                // 3. LIMPAR O PREFIXO DO NOME
                string displayName = obj.name;
                if (!string.IsNullOrEmpty(design.keyChar) && displayName.StartsWith(design.keyChar))
                {
                    displayName = displayName.Substring(design.keyChar.Length).Trim();
                }

                // 4. CONFIGURAR O ESTILO DO TEXTO
                GUIStyle style = new GUIStyle(EditorStyles.label)
                {
                    normal = { textColor = txtColor },
                    alignment = design.textAlignment,
                    fontStyle = design.fontStyle
                };

                // Garante que o texto mantém a cor legível mesmo quando selecionado, com foco ou hover
                style.active.textColor = txtColor;
                style.focused.textColor = txtColor;
                style.hover.textColor = txtColor;

                // 5. DESENHAR O TEXTO FINAL LIMPO
                EditorGUI.LabelField(selectionRect, displayName, style);
            }
        }

        /// <summary>
        /// Escurece os canais RGB da cor sem alterar o alpha (evita o efeito de mesclagem de texto).
        /// </summary>
        private static Color DimRgb(Color color, float factor)
        {
            return new Color(color.r * factor, color.g * factor, color.b * factor, color.a);
        }

        // Propriedade auxiliar para detetar se o tema do Unity é escuro (Dark Mode)
        private static bool UNITY_VERSION_IS_DARK => EditorGUIUtility.isProSkin;
    }
}