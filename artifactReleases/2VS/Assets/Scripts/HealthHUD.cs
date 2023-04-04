using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class HealthHUD : VisualElement
{
    public new class UxmlFactory : UxmlFactory<HealthHUD, UxmlTraits> { }

    public int width { get; set; }
    public int height { get; set; }

    private VisualElement hbParent;
    private VisualElement hbBackground;
    private VisualElement hbForeground;


    public new class UxmlTraits : VisualElement.UxmlTraits
    {
        UxmlIntAttributeDescription m_width =
            new UxmlIntAttributeDescription { name = "width", defaultValue = 300 };
        UxmlIntAttributeDescription m_height =
            new UxmlIntAttributeDescription { name = "height", defaultValue = 50 };

        public override IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription
        {
            get { yield break; }
        }

        public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
        {
            base.Init(ve, bag, cc);
            var ate = ve as HealthHUD;

            ate.width = m_width.GetValueFromBag(bag, cc);
            ate.height = m_height.GetValueFromBag(bag, cc);

            ate.Clear();
            VisualTreeAsset vt = Resources.Load<VisualTreeAsset>("UI/HealthBarUI");
            VisualElement bar = vt.Instantiate();
            ate.hbParent = bar.Q<VisualElement>("Container");
            ate.hbBackground = bar.Q<VisualElement>("Background");
            ate.hbForeground = bar.Q<VisualElement>("Foreground");
            ate.Add(bar);

            ate.hbParent.style.width = ate.width;
            ate.hbParent.style.height = ate.height;
            ate.style.width = ate.width;
            ate.style.height = ate.height;
        }
    }
}
