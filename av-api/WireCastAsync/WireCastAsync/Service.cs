using System.Threading.Tasks;

namespace WireCastAsync
{
    public class Service
    {
        private static WirecastBinding _wirecast = new WirecastBinding();        

        public async Task<object> getWirecastAsync(object input)
        {
            return await Task.Run(() => { return _wirecast.Get(); });
        }

        public async Task<object> setWirecastAsync(dynamic input)
        {
            return await Task.Run(() => { return _wirecast.Set(input); });
        }
    }
}
