using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;
using VIAVR.Scripts.Core;

public class HeadCursorFader : MonoBehaviour
{
    [SerializeField] private SpriteRenderer cursorSprite;
    [SerializeField] private ControllersHandler controllersHandler;
    
    private const float FadeDuration = 0.2f;

    public void Show()
    {
        if(!controllersHandler.NoControllerMode)
            return;
        
        SetFaded(1);
        controllersHandler.OnUIObjectHoverChanged -= OnUIObjectHoverChanged;
    }

    public void Hide()
    {
        if(!controllersHandler.NoControllerMode)
            return;
        
        SetFaded(0);
        controllersHandler.OnUIObjectHoverChanged += OnUIObjectHoverChanged;
    }
    
    private void SetFaded(float fadeValue) => cursorSprite.DOFade(fadeValue, FadeDuration);

    private void OnUIObjectHoverChanged(List<GameObject> list)
    {
        if (list != null && list.Any(uiObject => uiObject.CompareTag(GameGlobalConsts.ShowCursorOnHoverTag)))
            SetFaded(1);
        else
            SetFaded(0);
    }
}
