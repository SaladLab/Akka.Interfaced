messageTemplate = r'''
    public class ITest__Min##__Invoke : IInterfacedMessage, IAsyncInvokable
    {
        public int a;
        public int b;

        public Type GetInterfaceType() { return typeof(ITest); }

        public async Task<IValueGetable> Invoke(object target)
        {
            var __v = await ((ITest)target).Min##(a, b);
            return (IValueGetable)(new Temp__Result { v = __v });
        }
    }'''

for i in range(1, 21):
    print messageTemplate.replace('##', '%02d' % i)
    
