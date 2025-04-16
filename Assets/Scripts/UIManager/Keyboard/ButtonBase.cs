using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class ButtonBase : ActivatorBase, IPointerDownHandler, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private bool _useCustomHeadClickTime = false;
    [SerializeField] private float _customHeadClickTime = 2f;
    [SerializeField] private bool _clickOnPointerDown = false;
    
    private Button _button;
    
    public event Action OnClick;
    public event Action OnPointerEnterEventHandler;
    public event Action OnPointerExitEventHandler;
    
    public Button Button
    {
        get
        {
            if (_button == null)
            {
                _button = GetComponent<Button>();

                if (_button == null)
                    _button = gameObject.AddComponent<Button>();
            }
            return _button;
        }
    }

    public float? CustomHeadClickTime => _useCustomHeadClickTime ? (float?)_customHeadClickTime : null;

    public virtual void Awake() { }

    public bool Interactable
    {
        get => Button.interactable;
        set => Activate(value);
    }

    private void OnEnable()
    {
        if(!_clickOnPointerDown)
            Button.onClick.AddListener(OnClickHandler);
    }
    
    private void OnDisable()
    {
        Button.onClick.RemoveAllListeners();
    }

    public void Click()
    {
        OnClickHandler();
    }

    protected virtual void OnClickHandler()
    {
        OnClick?.Invoke();
    }
    
    // Если кнопка находится в ScrollView клик (onClick, нажал-отжал) по кнопке не проходит даже при миллиметровом скроллинге
    // в шлеме практически невозможно кликнуть кнопку так чтоб ничего не проскроллилось
    public void OnPointerDown(PointerEventData eventData)
    {
        if(_clickOnPointerDown)
            OnClickHandler();
    }
    
    // при state == false кнопка станет некликабельной
    public override UniTask Activate(bool state, bool withAnimation = false)
    {
        base.Activate(state, withAnimation);

        Button.interactable = state;
        
        return UniTask.CompletedTask;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        OnPointerEnterEventHandler?.Invoke();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        OnPointerExitEventHandler?.Invoke();
    }
}
