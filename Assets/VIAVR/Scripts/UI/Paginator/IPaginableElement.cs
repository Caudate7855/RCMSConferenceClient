
// интерфейс юзать для класса который будет висеть на префабе элемента который может отображаться постранично

using Cysharp.Threading.Tasks;

namespace VIAVR.Scripts.UI.Paginator
{
    public interface IPaginableElement<D> where D : IPaginableData
    {
        public D Data { get; set; }
    
        public UniTask SetupFromData(D data);

        public void ChangePageAnimationStarted(float duration);
        public void ChangePageAnimationFinished();

        public void UpdateLocalization();
    }
}