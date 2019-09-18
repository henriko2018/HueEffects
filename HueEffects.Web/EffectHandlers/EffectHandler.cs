using System.Threading;
using System.Threading.Tasks;
using Q42.HueApi.Interfaces;

namespace HueEffects.Web.EffectHandlers
{
    public abstract class EffectHandler
    {
        protected readonly ILocalHueClient HueClient;
        private Thread _thread;
        protected bool StopFlag;

        protected EffectHandler(ILocalHueClient hueClient)
        {
            HueClient = hueClient;
        }

        public void Start()
        {
#pragma warning disable 4014
            _thread = new Thread(() => DoWork()) { IsBackground = true, Name = nameof(XmasHandler) };
#pragma warning restore 4014
            _thread.Start();
        }

        public void Stop()
        {
            StopFlag = true;
        }

        protected abstract Task DoWork();
    }
}