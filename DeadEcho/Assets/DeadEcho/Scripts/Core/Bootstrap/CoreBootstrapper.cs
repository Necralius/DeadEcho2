using UnityEngine;
using UnityEngine.InputSystem;
using Game.Core.Events;
using Game.Core.Input;
using Game.Core.Pooling;

namespace Game.Core.Bootstrap
{
    /// <summary>
    /// Composition Root: cria serviços e executa tick.
    /// Um único GO na cena.
    /// </summary>
    public sealed class CoreBootstrapper : MonoBehaviour
    {
        [Header("Input")]
        [SerializeField] private InputActionAsset inputActions;

        [Header("Pooling")]
        [SerializeField] private PoolCatalog poolCatalog;
        [SerializeField] private Transform poolRoot;

        public IEventBus EventBus { get; private set; }
        public IInputService Input { get; private set; }
        public IPoolService Pool { get; private set; }

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);

            EventBus = new EventBus();

            Pool = new PoolService(
                eventBus: EventBus,
                catalog: poolCatalog,

                root: poolRoot != null ? poolRoot : transform);

            Input = new InputService(
                eventBus: EventBus,
                inputAsset: inputActions);

            // Se quiser, dá pra aquecer pools aqui:
            Pool.WarmupAll();
        }

        private void OnEnable() => Input.Enable();
        private void OnDisable() => Input.Disable();

        private void Update()
        {
            Input.Tick(Time.deltaTime);
        }
    }
}
