using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using VIAVR.Scripts.Core;

namespace VIAVR.Scripts.UI.Paginator
{
    public class Paginator : MonoBehaviour
    {
        public enum Mode
        {
            HORIZONTAL, VERTICAL
        }
    
        private enum PageAction
        {
            UPDATE = 0,
            NEXT_PAGE = -1,
            PREV_PAGE = 1
        }
    
        // класс страницы, главные поля для использования это Number, Items и ItemsIndices на основе которых грузим контент
        public class Page
        {
            private readonly int _maxContentIndex; // количество элементов (не страниц) - 1
            private readonly int _itemsPerPage; // элементов на страницу
        
            public readonly int[] ItemsIndices; // тут можно получить индексы чтоб загрузить контент из массива данных элементов на основе которого созданы страницы
            public readonly Transform Transform; // UI страницы - контейнер для элементов
        
            public bool Loading { get; set; }
        
            private int _number; // номер страницы
            public int Number
            {
                get => _number;
                set
                {
                    _number = value;
                    // пересчет индексов при смене номера страницы
                    for (int i = 0; i < ItemsIndices.Length; i++)
                        ItemsIndices[i] = _number * _itemsPerPage + i;
                }
            }

            private readonly GameObject[] _items; // элементы которые содержит страница
            public GameObject[] Items // UI объекты элементов под страницей, в них грузим данные для отображения
            {
                get
                {
                    for (int i = 0; i < Transform.childCount; i++)
                    {
                        if(i >= _items.Length) break;
                    
                        if (_items[i] == null)
                            _items[i] = Transform.GetChild(i).gameObject;
                        // если элементов меньше чем может отобразить страница, то лишние скрываются
                        _items[i].SetActive(ItemsIndices[i] <= _maxContentIndex);
                    }
                
                    return _items;
                }
            }

            public Page(int number, int itemsPerPage, int itemsCount, Transform transform)
            {
                _number = number;
                _itemsPerPage = itemsPerPage;
                _maxContentIndex = itemsCount - 1;
            
                Transform = transform;

                ItemsIndices = new int[itemsPerPage];

                _items = new GameObject[itemsPerPage];

                Number = _number;
            }
        }

        public event Action<int, int, int> OnPageChanged; //<страница, всего страниц, одновременно отображаемых страниц> возвращает номер новой страницы (первая страница == 0)
        public event Action<Page> OnRequestLoadContentToPage; // возвращает UI элемент и данные в которые грузим контент

        public event Action<float> OnChangePageAnimationStarted; // <float> - длительность анимации в секундах
        public event Action OnChangePageAnimationFinished;
    
        [SerializeField] private Mode _mode;

        [SerializeField] private bool _loop;

        [SerializeField] private bool _awaitLoadBeforeAnimation = true;
    
        [SerializeField] private float _slideAnimationDuation = 1f;
        [SerializeField] private int _visiblePagesCount = 1; // сколько страниц одновременно отображается в панели
    
        [SerializeField] private GameObject _pagePrefab; // префаб страницы-контейнера
        [SerializeField] private Transform _pagesContainer; // под этим UI элементом создаются объекты страниц

        private readonly List<Page> _pagesCreated = new List<Page>(); // массив страниц (не всех, а созданных "с оптимизацией")
        private readonly List<GameObject> _contentsCreated = new List<GameObject>();

        private GameObject _contentPrefab; // перфаб элементов (поле нужно по сути только для RecycleAll)

        private int _itemsPerPage; // сколько элементов отображается на страницу
        private int _itemsCount; // число ВСЕХ элементов
        private int _pagesCount; // число ВСЕХ страниц
        private int _currentPageNumber;

        public Mode PagesMode => _mode;

        public bool Looped => _loop;

        public int PagesCount => _pagesCount;
    
        public int CurrentPageNumber
        {
            get => _currentPageNumber;
            set
            {
                _currentPageNumber = value;
            
                for (int i = 0; i < _pagesCreated.Count; i++)
                    _pagesCreated[i].Number = CurrentPageNumber + i;

                OnPageChanged?.Invoke(CurrentPageNumber, _pagesCount, _visiblePagesCount);
            }
        } // номер текущей страницы это номер крайней левой отображаемой страницы, если _visiblePagesCount > 1

        public int VisiblePagesCount => _visiblePagesCount;

        private bool _animationNotFinished; // true если анимация смены страниц не кончилась

        private Vector2 PageSize { get; set; } // размер страницы, x - ширина, y - высота
        private Vector2 PageHalfSize => PageSize * 0.5f;

        // создать страницы для отображения контента, <D - класс/структура данных, T - MonoBehaviour класс на префабе>
        /// <summary>
        /// Создать страницы для отображения контента
        /// </summary>
        /// <param name="itemsPerPage">Количество элементов (напр. превьюх) на одну страницу</param>
        /// <param name="itemsCount">Сколько всего элементов</param>
        /// <param name="contentPrefab">Префаб элемента</param>
        /// <param name="mode"></param>
        /// <typeparam name="D">класс/структура данных, используется компонентом</typeparam>
        /// <typeparam name="T">Компонент префаба элемента с интерфейсом IPaginable</typeparam>
        /// <returns>массив созданных компонентов префаба (напр. превьюх), с оптимизацией</returns>
        public async UniTask<T[]> PaginationSetup<D, T>(int itemsPerPage, int itemsCount, GameObject contentPrefab) where T : IPaginableElement<D> where D : IPaginableData
        {
            ClearPages();

            _contentPrefab = contentPrefab;

            _itemsCount = itemsCount;
            _itemsPerPage = itemsPerPage;
            _pagesCount = Mathf.CeilToInt((float)itemsCount / itemsPerPage);
        
            ObjectPOOL.CreatePool(_contentPrefab, _visiblePagesCount * _itemsPerPage);
            ObjectPOOL.CreatePool(_pagePrefab, _visiblePagesCount + 1);

            // размер панели с страницами нарезаем на количество отображаемых страниц
            Vector2 containerSize = _pagesContainer.GetComponent<RectTransform>().rect.size;

            switch (_mode)
            {
                case Mode.HORIZONTAL:
                    PageSize = new Vector2(Mathf.FloorToInt(containerSize.x / _visiblePagesCount), containerSize.y);
                    break;
            
                case Mode.VERTICAL:
                    PageSize = new Vector2(containerSize.x, Mathf.FloorToInt(containerSize.y / _visiblePagesCount));
                    break;
            
                default:
                    throw new ArgumentOutOfRangeException(nameof(_mode), _mode, null);
            }
        
            for (int i = 0; i < _visiblePagesCount + 1; i++)
            {
                //GameObject page = Instantiate(_pagePrefab, _pagesContainer);
                GameObject page = ObjectPOOL.Spawn(_pagePrefab, _pagesContainer);
                page.SetActive(true);
            
                page.name = "Page_" + i;
            
                page.GetComponent<RectTransform>().sizeDelta = PageSize;
            
                _pagesCreated.Add(new Page(i, _itemsPerPage, itemsCount, page.transform));
            }

            CurrentPageNumber = 0;

            int itemsToCreateCount = (_visiblePagesCount + 1) * _itemsPerPage;

            var itemsComponents = new T[itemsToCreateCount];

            for (int i = 0; i < itemsToCreateCount; i++)
            {
                GameObject item = ObjectPOOL.Spawn(_contentPrefab, _pagesCreated[i / _itemsPerPage].Transform);
                item.SetActive(false);
            
                _contentsCreated.Add(item);
            
                var paginable = item.GetComponent<T>();

                OnChangePageAnimationStarted -= paginable.ChangePageAnimationStarted;
                OnChangePageAnimationStarted += paginable.ChangePageAnimationStarted;
            
                OnChangePageAnimationFinished -= paginable.ChangePageAnimationFinished;
                OnChangePageAnimationFinished += paginable.ChangePageAnimationFinished;

                itemsComponents[i] = paginable;
            }

            foreach (var page in _pagesCreated)
                OnRequestLoadContentToPage?.Invoke(page);

            await PaginationAction(PageAction.UPDATE);
        
            CheckFewPagesPositions();

            return itemsComponents;
        }

        // если страниц меньше чем _visiblePagesCount
        // то они при вертикальной расстановке сдвигаются так чтоб видимые страницы висели сверху а не снизу
        async void CheckFewPagesPositions() 
        {
            if(_pagesCount >= _visiblePagesCount || PagesMode == Mode.HORIZONTAL) return;
        
            await UniTask.NextFrame();
        
            Debug.Log("Выравнивание вертикального Пагинатора", this);
            await PaginationAction(PageAction.PREV_PAGE, false);

            for (int i = 0; i < _pagesCreated.Count; i++)
                _pagesCreated[i].Transform.gameObject.SetActive(i > 0 && i <= _pagesCount);
        }

        // метод смены страницы (или PageAction.UPDATE для обновления данных без смены страницы)
        private async UniTask PaginationAction(PageAction pageAction, bool animate = true)
        {
            if (_pagesContainer.childCount < 1)
            {
                Debug.Log("<color=yellow>PagesContainer has no Pages</color>");
                return;
            }
        
            if (pageAction == PageAction.UPDATE)
                animate = false;
        
            var first = _pagesContainer.GetChild(0);
            var last = _pagesContainer.GetChild(_pagesContainer.childCount - 1);
        
            int direction = (int) pageAction;
        
            // выравнивание страниц по центру контейнера в зависимости от количества отображаемых одновременно страниц
            float nextPositionX = 0;
            float nextPositionY = 0;

            if (_mode == Mode.HORIZONTAL)
            {
                nextPositionX = (_visiblePagesCount - 1) * PageHalfSize.x;

                if (direction > 0)
                    nextPositionX -= PageSize.x * _visiblePagesCount;
                else
                    nextPositionX -= PageSize.x * (_visiblePagesCount - 1);
            }
            else
            {
                nextPositionY = (_visiblePagesCount - 1) * PageHalfSize.y;

                if (direction > 0)
                    nextPositionY -= PageSize.y * _visiblePagesCount;
                else
                    nextPositionY -= PageSize.y * (_visiblePagesCount - 1);
            }

            bool condition = _mode == Mode.HORIZONTAL ?
                first.localPosition.x < -PageHalfSize.x * _visiblePagesCount :
                first.localPosition.y < -PageHalfSize.y * _visiblePagesCount ;

            Page pageToLoad = null;

            // при прокрутке на "следующую" страницу объеты смещаются влево, крайний элемент перемещаем в нужную позицию
            if (direction < 0 && condition)
            {
                first.SetAsLastSibling();
                first.localPosition = -first.localPosition;
            
                _pagesCreated.Move(0, _pagesCreated.Count - 1);

                RecalculatePages();
            
                // отдаем нужную инфу для прогрузки контента в эту страницу
                pageToLoad = _pagesCreated.Last();
            }
        
            condition = _mode == Mode.HORIZONTAL ? 
                last.localPosition.x > PageHalfSize.x * _visiblePagesCount :
                last.localPosition.y > PageHalfSize.y * _visiblePagesCount ;
        
            // при прокрутке на "предыдущую" страницу объекты смещаются вправо
            if (direction > 0 && condition)
            {
                last.SetAsFirstSibling();
                last.localPosition = -last.localPosition;
            
                _pagesCreated.Move(_pagesCreated.Count - 1, 0);

                RecalculatePages();

                pageToLoad = _pagesCreated.First();
            }

            if (pageToLoad != null)
            {
                OnRequestLoadContentToPage?.Invoke(pageToLoad);

                if(_awaitLoadBeforeAnimation)
                    await UniTask.WaitWhile(() => pageToLoad.Loading);
            }
        
            HashSet<UniTask> moveTasks = new HashSet<UniTask>(); // таски анимации слайдинга
        
            _animationNotFinished = true;

            // изначальная расстановка страниц на "стартовые" позиции
            foreach (Transform page in _pagesContainer)
            {
                page.gameObject.SetActive(true);
            
                page.localPosition = new Vector2(nextPositionX, nextPositionY);
            
                if(_mode == Mode.HORIZONTAL)
                    nextPositionX += PageSize.x;
                else
                    nextPositionY += PageSize.y;
            
                moveTasks.Add(PageSlideAnimation(page, direction, animate)); // анимация
            }
        
            OnChangePageAnimationStarted?.Invoke(_slideAnimationDuation);
        
            await UniTask.WhenAll(moveTasks);
        
            OnChangePageAnimationFinished?.Invoke();
        
            foreach (Transform page in _pagesContainer)
            {
                float position = _mode == Mode.HORIZONTAL ?
                    Mathf.Abs(page.localPosition.x) :
                    Mathf.Abs(page.localPosition.y) ;
            
                float visible = _mode == Mode.HORIZONTAL ?
                    Mathf.Abs(PageHalfSize.x * _visiblePagesCount) :
                    Mathf.Abs(PageHalfSize.y * _visiblePagesCount) ;
            
                // страница вне зоны видимости
                if (position > visible)
                {
                    page.gameObject.SetActive(false);
                    break; // только одна страница может располагаться за краем контейнера страниц
                }
            }

            _animationNotFinished = false;
        }

        async UniTask PageSlideAnimation(Transform page, int direction, bool animate)
        {
            float endPosition = _mode == Mode.HORIZONTAL ?
                page.localPosition.x + PageSize.x * direction :
                page.localPosition.y + PageSize.y * direction ;
                
            if (animate)
            {
                await UniTask.SwitchToMainThread();
            
                if(_mode == Mode.HORIZONTAL)
                    await page.DOLocalMoveX(endPosition, _slideAnimationDuation).AsyncWaitForCompletion();
                else
                    await page.DOLocalMoveY(endPosition, _slideAnimationDuation).AsyncWaitForCompletion();
            }

            page.localPosition = _mode == Mode.HORIZONTAL ?
                new Vector2(endPosition, 0) :
                new Vector2(0, endPosition) ;
        }

        private void ClearPages()
        {
            foreach (var page in _pagesCreated)
                ObjectPOOL.Recycle(page.Transform.gameObject, true);
        
            foreach (var content in _contentsCreated)
                ObjectPOOL.Recycle(content, true);
        
            _pagesCreated.Clear();
            _contentsCreated.Clear();
        }

        public async void SetPage(int pageNumber, bool force = false)
        {
            Debug.Log("SetPage: " + pageNumber);
        
            if (pageNumber < 0 || pageNumber >= _pagesCount)
                Debug.Log($"Page number: {pageNumber} out of range 0-{_pagesCount} and will be clamped");

            pageNumber = Mathf.Clamp(pageNumber, 0, _pagesCount - 1);
        
            if (!force && CurrentPageNumber == pageNumber)
                return;

            CurrentPageNumber = pageNumber;
        
            foreach (var page in _pagesCreated)
                OnRequestLoadContentToPage?.Invoke(page);
        
            RecalculatePages();

            await PaginationAction(PageAction.UPDATE, false);
        }

        public async void NextPage()
        {
            if(_animationNotFinished)
                return;
        
            if(_pagesCount < 2)
                return;
        
            int pageNumber = CurrentPageNumber + 1;

            if (_loop)
            {
                if (pageNumber + _visiblePagesCount > _pagesCount)
                    pageNumber = -1; // высчитывать не надо, всегда -1
            }
            else
            {
                pageNumber = Mathf.Clamp(CurrentPageNumber + 1, 0, _pagesCount - _visiblePagesCount);
            }

            if (CurrentPageNumber == pageNumber)
                return;

            CurrentPageNumber = pageNumber;
        
            //Debug.Log("Next: " + CurrenPageNumber + "/" + _pagesCount + $"({pageNumber})");

            await PaginationAction(PageAction.NEXT_PAGE);
        }

        public async void PrevPage()
        {
            if(_animationNotFinished)
                return;
        
            if(_pagesCount < 2)
                return;
        
            int pageNumber = CurrentPageNumber - 1;
        
            if (_loop)
            {
                if (pageNumber < _visiblePagesCount - _pagesCount)
                    pageNumber = _pagesCount + pageNumber;
            }
            else
            {
                pageNumber = Mathf.Clamp(CurrentPageNumber - 1, 0, _pagesCount - 1);
            }
        
            if (CurrentPageNumber == pageNumber)
                return;
        
            CurrentPageNumber = pageNumber;
        
            //Debug.Log("Prev: " + CurrenPageNumber + "/" + _pagesCount + $"({pageNumber})");

            await PaginationAction(PageAction.PREV_PAGE);
        }

        // Обновить данные о номерах страниц и индексах в экземплярах объектов Page
        void RecalculatePages()
        {
            int lastAction = _pagesContainer.GetChild(0).gameObject.activeSelf ? -1 : 0;
        
            for (int i = 0; i < _pagesContainer.childCount; i++)
            {
                var page = _pagesCreated.First(page1 => page1.Transform == _pagesContainer.GetChild(i));

                if (!Looped)
                {
                    page.Number = Mathf.Clamp(CurrentPageNumber + i + lastAction, 0, CurrentPageNumber + _visiblePagesCount - 1);
                }
                else
                {
                    int number = CurrentPageNumber + i + lastAction;

                    if (number >= _pagesCount)
                        number = _pagesCount - number;

                    if (number < 0)
                        number = _pagesCount + number;

                    page.Number = number;
                }
            }
        }
    }
}