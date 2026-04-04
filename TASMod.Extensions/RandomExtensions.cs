// uses the prng cloning for Net5 randoms defined in this stackoverflow post:
// https://stackoverflow.com/questions/8188844/is-there-a-way-to-grab-the-actual-state-of-system-random
// for some reason the way I was doing it manually just did not work at all...
// probably because we have no type information on the internal state of the random object for reflector to churn on

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using StardewValley;
using TASMod.Helpers;

namespace TASMod.Extensions
{
    public static class RandomExtensions
    {
        public static int SharedSeed = 0;
        public static Random SharedRandom;
        public static Dictionary<int, Dictionary<int, List<string>>> StackTraces;

        public const string ImplName = "_impl";
        public const string Net6_ImplName = "XoshiroImpl";
        public const string Net6_ImplTypeName = "System.Random+XoshiroImpl";
        public const string Net6_S0Name = "_s0";
        public const string Net6_S1Name = "_s1";
        public const string Net6_S2Name = "_s2";
        public const string Net6_S3Name = "_s3";
        private static FieldInfo S0Info;
        private static FieldInfo S1Info;
        private static FieldInfo S2Info;
        private static FieldInfo S3Info;

        public const string Net5_ImplName = "Net5CompatSeedImpl";
        public const string Net5_ImplTypeName = "System.Random+Net5CompatSeedImpl";
        public const string Net5_CompatPrngTypeName = "System.Random+CompatPrng";
        public const string Net5_Impl_CompatPrng = "_prng";
        public const string CompatPrng_SeedArrayName = "_seedArray";
        public const string CompatPrng_InextName = "_inext";
        public const string CompatPrng_InextpName = "_inextp";
        private static FieldInfo ImplInfo;
        private static FieldInfo PrngInfo;
        private static FieldInfo seedArrayInfo;
        private static FieldInfo inextInfo;
        private static FieldInfo inextpInfo;

        internal class Holder
        {
            public int Index = 0;
            public int Seed = 0;
            public bool IsNet6 = false;
        }

        internal static ConditionalWeakTable<Random, Holder> RandomData;
        private static readonly ConditionalWeakTable<Random, Holder>.CreateValueCallback HolderFactory =
            _ => new Holder();
        [ThreadStatic]
        private static Random LastDataRandom;
        [ThreadStatic]
        private static Holder LastDataHolder;
        [ThreadStatic]
        private static bool SuppressTrackingForCurrentThread;
        private static Type Net5ImplType;
        private static Type Net6ImplType;
        private static Type CompatPrngType;
        private static Func<object> CreateNet6Impl;
        static RandomExtensions()
        {
            Net5ImplType = Type.GetType(Net5_ImplTypeName)!;
            RandomData = new ConditionalWeakTable<Random, Holder>();
            ImplInfo = typeof(Random).GetField(
                ImplName,
                BindingFlags.Instance | BindingFlags.NonPublic
            )!;
            PrngInfo = Net5ImplType
                .GetField(Net5_Impl_CompatPrng, BindingFlags.Instance | BindingFlags.NonPublic)!;
            CompatPrngType = Type.GetType(Net5_CompatPrngTypeName);
            seedArrayInfo = CompatPrngType.GetField(
                CompatPrng_SeedArrayName,
                BindingFlags.Instance | BindingFlags.NonPublic
            )!;
            inextInfo = CompatPrngType.GetField(
                CompatPrng_InextName,
                BindingFlags.Instance | BindingFlags.NonPublic
            )!;
            inextpInfo = CompatPrngType.GetField(
                CompatPrng_InextpName,
                BindingFlags.Instance | BindingFlags.NonPublic
            )!;

            Net6ImplType = Type.GetType(Net6_ImplTypeName);
            CreateNet6Impl = BuildFactory(Net6ImplType);
            S0Info = Net6ImplType.GetField(
                Net6_S0Name,
                BindingFlags.Instance | BindingFlags.NonPublic
            )!;
            S1Info = Net6ImplType.GetField(
                Net6_S1Name,
                BindingFlags.Instance | BindingFlags.NonPublic
            )!;
            S2Info = Net6ImplType.GetField(
                Net6_S2Name,
                BindingFlags.Instance | BindingFlags.NonPublic
            )!;
            S3Info = Net6ImplType.GetField(
                Net6_S3Name,
                BindingFlags.Instance | BindingFlags.NonPublic
            )!;

            SharedRandom = new Random(SharedSeed);
            StackTraces = new();
        }

        public static void Reset()
        {
            SharedRandom = new Random(SharedSeed);
            StackTraces.Clear();
        }

        public static bool IsTrackingEnabledForCurrentThread => !SuppressTrackingForCurrentThread;

        public static bool SetTrackingEnabledForCurrentThread(bool enabled)
        {
            bool previous = !SuppressTrackingForCurrentThread;
            SuppressTrackingForCurrentThread = !enabled;
            if (enabled)
            {
                LastDataRandom = null;
                LastDataHolder = null;
            }
            return previous;
        }

        public static void PushTrace(Random r, int frame, int playerIndex)
        {
            if (Controller.PushStackTrace && r == Game1.random)
            {
                if (!StackTraces.ContainsKey(frame))
                {
                    StackTraces.Add(frame, new Dictionary<int, List<string>>());
                }
                if (!StackTraces[frame].ContainsKey(playerIndex))
                {
                    StackTraces[frame].Add(playerIndex, new List<string>());
                }
                StackTraces[frame][playerIndex].Add(Environment.StackTrace);
            }
        }

        public static void Update()
        {
            SharedRandom.Next();
        }

        public static void InitData(this Random random)
        {
            if (!IsTrackingEnabledForCurrentThread)
            {
                return;
            }

            var data = GetData(random);
            data.IsNet6 = true;
            data.Seed = 0;
            data.Index = 0;
        }

        public static void InitData(this Random random, int seed)
        {
            if (!IsTrackingEnabledForCurrentThread)
            {
                return;
            }

            var data = GetData(random);
            data.IsNet6 = false;
            data.Seed = seed;
            data.Index = 0;
        }

        private static Holder GetData(Random random)
        {
            if (ReferenceEquals(random, LastDataRandom))
            {
                return LastDataHolder;
            }

            if (!RandomData.TryGetValue(random, out Holder data))
            {
                data = RandomData.GetValue(random, HolderFactory);
            }

            LastDataRandom = random;
            LastDataHolder = data;
            return data;
        }

        public static bool IsNet6(this Random random)
        {
            return random.GetImpl().GetType().Name == Net6_ImplName;
        }

        //Random > Impl > CompatPrng
        public static object GetImpl(this Random random) =>
            ImplInfo.GetValueDirect(__makeref(random))!;

        public static void SetImpl(this Random random, object impl) =>
            ImplInfo.SetValueDirect(__makeref(random), impl);

        public static object GetCompatPrng(object impl) =>
            PrngInfo.GetValueDirect(__makeref(impl))!;

        public static object GetCompatPrng(this Random random)
        {
            if (random.IsNet6())
            {
                throw new Exception("random types do not match");
            }
            object impl = GetImpl(random);
            return PrngInfo.GetValueDirect(__makeref(impl))!;
        }

        // allows copying of random objects
        public static Random Copy(this Random random)
        {
            Random other = new Random(0);
            if (random.IsNet6())
            {
                CloneNet6(random, other);
            }
            else
            {
                CloneNet5(random, other);
            }
            return other;
        }

        public static int Peek(this Random random)
        {
            Random r = random.Copy();
            return r.Next();
        }

        public static double PeekDouble(this Random random)
        {
            Random r = random.Copy();
            return r.NextDouble();
        }

        private static void CloneNet5(this Random random, Random other)
        {
            object otherImpl = other.GetImpl();
            TypedReference otherImplRef = __makeref(otherImpl);
            object otherPrng = PrngInfo.GetValueDirect(otherImplRef)!;

            object currImpl = random.GetImpl();
            TypedReference currImplRef = __makeref(currImpl);
            object currPrng = PrngInfo.GetValueDirect(currImplRef)!;

            seedArrayInfo.SetValue(otherPrng, ((int[])seedArrayInfo.GetValue(currPrng)).Clone());
            inextInfo.SetValue(otherPrng, inextInfo.GetValue(currPrng));
            inextpInfo.SetValue(otherPrng, inextpInfo.GetValue(currPrng));
            PrngInfo.SetValueDirect(otherImplRef, otherPrng);

            if (!IsTrackingEnabledForCurrentThread)
            {
                return;
            }

            Holder src = GetData(random);
            Holder dst = GetData(other);
            dst.Index = src.Index;
            dst.Seed = src.Seed;
            dst.IsNet6 = false;
        }

        static void CloneNet6(this Random random, Random other)
        {
            other.SetImpl(CreateNet6Impl());
            object oldImpl = random.GetImpl();
            object newImpl = other.GetImpl();

            S0Info.SetValue(newImpl, S0Info.GetValue(oldImpl));
            S1Info.SetValue(newImpl, S1Info.GetValue(oldImpl));
            S2Info.SetValue(newImpl, S2Info.GetValue(oldImpl));
            S3Info.SetValue(newImpl, S3Info.GetValue(oldImpl));

            if (!IsTrackingEnabledForCurrentThread)
            {
                return;
            }

            Holder src = GetData(random);
            Holder dst = GetData(other);
            dst.Index = src.Index;
            dst.Seed = src.Seed;
            dst.IsNet6 = true;
        }

        private static Func<object> BuildFactory(Type type)
        {
            ConstructorInfo ctor = type.GetConstructor(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                binder: null,
                types: Type.EmptyTypes,
                modifiers: null
            );

            if (ctor == null)
            {
                return () => Activator.CreateInstance(type)!;
            }

            try
            {
                NewExpression newExpr = Expression.New(ctor);
                UnaryExpression castExpr = Expression.Convert(newExpr, typeof(object));
                return Expression.Lambda<Func<object>>(castExpr).Compile();
            }
            catch
            {
                return () => ctor.Invoke(null)!;
            }
        }

        public static void CloneOver(this Random random, Random other)
        {
            if (random.IsNet6())
            {
                CloneNet6(random, other);
            }
            else
            {
                CloneNet5(random, other);
            }
        }

        public static int IncrementCounter(this Random random)
        {
            if (!IsTrackingEnabledForCurrentThread)
            {
                return 0;
            }

            Holder data = GetData(random);
            return ++data.Index;
        }

        public static int IncrementCounter(this Random random, int n)
        {
            if (!IsTrackingEnabledForCurrentThread)
            {
                return 0;
            }

            Holder data = GetData(random);
            data.Index += n;
            return data.Index;
        }

        public static void set_Index(this Random random, int index)
        {
            Holder data = GetData(random);
            data.Index = index;
        }

        public static int get_Index(this Random random)
        {
            Holder data = GetData(random);
            return data.Index;
        }

        public static void set_Seed(this Random random, int seed)
        {
            Holder data = GetData(random);
            data.Seed = seed;
        }

        public static int get_Seed(this Random random)
        {
            Holder data = GetData(random);
            return data.Seed;
        }

        public static ulong get_S0(this Random random)
        {
            return (ulong)S0Info.GetValue(random.GetImpl())!;
        }

        public static void set_S0(this Random random, ulong value)
        {
            S0Info.SetValue(random.GetImpl(), value);
        }

        public static ulong get_S1(this Random random)
        {
            return (ulong)S1Info.GetValue(random.GetImpl())!;
        }

        public static void set_S1(this Random random, ulong value)
        {
            S1Info.SetValue(random.GetImpl(), value);
        }

        public static ulong get_S2(this Random random)
        {
            return (ulong)S2Info.GetValue(random.GetImpl())!;
        }

        public static void set_S2(this Random random, ulong value)
        {
            S2Info.SetValue(random.GetImpl(), value);
        }

        public static ulong get_S3(this Random random)
        {
            return (ulong)S3Info.GetValue(random.GetImpl())!;
        }

        public static void set_S3(this Random random, ulong value)
        {
            S3Info.SetValue(random.GetImpl(), value);
        }

        public static unsafe void LoadFromShared(this Random random)
        {
            if (!random.IsNet6())
            {
                throw new Exception("random types do not match");
            }

            ulong s0 = 0,
                s1 = 0,
                s2 = 0,
                s3 = 0;
            while ((s0 | s1 | s2 | s3) == 0)
            {
                s0 = SharedRandom.Net5NextUInt64();
                s1 = SharedRandom.Net5NextUInt64();
                s2 = SharedRandom.Net5NextUInt64();
                s3 = SharedRandom.Net5NextUInt64();
            }
            random.set_S0(s0);
            random.set_S1(s1);
            random.set_S2(s2);
            random.set_S3(s3);
        }

        public static unsafe Random SampleNet6Random(this Random random)
        {
            if (random.IsNet6())
            {
                throw new Exception("Expecting to call on Net5 random entity");
            }
            Random newRandom = new Random(0);
            newRandom.SetImpl(Activator.CreateInstance(Type.GetType(Net6_ImplTypeName)!));

            ulong s0 = 0,
                s1 = 0,
                s2 = 0,
                s3 = 0;
            while ((s0 | s1 | s2 | s3) == 0)
            {
                s0 = random.Net5NextUInt64();
                s1 = random.Net5NextUInt64();
                s2 = random.Net5NextUInt64();
                s3 = random.Net5NextUInt64();
            }
            newRandom.set_S0(s0);
            newRandom.set_S1(s1);
            newRandom.set_S2(s2);
            newRandom.set_S3(s3);
            return newRandom;
        }

        public static ulong Net5NextUInt64(this Random random)
        {
            return (uint)random.Next(4194304)
                | ((ulong)(uint)random.Next(4194304) << 22)
                | ((ulong)(uint)random.Next(1048576) << 44);
        }
    }
}
