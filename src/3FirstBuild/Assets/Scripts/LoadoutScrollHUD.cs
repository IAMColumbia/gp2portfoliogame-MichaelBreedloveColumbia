using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class LoadoutScrollHUD : VisualElement
{
    public new class UxmlFactory : UxmlFactory<LoadoutScrollHUD, UxmlTraits> { }

    private VisualElement Top;
    private VisualElement Middle;
    private VisualElement Bottom;

    public Player player = null;

    public new class UxmlTraits : VisualElement.UxmlTraits
    {
        public override IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription
        {
            get { yield break; }
        }

        public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
        {
            base.Init(ve, bag, cc);
            var ate = ve as LoadoutScrollHUD;

            ate.Clear();
            VisualTreeAsset vt = Resources.Load<VisualTreeAsset>("UI/LoadoutScroller");

            VisualElement bar = vt.Instantiate();
            ate.Top = bar.Q<VisualElement>("Top");
            ate.Middle = bar.Q<VisualElement>("Middle");
            ate.Bottom = bar.Q<VisualElement>("Bottom");
            ate.Add(bar);

            if (ate.player == null)
                return;
            if (ate.player.Weapons == null)
                return;
            if (ate.player.Weapons.Count < 1)
                return;

            ate.Top.Clear();
            ate.Middle.Clear();
            ate.Bottom.Clear();

            ate.Top.visible = ate.player.Weapons.Count > 0;
            ate.Bottom.visible = ate.player.Weapons.Count > 1;

            int slot = ate.player.WeaponIndex;

            Image MyImage = new Image();
            MyImage.image = ate.player.Weapons[slot].GetComponent<Weapon>().Renderer.sprite.texture;
            ate.Middle.Add(MyImage);

            if (ate.Top.visible)
            {
                if (slot == ate.player.Weapons.Count - 1)
                {
                    MyImage.image = ate.player.Weapons[0].GetComponent<Weapon>().Renderer.sprite.texture;
                }
                else
                {
                    MyImage.image = ate.player.Weapons[slot + 1].GetComponent<Weapon>().Renderer.sprite.texture;
                }
                ate.Top.Add(MyImage);
            }

            if (ate.Bottom.visible)
            {
                if (slot == 0)
                {
                    MyImage.image = ate.player.Weapons[ate.player.Weapons.Count - 1].GetComponent<Weapon>().Renderer.sprite.texture;
                }
                else
                {
                    MyImage.image = ate.player.Weapons[slot - 1].GetComponent<Weapon>().Renderer.sprite.texture;
                }
                ate.Bottom.Add(MyImage);
            }
        }
    }
}
