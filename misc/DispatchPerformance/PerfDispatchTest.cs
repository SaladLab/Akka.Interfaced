using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DispatchPerformance
{
    static class PerfDispatchTest
    {
        const int TestCount = 10000000;
        const int MessageCycle = 20;

        public static void Run()
        {
            for (int i = 0; i < 2; i++)
            {
                DoNothing();
                DoGetType();
                ManualAsDispatch();
                ManualIsDispatch();
                TypeDictionaryDispatch();
                TypeHandleDictionaryDispatch();
                TypeStringDictionaryDispatch();
                Console.WriteLine("");
            }
        }

        private static void DoNothing()
        {
            RunTest("DoNothing", (msg) =>
            {
            });
        }

        private static void DoGetType()
        {
            RunTest("DoGetType", (msg) =>
            {
                if (msg.GetType() == null)
                    throw new Exception();
            });
        }

        private static void ManualAsDispatch()
        {
            Action<object> a01 = (msg) => { };
            Action<object> a02 = (msg) => { };
            Action<object> a03 = (msg) => { };
            Action<object> a04 = (msg) => { };
            Action<object> a05 = (msg) => { };
            Action<object> a06 = (msg) => { };
            Action<object> a07 = (msg) => { };
            Action<object> a08 = (msg) => { };
            Action<object> a09 = (msg) => { };
            Action<object> a10 = (msg) => { };
            Action<object> a11 = (msg) => { };
            Action<object> a12 = (msg) => { };
            Action<object> a13 = (msg) => { };
            Action<object> a14 = (msg) => { };
            Action<object> a15 = (msg) => { };
            Action<object> a16 = (msg) => { };
            Action<object> a17 = (msg) => { };
            Action<object> a18 = (msg) => { };
            Action<object> a19 = (msg) => { };
            Action<object> a20 = (msg) => { };

            RunTest("ManualAsDispatch", (msg) =>
            {
                var m01 = msg as ITest__Min01__Invoke;
                if (m01 != null) { a01(m01); return; }

                var m02 = msg as ITest__Min02__Invoke;
                if (m02 != null) { a02(m02); return; }

                var m03 = msg as ITest__Min03__Invoke;
                if (m03 != null) { a03(m03); return; }

                var m04 = msg as ITest__Min04__Invoke;
                if (m04 != null) { a04(m04); return; }

                var m05 = msg as ITest__Min05__Invoke;
                if (m05 != null) { a05(m05); return; }

                var m06 = msg as ITest__Min06__Invoke;
                if (m06 != null) { a06(m06); return; }

                var m07 = msg as ITest__Min07__Invoke;
                if (m07 != null) { a07(m07); return; }

                var m08 = msg as ITest__Min08__Invoke;
                if (m08 != null) { a08(m08); return; }

                var m09 = msg as ITest__Min09__Invoke;
                if (m09 != null) { a09(m09); return; }

                var m10 = msg as ITest__Min10__Invoke;
                if (m10 != null) { a10(m10); return; }

                var m11 = msg as ITest__Min11__Invoke;
                if (m11 != null) { a11(m11); return; }

                var m12 = msg as ITest__Min12__Invoke;
                if (m12 != null) { a12(m12); return; }

                var m13 = msg as ITest__Min13__Invoke;
                if (m13 != null) { a13(m13); return; }

                var m14 = msg as ITest__Min14__Invoke;
                if (m14 != null) { a14(m14); return; }

                var m15 = msg as ITest__Min15__Invoke;
                if (m15 != null) { a15(m15); return; }

                var m16 = msg as ITest__Min16__Invoke;
                if (m16 != null) { a16(m16); return; }

                var m17 = msg as ITest__Min17__Invoke;
                if (m17 != null) { a17(m17); return; }

                var m18 = msg as ITest__Min18__Invoke;
                if (m18 != null) { a18(m18); return; }

                var m19 = msg as ITest__Min19__Invoke;
                if (m19 != null) { a19(m19); return; }

                var m20 = msg as ITest__Min20__Invoke;
                if (m20 != null) { a20(m20); return; }
            });
        }

        private static void ManualIsDispatch()
        {
            Action<object> a01 = (msg) => { };
            Action<object> a02 = (msg) => { };
            Action<object> a03 = (msg) => { };
            Action<object> a04 = (msg) => { };
            Action<object> a05 = (msg) => { };
            Action<object> a06 = (msg) => { };
            Action<object> a07 = (msg) => { };
            Action<object> a08 = (msg) => { };
            Action<object> a09 = (msg) => { };
            Action<object> a10 = (msg) => { };
            Action<object> a11 = (msg) => { };
            Action<object> a12 = (msg) => { };
            Action<object> a13 = (msg) => { };
            Action<object> a14 = (msg) => { };
            Action<object> a15 = (msg) => { };
            Action<object> a16 = (msg) => { };
            Action<object> a17 = (msg) => { };
            Action<object> a18 = (msg) => { };
            Action<object> a19 = (msg) => { };
            Action<object> a20 = (msg) => { };

            RunTest("ManualIsDispatch", (msg) =>
            {
                if (msg is ITest__Min01__Invoke)
                {
                    a01(msg);
                    return;
                }
                if (msg is ITest__Min02__Invoke)
                {
                    a02(msg);
                    return;
                }
                if (msg is ITest__Min03__Invoke)
                {
                    a03(msg);
                    return;
                }
                if (msg is ITest__Min04__Invoke)
                {
                    a04(msg);
                    return;
                }
                if (msg is ITest__Min05__Invoke)
                {
                    a05(msg);
                    return;
                }
                if (msg is ITest__Min06__Invoke)
                {
                    a06(msg);
                    return;
                }
                if (msg is ITest__Min07__Invoke)
                {
                    a07(msg);
                    return;
                }
                if (msg is ITest__Min08__Invoke)
                {
                    a08(msg);
                    return;
                }
                if (msg is ITest__Min09__Invoke)
                {
                    a09(msg);
                    return;
                }
                if (msg is ITest__Min10__Invoke)
                {
                    a10(msg);
                    return;
                }
                if (msg is ITest__Min11__Invoke)
                {
                    a11(msg);
                    return;
                }
                if (msg is ITest__Min12__Invoke)
                {
                    a12(msg);
                    return;
                }
                if (msg is ITest__Min13__Invoke)
                {
                    a13(msg);
                    return;
                }
                if (msg is ITest__Min14__Invoke)
                {
                    a14(msg);
                    return;
                }
                if (msg is ITest__Min15__Invoke)
                {
                    a15(msg);
                    return;
                }
                if (msg is ITest__Min16__Invoke)
                {
                    a16(msg);
                    return;
                }
                if (msg is ITest__Min17__Invoke)
                {
                    a17(msg);
                    return;
                }
                if (msg is ITest__Min18__Invoke)
                {
                    a18(msg);
                    return;
                }
                if (msg is ITest__Min19__Invoke)
                {
                    a19(msg);
                    return;
                }
                if (msg is ITest__Min20__Invoke)
                {
                    a20(msg);
                    return;
                }
            });
        }

        private static void TypeDictionaryDispatch()
        {
            var types = ITest__MessageTable.GetMessageTypes();
            var msgLength = Math.Min(MessageCycle, types.GetLength(0));

            // http://www.dotnetperls.com/dictionary-optimization
            var table = new Dictionary<Type, Action<object>>(msgLength * 5);
            for (int i=0; i< msgLength; i++)
            {
                table[types[i, 0]] = (msg) => { };
            }

            RunTest("TypeDictionaryDispatch", msg =>
            {
                Action<object> handler;
                if (table.TryGetValue(msg.GetType(), out handler))
                    handler(msg);
            });
        }

        private static void TypeHandleDictionaryDispatch()
        {
            var types = ITest__MessageTable.GetMessageTypes();

            var msgLength = Math.Min(MessageCycle, types.GetLength(0));
            var table = new Dictionary<RuntimeTypeHandle, Action<object>>();
            for (int i = 0; i < msgLength; i++)
            {
                table[types[i, 0].TypeHandle] = (msg) => { };
            }

            RunTest("TypeHandleDictionaryDispatch", msg =>
            {
                Action<object> handler;
                if (table.TryGetValue(msg.GetType().TypeHandle, out handler))
                    handler(msg);
            });
        }

        private static void TypeStringDictionaryDispatch()
        {
            var types = ITest__MessageTable.GetMessageTypes();

            var msgLength = Math.Min(MessageCycle, types.GetLength(0));
            var table = new Dictionary<string, Action<object>>();
            for (int i = 0; i < msgLength; i++)
            {
                table[types[i, 0].FullName] = (msg) => { };
            }

            RunTest("TypeStringDictionaryDispatch", msg =>
            {
                Action<object> handler;
                if (table.TryGetValue(msg.GetType().FullName, out handler))
                    handler(msg);
            });
        }

        private static void RunTest(string testName, Action<object> test)
        {
            var types = ITest__MessageTable.GetMessageTypes();
            var msgs = new object[Math.Min(MessageCycle, types.GetLength(0))];
            for (var i = 0; i < msgs.Length; i++)
            {
                msgs[i] = Activator.CreateInstance(types[i, 0]);
            }

            var sw = Stopwatch.StartNew();

            for (var i = 0; i < TestCount; i++)
            {
                test(msgs[i % msgs.Length]);
            }
            sw.Stop();

            Console.WriteLine($"{testName.PadRight(30, ' ')} {sw.ElapsedMilliseconds} ms");
        }
    }
}
