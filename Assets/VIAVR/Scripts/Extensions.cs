using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace VIAVR.Scripts
{
    public static class Extensions
    {
        public static Task SafeContinueWith(this Task task, Action<Task> action)
        {
            return task.ContinueWith(action, TaskScheduler.FromCurrentSynchronizationContext());
        }

        public static Task SafeContinueWith<TIn>(this Task<TIn> task, Action<Task<TIn>> action)
        {
            return task.ContinueWith(action, TaskScheduler.FromCurrentSynchronizationContext());
        }

        public static Task<TOut> SafeContinueWith<TOut>(this Task task, Func<Task, TOut> action)
        {
            return task.ContinueWith(action, TaskScheduler.FromCurrentSynchronizationContext());
        }

        public static Task<TOut> SafeContinueWith<TIn, TOut>(this Task<TIn> task, Func<Task<TIn>, TOut> action)
        {
            return task.ContinueWith(action, TaskScheduler.FromCurrentSynchronizationContext());
        }
    
        public static Task LogException(this Task task)
        {
            return task.ContinueWith(t =>
            {
                if (t.Status != TaskStatus.RanToCompletion)
                    Debug.LogErrorFormat("Async exception caught: {0}", t.Exception.InnerException);
            });
        }
    
        public static void Move<T>(this List<T> list, int oldIndex, int newIndex)
        {
            var item = list[oldIndex];

            list.RemoveAt(oldIndex);
            list.Insert(newIndex, item);
        }

        public static void Move<T>(this List<T> list, T item, int newIndex)
        {
            if (item != null)
            {
                var oldIndex = list.IndexOf(item);
                if (oldIndex > -1)
                {
                    list.RemoveAt(oldIndex);
                    list.Insert(newIndex, item);
                }
            }
        }

        // вписывает картинку в размеры парента по высоте учитывая соотношение сторон исходной текстуры,
        // AspectRatioFitter юзать для множества элементов накладно, т.к.содержит Update
        public static async void FitInContainerHeight(this Image image)
        {
            if(image == null) return;
        
            Sprite sprite = image.sprite;
        
            if(sprite == null || sprite.texture == null || sprite.texture.height == 0) return;
        
            await UniTask.WaitForEndOfFrame(); // нужно чтоб у RectTransform движком просчитался реальный размер transform.rect
        
            RectTransform rectTransform = (RectTransform)image.transform;

            float containerHeight = ((RectTransform)rectTransform.parent).rect.size.y;
            float aspect = (float)sprite.texture.width / sprite.texture.height;

            rectTransform.sizeDelta = new Vector2(containerHeight * aspect, containerHeight);
        }
    
        public static async void FitInContainerHeight(this RawImage image)
        {
            if(image == null) return;
        
            var texture = image.texture;
        
            await UniTask.WaitForEndOfFrame(); // нужно чтоб у RectTransform движком просчитался реальный размер transform.rect
        
            if(texture == null || texture.height == 0) return;
        
            RectTransform rectTransform = (RectTransform)image.transform;

            float containerHeight = ((RectTransform)rectTransform.parent).rect.size.y;
            float aspect = (float)texture.width / texture.height;

            rectTransform.sizeDelta = new Vector2(containerHeight * aspect, containerHeight);
        }
    
        public static string FirstCharToUpper(this string input)
        {
            return input switch
            {
                null => throw new ArgumentNullException(nameof(input)),
                "" => throw new ArgumentException($"{nameof(input)} cannot be empty", nameof(input)),
                _ => input[0].ToString().ToUpper() + input.Substring(1)
            };
        }
    }
}