using TMPro;
using UnityEngine;
using VIAVR.Scripts.UI.Activators;

namespace VIAVR.Scripts.UI.Pages
{
    public class PagesDisplay : MonoBehaviour
    {
        [SerializeField] private Paginator.Paginator _paginator;
    
        [Header("can be NONE")]
        [SerializeField] private TextMeshProUGUI _text;

        [SerializeField] private ActivatorBase _buttonNext;
        [SerializeField] private ActivatorBase _buttonPrev;

        private void Awake()
        {
            if(_paginator != null)
                _paginator.OnPageChanged += OnPageChangedHandler;
            else
                Debug.LogError(nameof(_paginator) + " == null", this);
        }

        private void OnEnable()
        {
            if(_paginator == null) return;

            OnPageChangedHandler(_paginator.CurrentPageNumber, _paginator.PagesCount, _paginator.VisiblePagesCount);
        }

        void OnPageChangedHandler(int page, int totalPages, int visiblePages)
        {
            if (totalPages <= visiblePages)
            {
                gameObject.SetActive(false);
                return;
            }
        
            gameObject.SetActive(true);
        
            if(_text != null)
                _text.text = (page + 1) + "  /  " + (totalPages == 0 ? 1 : totalPages);

            if (_buttonPrev != null && !_paginator.Looped)
                _buttonPrev.Activate(page > 0);

            if (_buttonNext != null && !_paginator.Looped)
                _buttonNext.Activate(page < totalPages - visiblePages);
        }
    }
}