using System;
using System.Collections;
using UnityEngine;

namespace Timers
{
    /// <summary>
    /// Timer leve baseado em coroutine. Não usa Update().
    /// </summary>
    public sealed class LightweightTimer : IDisposable
    {
        public float Duration { get; private set; }
        public bool AutoReset { get; private set; }
        public bool UseUnscaledTime { get; private set; }

        /// <summary>Chamado sempre que o timer conclui um ciclo.</summary>
        public Action OnFinish;

        /// <summary>0..1 do ciclo atual.</summary>
        public float Progress01 =>
            Mathf.Approximately(Duration, 0f) ? 1f : Mathf.Clamp01(Elapsed / Duration);

        /// <summary>Tempo decorrido do ciclo atual.</summary>
        public float Elapsed { get; private set; }

        /// <summary>Tempo restante do ciclo atual.</summary>
        public float Remaining => Mathf.Max(0f, Duration - Elapsed);

        public bool IsRunning { get; private set; }
        public bool IsPaused { get; private set; }

        private Coroutine _routine;
        private bool _disposed;

        private LightweightTimer(float duration, bool autoReset, bool useUnscaledTime, Action onFinish)
        {
            Duration = Mathf.Max(0f, duration);
            AutoReset = autoReset;
            UseUnscaledTime = useUnscaledTime;
            OnFinish = onFinish;
        }

        /// <summary>Cria e já inicia o timer.</summary>
        public static LightweightTimer StartNew(
            float durationSeconds,
            Action onFinish,
            bool autoReset = false,
            bool useUnscaledTime = false)
        {
            var t = new LightweightTimer(durationSeconds, autoReset, useUnscaledTime, onFinish);
            t.Start();
            return t;
        }

        /// <summary>Inicia (ou reinicia do zero) e começa a contar.</summary>
        public void Start()
        {
            ThrowIfDisposed();
            StopInternal(silent: true);
            Elapsed = 0f;
            _routine = TimerRunner.Instance.StartCoroutine(Run());
            IsRunning = true;
            IsPaused = false;
        }

        /// <summary>Pausa a contagem (idempotente).</summary>
        public void Pause()
        {
            ThrowIfDisposed();
            if (!IsRunning || IsPaused) return;
            IsPaused = true;
        }

        /// <summary>Retoma a contagem se estiver pausado.</summary>
        public void Resume()
        {
            ThrowIfDisposed();
            if (!IsRunning || !IsPaused) return;
            IsPaused = false;
        }

        /// <summary>Interrompe o timer atual (não dispara OnFinish).</summary>
        public void Stop()
        {
            ThrowIfDisposed();
            StopInternal(silent: false);
        }

        /// <summary>Reinicia a partir do zero.</summary>
        public void Restart()
        {
            ThrowIfDisposed();
            Start();
        }

        /// <summary>Altera a duração (aplica no próximo ciclo; no ciclo atual mantém progress proporc.).</summary>
        public void SetDuration(float newDuration)
        {
            ThrowIfDisposed();
            newDuration = Mathf.Max(0f, newDuration);
            if (Mathf.Approximately(Duration, 0f))
            {
                Duration = newDuration;
                return;
            }
            // mantém o mesmo progress 0..1 ao mudar a duração em tempo real
            var p = Progress01;
            Duration = newDuration;
            Elapsed = Duration * p;
        }

        /// <summary>Libera o timer (para e não pode mais ser usado).</summary>
        public void Dispose()
        {
            if (_disposed) return;
            StopInternal(silent: true);
            OnFinish = null;
            _disposed = true;
        }

        private IEnumerator Run()
        {
            // loop externo: 1 ou vários ciclos quando AutoReset=true
            do
            {
                // ciclo atual
                Elapsed = 0f;
                while (Elapsed < Duration)
                {
                    // espera 1 frame sem alocar com WaitForEndOfFrame? Aqui usamos delta para precisão fina
                    yield return null;

                    if (IsPaused) continue;

                    var dt = UseUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                    // Proteção: se delta vier 0 (pause global), o loop não avança
                    if (dt > 0f)
                        Elapsed += dt;
                }

                // garante fim exato
                Elapsed = Duration;

                // Callback seguro (try/catch para não matar a coroutine)
                try { OnFinish?.Invoke(); }
                catch (Exception e) { Debug.LogException(e); }

                // se não tiver auto reset, encerra
                if (!AutoReset) break;

                // reinicia próximo ciclo automaticamente
                // (sem realocar nada significativo)
            } while (AutoReset);

            // terminou (sem reinício)
            IsRunning = false;
            _routine = null;
        }

        private void StopInternal(bool silent)
        {
            if (_routine != null)
            {
                TimerRunner.Instance.StopCoroutine(_routine);
                _routine = null;
            }
            IsRunning = false;
            IsPaused = false;
            if (!silent)
            {
                // reset progress ao parar explicitamente
                Elapsed = 0f;
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(LightweightTimer));
        }
    }
}
