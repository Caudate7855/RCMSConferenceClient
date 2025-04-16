using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;

//<D - данные, T - скрипт_префаба_куда_грузим_данные>
namespace VIAVR.Scripts.UI.Paginator
{
    public class PaginableContentProcessorBase<D, T> : MonoBehaviour where T : IPaginableElement<D> where D : IPaginableData
    {
        [SerializeField] private int _itemsPerPage = 1;
        [SerializeField] private GameObject _contentPrefab;
    
        private Paginator _paginator;

        private List<UniTask> _loadingTasks = new List<UniTask>();
    
        public bool LoadingPage { get; private set; }

        public List<D> DataList { get; private set; } = new List<D>(); // все данные, на основе которых создаются страницы (страницы оптимизированные! Будут заюзаны данные для создания нужного количества страниц)
        public T[] ItemComponents { get; private set; } // элементы под страницами, созданные "с оптимизацией".

        public async UniTask<T[]> InitializePaginator(Paginator paginator, IEnumerable<D> providedDatas, Func<D, bool> filter = null)
        {
            if (_contentPrefab.GetComponent<T>() == null)
            {
                Debug.Log($"Content Prefab must have script with <color=orange>IPaginable<{typeof(T)}></color> interface!", this);
                ItemComponents = Array.Empty<T>();

                return ItemComponents;
            }

            _paginator = paginator;
        
            InitializeData(providedDatas, filter);
        
            _paginator.OnRequestLoadContentToPage -= ShowContentOnPage;
            _paginator.OnRequestLoadContentToPage += ShowContentOnPage;
        
            ItemComponents = await _paginator.PaginationSetup<D, T>(_itemsPerPage, DataList.Count, _contentPrefab);

            return ItemComponents;
        }

        // в этом методе заполнять _elements
        protected virtual void InitializeData(IEnumerable<D> providedDatas, Func<D, bool> filter)
        {
            DataList.Clear();

            var filteredDatas = providedDatas?.Where(filter ?? (element => true));

            if(filteredDatas != null)
                DataList.AddRange(filteredDatas);
        }

        // типичный и универсальный метод загрузки данных в страницу с контентом, переопределять возможно даже не понадобится
        protected virtual async void ShowContentOnPage(Paginator.Page page)
        {
            _loadingTasks.Clear();

            page.Loading = LoadingPage = true;
        
            for (var i = 0; i < page.ItemsIndices.Length; i++)
            {
                int itemIndex = page.ItemsIndices[i];   
           
                if(itemIndex >= DataList.Count) break;

                T paginable = page.Items[i].GetComponent<T>();
            
                if(paginable != null)
                    _loadingTasks.Add(paginable.SetupFromData(DataList[itemIndex]));
            }

            await UniTask.WhenAll(_loadingTasks);

            page.Loading = LoadingPage = false;
        }
    }
}