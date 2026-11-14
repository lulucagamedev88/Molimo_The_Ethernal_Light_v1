using System;
using System.Collections.Generic;
using UnityEngine;

namespace LulucaDesign
{
    [Serializable]
    public class ColorDesign
    {
        [Header("Identificação")]
        [Tooltip("Prefixo ou caractere que identifica este estilo na Hierarchy (ex: #, -, >>).")]
        public string keyChar = "#";

        [Header("Estilo Visual")]
        [Tooltip("Cor do texto.")]
        public Color textColor = Color.white;

        [Tooltip("Cor de fundo da linha na Hierarchy.")]
        public Color backgroundColor = new Color(0.2f, 0.2f, 0.2f, 1f);

        [Tooltip("Alinhamento do texto.")]
        public TextAnchor textAlignment = TextAnchor.MiddleLeft;

        [Tooltip("Estilo da fonte (Negrito, Itálico, etc).")]
        public FontStyle fontStyle = FontStyle.Normal;

        [Header("Ícone")]
        [Tooltip("Ícone opcional para aparecer na Hierarchy.")]
        public Texture2D icon;

        [Tooltip("Se ativado, o ícone é desenhado à esquerda. Se desativado, substitui o ícone padrão da Unity.")]
        public bool useCustomIconPlacement = true;
    }

    [CreateAssetMenu(fileName = "NewColorPalette", menuName = "Rodrigo Design/Color Palette", order = 100)]
    public class ColorPalette : ScriptableObject
    {
        [SerializeField]
        private List<ColorDesign> colorDesigns = new List<ColorDesign>();

        private List<ColorDesign> _sortedDesignsCache;
        private Dictionary<string, ColorDesign> _lookupCache;
        private bool _isInitialized = false;

        private void OnEnable()
        {
            InitializeCache();
        }

        private void OnValidate()
        {
            InitializeCache();
        }

        /// <summary>
        /// Inicializa e organiza a estrutura de cache em memória para consultas rápidas.
        /// </summary>
        public void InitializeCache()
        {
            if (_lookupCache == null)
            {
                _lookupCache = new Dictionary<string, ColorDesign>(StringComparer.Ordinal);
            }
            else
            {
                _lookupCache.Clear();
            }

            if (_sortedDesignsCache == null)
            {
                _sortedDesignsCache = new List<ColorDesign>();
            }
            else
            {
                _sortedDesignsCache.Clear();
            }

            if (colorDesigns == null) return;

            // Filtra elementos nulos ou com keyChar ainda não preenchido
            foreach (var design in colorDesigns)
            {
                if (design == null) continue;

                // Ignora silenciosamente se o keyChar estiver em branco (novo elemento a ser editado)
                if (string.IsNullOrWhiteSpace(design.keyChar))
                {
                    continue;
                }

                _sortedDesignsCache.Add(design);

                if (!_lookupCache.ContainsKey(design.keyChar))
                {
                    _lookupCache.Add(design.keyChar, design);
                }
                else
                {
                    Debug.LogWarning($"[ColorPalette] KeyChar duplicado '{design.keyChar}' detetado no asset '{name}'. Apenas a primeira ocorrência será considerada.", this);
                }
            }

            // Ordena do maior prefixo para o menor (ex: "###" tem prioridade sobre "#")
            _sortedDesignsCache.Sort((a, b) =>
            {
                if (a == null || b == null) return 0;
                return b.keyChar.Length.CompareTo(a.keyChar.Length);
            });

            _isInitialized = true;
        }

        /// <summary>
        /// Tenta encontrar uma configuração de cor/estilo compatível com o nome do GameObject.
        /// </summary>
        public bool TryGetDesign(string objectName, out ColorDesign design)
        {
            design = null;

            if (string.IsNullOrEmpty(objectName))
                return false;

            if (!_isInitialized || _lookupCache == null || _sortedDesignsCache == null)
            {
                InitializeCache();
            }

            // Pesquisa iterativa garantindo prioridade por tamanho de prefixo
            for (int i = 0; i < _sortedDesignsCache.Count; i++)
            {
                var item = _sortedDesignsCache[i];
                if (item != null && !string.IsNullOrEmpty(item.keyChar))
                {
                    if (objectName.StartsWith(item.keyChar, StringComparison.Ordinal))
                    {
                        design = item;
                        return true;
                    }
                }
            }

            return false;
        }
    }
}