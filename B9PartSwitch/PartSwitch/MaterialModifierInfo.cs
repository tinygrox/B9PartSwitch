﻿using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using B9PartSwitch.Fishbones;
using B9PartSwitch.Fishbones.Context;
using B9PartSwitch.PartSwitch.PartModifiers;
using B9PartSwitch.Utils;

namespace B9PartSwitch
{
    public class MaterialModifierInfo : IContextualNode
    {
        [NodeData]
        public string name;

        [NodeData(name = "baseTransform")]
        public List<IStringMatcher> baseTransformNames = new List<IStringMatcher>();

        [NodeData(name = "transform")]
        public List<IStringMatcher> transformNames = new List<IStringMatcher>();

        [NodeData(name = "FLOAT")]
        public List<FloatPropertyModifierInfo> floatPropertyModifierInfos = new List<FloatPropertyModifierInfo>();

        [NodeData(name = "COLOR")]
        public List<ColorPropertyModifierInfo> colorPropertyModifierInfos = new List<ColorPropertyModifierInfo>();

        [NodeData(name = "TEXTURE")]
        public List<TexturePropertyModifierInfo> texturePropertyModifierInfos = new List<TexturePropertyModifierInfo>();

        public void Load(ConfigNode node, OperationContext context) => this.LoadFields(node, context);

        public void Save(ConfigNode node, OperationContext context) => this.SaveFields(node, context);

        public IEnumerable<IPartModifier> CreateModifiers(Transform rootTransform, Action<string> onError)
        {
            rootTransform.ThrowIfNullArgument(nameof(rootTransform));

            IEnumerable<Renderer> renderers;
            if (baseTransformNames.IsNullOrEmpty() && transformNames.IsNullOrEmpty())
            {
                renderers = rootTransform.GetComponentsInChildren<Renderer>(true);
            }
            else
            {
                renderers = GetBaseTransformRenderers(rootTransform, onError);
                renderers = renderers.Concat(GetTransformRenderers(rootTransform, onError));

                renderers = renderers.Distinct();
            }

            foreach (FloatPropertyModifierInfo floatPropertyModifierInfo in floatPropertyModifierInfos)
            {
                foreach (IPartModifier partModifier in floatPropertyModifierInfo.CreateModifiers(renderers))
                {
                    yield return partModifier;
                }
            }

            foreach (ColorPropertyModifierInfo colorPropertyModifierInfo in colorPropertyModifierInfos)
            {
                foreach (IPartModifier partModifier in colorPropertyModifierInfo.CreateModifiers(renderers))
                {
                    yield return partModifier;
                }
            }

            foreach (TexturePropertyModifierInfo texturePropertyModifierInfo in texturePropertyModifierInfos)
            {
                foreach (IPartModifier partModifier in texturePropertyModifierInfo.CreateModifiers(renderers, onError))
                {
                    yield return partModifier;
                }
            }
        }

        private IEnumerable<Renderer> GetBaseTransformRenderers(Transform rootTransform, Action<string> onError)
        {
            IEnumerable<Renderer> result = Enumerable.Empty<Renderer>();
            if (baseTransformNames == null) return result;

            foreach (IStringMatcher baseTransformName in baseTransformNames)
            {
                bool foundTransform = false;

                foreach (Transform transform in rootTransform.TraverseHierarchy().Where(t => baseTransformName.Match(t.name)))
                {
                    foundTransform = true;

                    Renderer[] transformRenderers = transform.GetComponentsInChildren<Renderer>(true);

                    if (transformRenderers.Length == 0)
                    {
                        onError($"No renderers found on transform '{baseTransformName}'");
                        continue;
                    }

                    result = result.Concat(transformRenderers);
                }

                if (!foundTransform) onError($"No transforms matching '{baseTransformName}' found");
            }

            return result;
        }

        private IEnumerable<Renderer> GetTransformRenderers(Transform rootTransform, Action<string> onError)
        {
            IEnumerable<Renderer> result = Enumerable.Empty<Renderer>();
            if (transformNames == null) return result;

            foreach (IStringMatcher transformName in transformNames)
            {
                bool foundTransform = false;

                foreach (Transform transform in rootTransform.TraverseHierarchy().Where(t => transformName.Match(t.name)))
                {
                    foundTransform = true;
                    Renderer[] transformRenderers = transform.GetComponents<Renderer>();

                    if (transformRenderers.Length == 0)
                    {
                        onError($"No renderers found on transform '{transformName}'");
                        continue;
                    }

                    result = result.Concat(transformRenderers);
                }

                if (!foundTransform) onError($"No transforms matching '{transformName}' found");
            }

            return result;
        }
    }
}
